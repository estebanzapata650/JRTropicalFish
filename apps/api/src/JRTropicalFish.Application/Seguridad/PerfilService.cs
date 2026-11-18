using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Domain.Seguridad;

namespace JRTropicalFish.Application.Seguridad;

public class PerfilService(
    IPerfilRepository perfiles,
    IPermisoRepository permisos,
    IUsuarioRepository usuarios,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    public const string Entidad = "Perfil";

    public Task<IReadOnlyList<PermisoLectura>> ListarPermisosAsync(CancellationToken ct = default) =>
        permisos.ListarAsync(ct);

    public Task<IReadOnlyList<PerfilResumen>> ListarAsync(string? busqueda, CancellationToken ct = default) =>
        perfiles.ListarAsync(busqueda, ct);

    public async Task<Resultado<PerfilDetalle>> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        await perfiles.ObtenerPorIdAsync(id, ct) is { } perfil ? perfil : NoExiste();

    public async Task<Resultado<Guid>> CrearAsync(GuardarPerfilRequest request, CancellationToken ct = default)
    {
        var validacion = await ValidarAsync(request, null, ct);
        if (!validacion.Exitoso)
        {
            return validacion.Error!;
        }

        var (nombre, descripcion, codigos) = validacion.Valor;
        var perfil = new Perfil
        {
            Nombre = nombre,
            Descripcion = descripcion,
            EsPlantilla = false,
            Activo = request.Activo,
            CreadoEn = reloj.AhoraUtc,
        };

        await perfiles.CrearAsync(perfil, codigos, ct);
        await auditoria.CreacionAsync(
            Entidad, perfil.Id, new PerfilAuditable(nombre, descripcion, request.Activo),
            ComparadorColecciones.CompararConjunto("Permisos", [], codigos), ct);
        return perfil.Id;
    }

    /// <summary>
    /// Si cambian los permisos o el estado, los usuarios del perfil deben volver a iniciar sesión:
    /// sus tokens llevan los permisos viejos. Así un permiso retirado deja de valer de inmediato.
    /// </summary>
    public async Task<Resultado<Guid>> ActualizarAsync(Guid id, GuardarPerfilRequest request, CancellationToken ct = default)
    {
        if (await perfiles.ObtenerPorIdAsync(id, ct) is not { } existente)
        {
            return NoExiste();
        }

        var validacion = await ValidarAsync(request, id, ct);
        if (!validacion.Exitoso)
        {
            return validacion.Error!;
        }

        var (nombre, descripcion, codigos) = validacion.Valor;

        // El sembrado de migraciones y el seeder de desarrollo ubican los perfiles base por nombre.
        if (existente.EsPlantilla && !string.Equals(existente.Nombre, nombre, StringComparison.Ordinal))
        {
            return Error.Validacion("El nombre de un perfil base no se puede cambiar.");
        }

        var concediaAdministracion = UsuarioService.EsAdministrador(existente);
        var concederaAdministracion = request.Activo && codigos.Contains(Permisos.UsuariosEditar);
        if (concediaAdministracion && !concederaAdministracion
            && await perfiles.ContarUsuariosAsync(id, ct) > 0
            && await usuarios.ContarAdministradoresAsync(null, id, ct) == 0)
        {
            return Error.Conflicto(
                "No se puede: el sistema quedaría sin ningún usuario activo que pueda administrar usuarios.");
        }

        var cambianPermisos = !existente.Permisos.ToHashSet().SetEquals(codigos);
        var cambiaEstado = existente.Activo != request.Activo;

        await perfiles.ActualizarAsync(
            new Perfil
            {
                Id = id,
                Nombre = nombre,
                Descripcion = descripcion,
                EsPlantilla = existente.EsPlantilla,
                Activo = request.Activo,
                ActualizadoEn = reloj.AhoraUtc,
            },
            codigos,
            invalidarSesiones: cambianPermisos || cambiaEstado,
            ct);

        var cambios = ComparadorAuditoria.Comparar(
                new PerfilAuditable(existente.Nombre, existente.Descripcion, existente.Activo),
                new PerfilAuditable(nombre, descripcion, request.Activo))
            .Concat(ComparadorColecciones.CompararConjunto("Permisos", existente.Permisos, codigos))
            .ToList();

        await auditoria.CambiosAsync(Entidad, id, cambios, ct);
        return id;
    }

    public async Task<Resultado<Guid>> EliminarAsync(Guid id, CancellationToken ct = default)
    {
        if (await perfiles.ObtenerPorIdAsync(id, ct) is not { } existente)
        {
            return NoExiste();
        }

        if (existente.EsPlantilla)
        {
            return Error.Conflicto("Los perfiles base del sistema no se pueden eliminar.");
        }

        if (await perfiles.ContarUsuariosAsync(id, ct) > 0)
        {
            return Error.Conflicto("No se puede eliminar un perfil que tiene usuarios asignados.");
        }

        await perfiles.EliminarAsync(id, ct);
        await auditoria.EliminacionAsync(
            Entidad, id, new PerfilAuditable(existente.Nombre, existente.Descripcion, existente.Activo), ct);
        return id;
    }

    public Task<IReadOnlyList<RegistroAuditoriaLectura>> HistorialAsync(Guid id, CancellationToken ct = default) =>
        auditoria.HistorialAsync(Entidad, id, ct);

    private async Task<Resultado<(string Nombre, string? Descripcion, List<string> Permisos)>> ValidarAsync(
        GuardarPerfilRequest request, Guid? id, CancellationToken ct)
    {
        var nombre = request.Nombre?.Trim() ?? string.Empty;
        if (nombre.Length is < 2 or > 100)
        {
            return Error.Validacion("El nombre del perfil debe tener entre 2 y 100 caracteres.");
        }

        var descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim();
        if (descripcion?.Length > 250)
        {
            return Error.Validacion("La descripción no puede superar 250 caracteres.");
        }

        var codigos = DepurarPermisos(request.Permisos);
        if (codigos.Count == 0)
        {
            return Error.Validacion("El perfil debe tener al menos un permiso.");
        }

        if (await perfiles.ExisteNombreAsync(nombre, id, ct))
        {
            return Error.Conflicto("Ya existe un perfil con ese nombre.");
        }

        return (nombre, descripcion, codigos);
    }

    /// <summary>Descarta códigos desconocidos: el cliente no puede inventar permisos que no existen.</summary>
    internal static List<string> DepurarPermisos(IReadOnlyList<string>? codigos) =>
        [.. (codigos ?? []).Distinct().Where(Permisos.Todos.ContainsKey).Order(StringComparer.Ordinal)];

    private static Error NoExiste() => Error.NoEncontrado("El perfil no existe.");
}
