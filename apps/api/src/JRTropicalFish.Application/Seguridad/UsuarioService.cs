using System.Globalization;
using System.Text.RegularExpressions;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Domain.Seguridad;

namespace JRTropicalFish.Application.Seguridad;

public partial class UsuarioService(
    IUsuarioRepository usuarios,
    IPerfilRepository perfiles,
    IPasswordHasher passwordHasher,
    IUsuarioActual usuarioActual,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    /// <summary>Nombre con el que se guarda en <c>auditoria.entidad</c>.</summary>
    public const string Entidad = "Usuario";

    private const string MensajeSinAdministrador =
        "No se puede: el sistema quedaría sin ningún usuario activo que pueda administrar usuarios.";

    public Task<IReadOnlyList<UsuarioResumen>> ListarAsync(string? busqueda, CancellationToken ct = default) =>
        usuarios.ListarAsync(busqueda, ct);

    public async Task<Resultado<UsuarioResumen>> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        await usuarios.ObtenerPorIdAsync(id, ct) is { } usuario ? usuario : NoExiste();

    public async Task<Resultado<Guid>> CrearAsync(CrearUsuarioRequest request, CancellationToken ct = default)
    {
        var datos = await ValidarDatosAsync(request.Nombre, request.Email, request.PerfilId, null, ct);
        if (!datos.Exitoso)
        {
            return datos.Error!;
        }

        if (ReglasPassword.Validar(request.Password) is { } motivo)
        {
            return Error.Validacion(motivo);
        }

        var (nombre, email, perfil) = datos.Valor;
        var usuario = new Usuario
        {
            Nombre = nombre,
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            PerfilId = perfil.Id,
            Activo = true,
            CreadoEn = reloj.AhoraUtc,
        };

        await usuarios.CrearAsync(usuario, ct);
        await auditoria.CreacionAsync(Entidad, usuario.Id, new UsuarioAuditable(nombre, email, perfil.Nombre, true), ct: ct);
        return usuario.Id;
    }

    public async Task<Resultado<Guid>> ActualizarAsync(Guid id, ActualizarUsuarioRequest request, CancellationToken ct = default)
    {
        if (await usuarios.ObtenerPorIdAsync(id, ct) is not { } existente)
        {
            return NoExiste();
        }

        var datos = await ValidarDatosAsync(request.Nombre, request.Email, request.PerfilId, id, ct);
        if (!datos.Exitoso)
        {
            return datos.Error!;
        }

        var cambiaPassword = !string.IsNullOrEmpty(request.Password);
        if (cambiaPassword && ReglasPassword.Validar(request.Password) is { } motivo)
        {
            return Error.Validacion(motivo);
        }

        var (nombre, email, perfil) = datos.Valor;
        var cambiaPerfil = perfil.Id != existente.PerfilId;

        if (cambiaPerfil && existente.Activo && !EsAdministrador(perfil) && await EsAdministradorAsync(existente.PerfilId, ct)
            && await usuarios.ContarAdministradoresAsync(id, null, ct) == 0)
        {
            return Error.Conflicto(MensajeSinAdministrador);
        }

        var ahora = reloj.AhoraUtc;
        // Cambiar de perfil cambia los permisos: las sesiones abiertas tienen los viejos en el token.
        await usuarios.ActualizarAsync(id, nombre, email, perfil.Id, invalidarSesiones: cambiaPerfil, ahora, ct);
        if (cambiaPassword)
        {
            await usuarios.CambiarPasswordAsync(id, passwordHasher.Hash(request.Password!), ahora, ct);
        }

        var cambios = ComparadorAuditoria.Comparar(
            new UsuarioAuditable(existente.Nombre, existente.Email, existente.PerfilNombre, existente.Activo),
            new UsuarioAuditable(nombre, email, perfil.Nombre, existente.Activo)).ToList();
        if (cambiaPassword)
        {
            cambios.Add(new CambioAuditado("Contraseña", null, "Restablecida por un administrador"));
        }

        await auditoria.CambiosAsync(Entidad, id, cambios, ct);
        return id;
    }

    /// <summary>Desactivar revoca las sesiones abiertas del usuario de inmediato.</summary>
    public async Task<Resultado<Guid>> CambiarEstadoAsync(Guid id, bool activo, CancellationToken ct = default)
    {
        if (await usuarios.ObtenerPorIdAsync(id, ct) is not { } existente)
        {
            return NoExiste();
        }

        if (existente.Activo == activo)
        {
            return id;
        }

        if (!activo)
        {
            if (usuarioActual.UsuarioId == id)
            {
                return Error.Conflicto("No puedes desactivar tu propio usuario.");
            }

            if (await EsAdministradorAsync(existente.PerfilId, ct) && await usuarios.ContarAdministradoresAsync(id, null, ct) == 0)
            {
                return Error.Conflicto(MensajeSinAdministrador);
            }
        }

        await usuarios.CambiarEstadoAsync(id, activo, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(
            Entidad, id, [new CambioAuditado(nameof(UsuarioAuditable.Activo), Bool(existente.Activo), Bool(activo))], ct);
        return id;
    }

    public async Task<Resultado<Guid>> DesbloquearAsync(Guid id, CancellationToken ct = default)
    {
        if (await usuarios.ObtenerPorIdAsync(id, ct) is not { } existente)
        {
            return NoExiste();
        }

        await usuarios.DesbloquearAsync(id, ct);

        if (ReglasBloqueo.EstaBloqueado(existente.BloqueadoHasta, reloj.AhoraUtc))
        {
            await auditoria.CambiosAsync(
                Entidad, id,
                [new CambioAuditado(
                    "Bloqueo",
                    $"Bloqueado hasta {existente.BloqueadoHasta!.Value.ToString("O", CultureInfo.InvariantCulture)}",
                    "Desbloqueado por un administrador")],
                ct);
        }

        return id;
    }

    public Task<IReadOnlyList<RegistroAuditoriaLectura>> HistorialAsync(Guid id, CancellationToken ct = default) =>
        auditoria.HistorialAsync(Entidad, id, ct);

    /// <summary>
    /// Valida y normaliza nombre, email y perfil. Normaliza en C# igual que en SQL (email en
    /// minúsculas): si no, el objeto en memoria difiere del guardado y la auditoría ve cambios falsos.
    /// </summary>
    private async Task<Resultado<(string Nombre, string Email, PerfilDetalle Perfil)>> ValidarDatosAsync(
        string? nombre, string? email, Guid perfilId, Guid? excluirId, CancellationToken ct)
    {
        var nombreLimpio = nombre?.Trim() ?? string.Empty;
        if (nombreLimpio.Length is < 2 or > 200)
        {
            return Error.Validacion("El nombre debe tener entre 2 y 200 caracteres.");
        }

        var emailLimpio = email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (emailLimpio.Length > 320 || !EmailValido().IsMatch(emailLimpio))
        {
            return Error.Validacion("El correo electrónico no es válido.");
        }

        if (await usuarios.ExisteEmailAsync(emailLimpio, excluirId, ct))
        {
            return Error.Conflicto("Ya existe un usuario con ese correo electrónico.");
        }

        var perfil = await perfiles.ObtenerPorIdAsync(perfilId, ct);
        if (perfil is null)
        {
            return Error.Validacion("El perfil seleccionado no existe.");
        }

        if (!perfil.Activo)
        {
            return Error.Validacion("El perfil seleccionado está inactivo.");
        }

        return (nombreLimpio, emailLimpio, perfil);
    }

    private async Task<bool> EsAdministradorAsync(Guid perfilId, CancellationToken ct) =>
        await perfiles.ObtenerPorIdAsync(perfilId, ct) is { } perfil && EsAdministrador(perfil);

    /// <summary>"Administrador" = puede editar usuarios: es quien podría reparar cualquier otro acceso.</summary>
    internal static bool EsAdministrador(PerfilDetalle perfil) =>
        perfil.Activo && perfil.Permisos.Contains(Permisos.UsuariosEditar);

    private static Error NoExiste() => Error.NoEncontrado("El usuario no existe.");

    private static string Bool(bool valor) => valor ? "true" : "false";

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailValido();
}
