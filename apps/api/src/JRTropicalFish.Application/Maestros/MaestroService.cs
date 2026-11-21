using System.Text.RegularExpressions;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;

namespace JRTropicalFish.Application.Maestros;

/// <summary>
/// Administración genérica de los maestros registrados en <see cref="RegistroMaestros"/>. Cada
/// maestro tiene su propia tabla, pero las reglas (código y nombre únicos, borrado lógico,
/// auditoría) son las mismas para todos.
/// </summary>
public partial class MaestroService(IMaestroRepository repositorio, IReloj reloj, ServicioAuditoria auditoria)
{
    /// <summary>Nombre en <c>auditoria.entidad</c>: <c>Maestro:{clave}</c>.</summary>
    public static string Entidad(string clave) => $"Maestro:{clave}";

    public static IReadOnlyList<MaestroInfo> Disponibles() =>
        [.. RegistroMaestros.Todos.Select(m => new MaestroInfo(m.Clave, m.Nombre, m.NombreSingular, m.Descripcion))];

    public async Task<Resultado<IReadOnlyList<ValorMaestro>>> ListarAsync(string clave, bool soloActivos, CancellationToken ct = default)
    {
        if (RegistroMaestros.Buscar(clave) is not { } maestro)
        {
            return MaestroNoExiste();
        }

        return Resultado<IReadOnlyList<ValorMaestro>>.Ok(await repositorio.ListarAsync(maestro.Tabla, soloActivos, ct));
    }

    public async Task<Resultado<Guid>> CrearAsync(string clave, GuardarValorMaestroRequest request, CancellationToken ct = default)
    {
        if (RegistroMaestros.Buscar(clave) is not { } maestro)
        {
            return MaestroNoExiste();
        }

        var datos = await ValidarAsync(maestro, request, null, ct);
        if (!datos.Exitoso)
        {
            return datos.Error!;
        }

        var (codigo, nombre) = datos.Valor;
        var id = Guid.CreateVersion7();
        await repositorio.CrearAsync(maestro.Tabla, id, codigo, nombre, request.Activo, reloj.AhoraUtc, ct);
        await auditoria.CreacionAsync(Entidad(clave), id, new ValorMaestroAuditable(codigo, nombre, request.Activo), ct: ct);
        return id;
    }

    public async Task<Resultado<Guid>> ActualizarAsync(
        string clave, Guid id, GuardarValorMaestroRequest request, CancellationToken ct = default)
    {
        if (RegistroMaestros.Buscar(clave) is not { } maestro)
        {
            return MaestroNoExiste();
        }

        if (await repositorio.ObtenerAsync(maestro.Tabla, id, ct) is not { } existente)
        {
            return ValorNoExiste(maestro);
        }

        var datos = await ValidarAsync(maestro, request, id, ct);
        if (!datos.Exitoso)
        {
            return datos.Error!;
        }

        var (codigo, nombre) = datos.Valor;
        await repositorio.ActualizarAsync(maestro.Tabla, id, codigo, nombre, request.Activo, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(
            Entidad(clave), id,
            ComparadorAuditoria.Comparar(
                new ValorMaestroAuditable(existente.Codigo, existente.Nombre, existente.Activo),
                new ValorMaestroAuditable(codigo, nombre, request.Activo)),
            ct);
        return id;
    }

    public async Task<Resultado<Guid>> CambiarEstadoAsync(string clave, Guid id, bool activo, CancellationToken ct = default)
    {
        if (RegistroMaestros.Buscar(clave) is not { } maestro)
        {
            return MaestroNoExiste();
        }

        if (await repositorio.ObtenerAsync(maestro.Tabla, id, ct) is not { } existente)
        {
            return ValorNoExiste(maestro);
        }

        if (existente.Activo == activo)
        {
            return id;
        }

        await repositorio.ActualizarAsync(maestro.Tabla, id, existente.Codigo, existente.Nombre, activo, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(
            Entidad(clave), id,
            [new CambioAuditado(nameof(ValorMaestroAuditable.Activo), Bool(existente.Activo), Bool(activo))], ct);
        return id;
    }

    /// <summary>
    /// Borrado lógico: los registros que ya lo usan (ej. empleados) conservan la referencia y siguen
    /// mostrando el nombre; simplemente deja de ofrecerse.
    /// </summary>
    public async Task<Resultado> EliminarAsync(string clave, Guid id, CancellationToken ct = default)
    {
        if (RegistroMaestros.Buscar(clave) is not { } maestro)
        {
            return MaestroNoExiste();
        }

        if (await repositorio.ObtenerAsync(maestro.Tabla, id, ct) is not { } existente)
        {
            return ValorNoExiste(maestro);
        }

        await repositorio.EliminarAsync(maestro.Tabla, id, reloj.AhoraUtc, ct);
        await auditoria.EliminacionAsync(
            Entidad(clave), id, new ValorMaestroAuditable(existente.Codigo, existente.Nombre, existente.Activo), ct);
        return Resultado.Ok();
    }

    public async Task<Resultado<IReadOnlyList<RegistroAuditoriaLectura>>> HistorialAsync(
        string clave, Guid id, CancellationToken ct = default)
    {
        if (RegistroMaestros.Buscar(clave) is null)
        {
            return MaestroNoExiste();
        }

        return Resultado<IReadOnlyList<RegistroAuditoriaLectura>>.Ok(await auditoria.HistorialAsync(Entidad(clave), id, ct));
    }

    /// <summary>
    /// Normaliza igual que se guarda (código en mayúsculas, sin espacios sobrantes) para que la
    /// auditoría no registre cambios falsos y la unicidad no dependa de mayúsculas.
    /// </summary>
    private async Task<Resultado<(string Codigo, string Nombre)>> ValidarAsync(
        DefinicionMaestro maestro, GuardarValorMaestroRequest request, Guid? excluirId, CancellationToken ct)
    {
        var codigo = request.Codigo?.Trim().ToUpperInvariant() ?? string.Empty;
        if (codigo.Length is < 1 or > 20 || !CodigoValido().IsMatch(codigo))
        {
            return Error.Validacion("El código debe tener entre 1 y 20 caracteres: letras, números, guion o guion bajo.");
        }

        var nombre = EspaciosRepetidos().Replace(request.Nombre?.Trim() ?? string.Empty, " ");
        if (nombre.Length is < 2 or > 150)
        {
            return Error.Validacion("El nombre debe tener entre 2 y 150 caracteres.");
        }

        if (await repositorio.ExisteCodigoAsync(maestro.Tabla, codigo, excluirId, ct))
        {
            return Error.Conflicto($"Ya existe un {maestro.NombreSingular} con el código {codigo}.");
        }

        if (await repositorio.ExisteNombreAsync(maestro.Tabla, nombre, excluirId, ct))
        {
            return Error.Conflicto($"Ya existe un {maestro.NombreSingular} con el nombre «{nombre}».");
        }

        return (codigo, nombre);
    }

    private static Error MaestroNoExiste() => Error.NoEncontrado("El maestro solicitado no existe.");

    private static Error ValorNoExiste(DefinicionMaestro maestro) =>
        Error.NoEncontrado($"El {maestro.NombreSingular} no existe.");

    private static string Bool(bool valor) => valor ? "true" : "false";

    [GeneratedRegex(@"^[A-Z0-9Ñ][A-Z0-9Ñ_-]*$")]
    private static partial Regex CodigoValido();

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspaciosRepetidos();
}
