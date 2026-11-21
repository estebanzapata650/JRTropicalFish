using System.Text.Json;
using System.Text.RegularExpressions;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Archivos;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Maestros;
using JRTropicalFish.Domain.Administracion;

namespace JRTropicalFish.Application.Administracion;

/// <summary>
/// Administración de empleados. El salario es un dato protegido: quien no tiene
/// <c>empleados:ver-salario</c> no lo recibe en ninguna respuesta (ni en el historial), no puede
/// fijarlo y, al editar, el salario guardado se conserva. El controlador decide <c>verSalario</c>
/// a partir de los permisos del token.
/// </summary>
public partial class EmpleadoService(
    IEmpleadoRepository empleados,
    IMaestroRepository maestros,
    IAlmacenArchivos almacen,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    /// <summary>Nombre con el que se guarda en <c>auditoria.entidad</c>.</summary>
    public const string Entidad = "Empleado";

    /// <summary>Carpeta de las fotos en el almacén (primer segmento de la clave).</summary>
    public const string CarpetaFotos = "empleados";

    public const long TamanoMaximoFotoBytes = 10 * 1024 * 1024;

    public const int EdadMinima = 14;

    private static readonly string[] TiposFoto = ["image/jpeg", "image/png", "image/webp"];

    public async Task<IReadOnlyList<EmpleadoDetalle>> ListarAsync(string? busqueda, bool verSalario, CancellationToken ct = default) =>
        [.. (await empleados.ListarAsync(busqueda, ct)).Select(e => ADetalle(e, verSalario))];

    public async Task<Resultado<EmpleadoDetalle>> ObtenerAsync(Guid id, bool verSalario, CancellationToken ct = default) =>
        await empleados.ObtenerPorIdAsync(id, ct) is { } empleado ? ADetalle(empleado, verSalario) : NoExiste();

    public async Task<Resultado<Guid>> CrearAsync(GuardarEmpleadoRequest request, bool verSalario, CancellationToken ct = default)
    {
        if (!verSalario && request.Salario is not null)
        {
            return SinPermisoSalario();
        }

        var datos = await ValidarAsync(request, null, verSalario ? request.Salario : 0m, ct);
        if (!datos.Exitoso)
        {
            return datos.Error!;
        }

        var d = datos.Valor;
        var empleado = NuevoEmpleado(request, d);
        empleado.Activo = true;
        empleado.CreadoEn = reloj.AhoraUtc;

        await empleados.CrearAsync(empleado, ct);
        await auditoria.CreacionAsync(Entidad, empleado.Id, Auditable(d, request, activo: true), ct: ct);
        return empleado.Id;
    }

    public async Task<Resultado<Guid>> ActualizarAsync(
        Guid id, GuardarEmpleadoRequest request, bool verSalario, CancellationToken ct = default)
    {
        if (await empleados.ObtenerPorIdAsync(id, ct) is not { } existente)
        {
            return NoExiste();
        }

        if (!verSalario && request.Salario is not null)
        {
            return SinPermisoSalario();
        }

        var datos = await ValidarAsync(request, existente, verSalario ? request.Salario : existente.Salario, ct);
        if (!datos.Exitoso)
        {
            return datos.Error!;
        }

        var d = datos.Valor;
        var empleado = NuevoEmpleado(request, d);
        empleado.Id = id;
        empleado.Activo = existente.Activo;
        empleado.CreadoEn = existente.CreadoEn;
        empleado.ActualizadoEn = reloj.AhoraUtc;

        await empleados.ActualizarAsync(empleado, actualizarSalario: verSalario, ct);
        await auditoria.CambiosAsync(
            Entidad, id,
            ComparadorAuditoria.Comparar(Auditable(existente), Auditable(d, request, existente.Activo)),
            ct);
        return id;
    }

    public async Task<Resultado<Guid>> CambiarEstadoAsync(Guid id, bool activo, CancellationToken ct = default)
    {
        if (await empleados.ObtenerPorIdAsync(id, ct) is not { } existente)
        {
            return NoExiste();
        }

        if (existente.Activo == activo)
        {
            return id;
        }

        await empleados.CambiarEstadoAsync(id, activo, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(
            Entidad, id, [new CambioAuditado(nameof(EmpleadoAuditable.Activo), Bool(existente.Activo), Bool(activo))], ct);
        return id;
    }

    /// <summary>Borrado lógico. La foto se conserva en el almacén junto con el registro.</summary>
    public async Task<Resultado> EliminarAsync(Guid id, CancellationToken ct = default)
    {
        if (await empleados.ObtenerPorIdAsync(id, ct) is not { } existente)
        {
            return NoExiste();
        }

        await empleados.EliminarAsync(id, reloj.AhoraUtc, ct);
        await auditoria.EliminacionAsync(Entidad, id, Auditable(existente), ct);
        return Resultado.Ok();
    }

    /// <summary>Sin <c>empleados:ver-salario</c>, los cambios de salario se quitan del historial.</summary>
    public async Task<IReadOnlyList<RegistroAuditoriaLectura>> HistorialAsync(Guid id, bool verSalario, CancellationToken ct = default)
    {
        var registros = await auditoria.HistorialAsync(Entidad, id, ct);
        if (verSalario)
        {
            return registros;
        }

        var resultado = new List<RegistroAuditoriaLectura>(registros.Count);
        foreach (var registro in registros)
        {
            var cambios = JsonSerializer.Deserialize<List<CambioAuditado>>(registro.CambiosJson) ?? [];
            var visibles = cambios.Where(c => c.Campo != nameof(EmpleadoAuditable.Salario)).ToList();
            if (visibles.Count == 0)
            {
                continue;
            }

            resultado.Add(visibles.Count == cambios.Count
                ? registro
                : registro with { CambiosJson = JsonSerializer.Serialize(visibles) });
        }

        return resultado;
    }

    // --- Foto ---

    /// <summary>
    /// Reemplaza la foto. El tipo se decide por los bytes (no por el nombre ni el Content-Type) y
    /// la clave la arma <see cref="ClaveArchivo"/>. La foto anterior se borra después de guardar la nueva.
    /// </summary>
    /// <returns>La nueva versión de la foto.</returns>
    public async Task<Resultado<string>> CambiarFotoAsync(Guid id, Stream contenido, CancellationToken ct = default)
    {
        if (await empleados.ObtenerPorIdAsync(id, ct) is not { } existente)
        {
            return NoExiste();
        }

        using var buffer = new MemoryStream();
        await contenido.CopyToAsync(buffer, ct);
        if (buffer.Length == 0)
        {
            return Error.Validacion("El archivo de la foto está vacío.");
        }

        if (buffer.Length > TamanoMaximoFotoBytes)
        {
            return Error.Validacion("La foto no puede pesar más de 10 MB.");
        }

        var inicio = buffer.GetBuffer().AsSpan(0, (int)Math.Min(buffer.Length, DetectorTipoArchivo.BytesNecesarios));
        var tipo = DetectorTipoArchivo.Detectar(inicio);
        if (tipo is null || !TiposFoto.Contains(tipo))
        {
            return Error.Validacion("La foto debe ser una imagen JPG, PNG o WEBP.");
        }

        buffer.Position = 0;
        var clave = ClaveArchivo.ParaDocumento(CarpetaFotos, id, Guid.CreateVersion7(), tipo);
        await almacen.SubirAsync(clave, buffer, tipo, ct);
        await empleados.CambiarFotoAsync(id, clave, reloj.AhoraUtc, ct);

        if (existente.FotoClave is { } anterior)
        {
            await almacen.EliminarAsync(anterior, ct);
        }

        await auditoria.CambiosAsync(
            Entidad, id, [new CambioAuditado("Foto", existente.FotoClave is null ? null : "Anterior", "Actualizada")], ct);
        return VersionFoto(clave)!;
    }

    public async Task<Resultado<ArchivoDescargado>> DescargarFotoAsync(Guid id, CancellationToken ct = default)
    {
        if (await empleados.ObtenerPorIdAsync(id, ct) is not { FotoClave: { } clave })
        {
            return Error.NoEncontrado("El empleado no tiene foto.");
        }

        return await almacen.DescargarAsync(clave, ct) is { } archivo
            ? archivo
            : Error.NoEncontrado("No se encontró el archivo de la foto.");
    }

    public async Task<Resultado> EliminarFotoAsync(Guid id, CancellationToken ct = default)
    {
        if (await empleados.ObtenerPorIdAsync(id, ct) is not { } existente)
        {
            return NoExiste();
        }

        if (existente.FotoClave is not { } clave)
        {
            return Resultado.Ok();
        }

        await empleados.CambiarFotoAsync(id, null, reloj.AhoraUtc, ct);
        await almacen.EliminarAsync(clave, ct);
        await auditoria.CambiosAsync(Entidad, id, [new CambioAuditado("Foto", "Anterior", "Eliminada")], ct);
        return Resultado.Ok();
    }

    // --- Validación ---

    private sealed record DatosEmpleado(
        string Numero,
        string Nombres,
        string Apellidos,
        string? Genero,
        string? Telefono,
        string? Email,
        string? Direccion,
        string? Ciudad,
        decimal Salario,
        string TipoIdentificacion,
        string Cargo,
        string? Area,
        string TipoContrato);

    /// <summary>
    /// Valida y normaliza. Normaliza en C# igual que se guarda (documento en mayúsculas sin espacios
    /// ni puntos, email en minúsculas) para que la auditoría no vea cambios falsos.
    /// </summary>
    /// <param name="salario">El salario que quedará guardado (el pedido, o el actual si no se puede editar).</param>
    private async Task<Resultado<DatosEmpleado>> ValidarAsync(
        GuardarEmpleadoRequest request, EmpleadoFila? existente, decimal? salario, CancellationToken ct)
    {
        var numero = SeparadoresDocumento().Replace(request.NumeroIdentificacion ?? string.Empty, string.Empty).ToUpperInvariant();
        if (numero.Length is < 3 or > 30 || !DocumentoValido().IsMatch(numero))
        {
            return Error.Validacion("El número de identificación debe tener entre 3 y 30 caracteres: letras, números o guion.");
        }

        var nombres = Limpiar(request.Nombres) ?? string.Empty;
        if (nombres.Length is < 2 or > 100)
        {
            return Error.Validacion("Los nombres deben tener entre 2 y 100 caracteres.");
        }

        var apellidos = Limpiar(request.Apellidos) ?? string.Empty;
        if (apellidos.Length is < 2 or > 100)
        {
            return Error.Validacion("Los apellidos deben tener entre 2 y 100 caracteres.");
        }

        var genero = Limpiar(request.Genero);
        if (genero is not null && !Generos.Todos.Contains(genero))
        {
            return Error.Validacion("El género no es válido.");
        }

        if (request.FechaNacimiento is { } nacimiento)
        {
            var hoy = reloj.Hoy;
            if (nacimiento > hoy)
            {
                return Error.Validacion("La fecha de nacimiento no puede ser futura.");
            }

            var edad = Edad(nacimiento, hoy);
            if (edad < EdadMinima || edad > 100)
            {
                return Error.Validacion($"La edad debe estar entre {EdadMinima} y 100 años.");
            }
        }

        var telefono = Limpiar(request.Telefono);
        if (telefono is { Length: > 30 })
        {
            return Error.Validacion("El teléfono no puede tener más de 30 caracteres.");
        }

        var email = Limpiar(request.Email)?.ToLowerInvariant();
        if (email is not null && (email.Length > 320 || !EmailValido().IsMatch(email)))
        {
            return Error.Validacion("El correo electrónico no es válido.");
        }

        var direccion = Limpiar(request.Direccion);
        if (direccion is { Length: > 250 })
        {
            return Error.Validacion("La dirección no puede tener más de 250 caracteres.");
        }

        var ciudad = Limpiar(request.Ciudad);
        if (ciudad is { Length: > 100 })
        {
            return Error.Validacion("La ciudad no puede tener más de 100 caracteres.");
        }

        // Un DateOnly ausente en el JSON llega como 0001-01-01.
        if (request.FechaIngreso.Year < 1950)
        {
            return Error.Validacion("La fecha de ingreso es obligatoria.");
        }

        if (request.FechaNacimiento is { } nac && request.FechaIngreso <= nac)
        {
            return Error.Validacion("La fecha de ingreso debe ser posterior a la de nacimiento.");
        }

        if (request.FechaRetiro is { } retiro && retiro < request.FechaIngreso)
        {
            return Error.Validacion("La fecha de retiro no puede ser anterior a la de ingreso.");
        }

        if (salario is null)
        {
            return Error.Validacion("El salario es obligatorio.");
        }

        var salarioFinal = Math.Round(salario.Value, 2, MidpointRounding.AwayFromZero);
        if (salarioFinal is < 0 or > 999_999_999_999m)
        {
            return Error.Validacion("El salario no es válido.");
        }

        var tipo = await ValidarMaestroAsync(
            RegistroMaestros.TiposIdentificacion, request.TipoIdentificacionId, existente?.TipoIdentificacionId, ct);
        if (!tipo.Exitoso)
        {
            return tipo.Error!;
        }

        var cargo = await ValidarMaestroAsync(RegistroMaestros.Cargos, request.CargoId, existente?.CargoId, ct);
        if (!cargo.Exitoso)
        {
            return cargo.Error!;
        }

        string? area = null;
        if (request.AreaId is { } areaId)
        {
            var resultadoArea = await ValidarMaestroAsync(RegistroMaestros.Areas, areaId, existente?.AreaId, ct);
            if (!resultadoArea.Exitoso)
            {
                return resultadoArea.Error!;
            }

            area = resultadoArea.Valor.Nombre;
        }

        var contrato = await ValidarMaestroAsync(
            RegistroMaestros.TiposContrato, request.TipoContratoId, existente?.TipoContratoId, ct);
        if (!contrato.Exitoso)
        {
            return contrato.Error!;
        }

        if (await empleados.ExisteIdentificacionAsync(request.TipoIdentificacionId, numero, existente?.Id, ct))
        {
            return Error.Conflicto($"Ya existe un empleado con la identificación {tipo.Valor.Codigo} {numero}.");
        }

        return new DatosEmpleado(
            numero, nombres, apellidos, genero, telefono, email, direccion, ciudad, salarioFinal,
            tipo.Valor.Codigo, cargo.Valor.Nombre, area, contrato.Valor.Nombre);
    }

    /// <summary>
    /// El valor debe existir. Si se está eligiendo (no es el que ya tenía el empleado), además debe
    /// estar activo: un valor desactivado no se ofrece, pero quien ya lo tiene lo conserva.
    /// </summary>
    private async Task<Resultado<ValorMaestro>> ValidarMaestroAsync(string clave, Guid id, Guid? actual, CancellationToken ct)
    {
        var maestro = RegistroMaestros.Obtener(clave);
        var valor = await maestros.ObtenerAsync(maestro.Tabla, id, ct);
        if (valor is null)
        {
            return Error.Validacion($"Selecciona un {maestro.NombreSingular} válido.");
        }

        if (!valor.Activo && id != actual)
        {
            return Error.Validacion($"El {maestro.NombreSingular} «{valor.Nombre}» está inactivo.");
        }

        return valor;
    }

    // --- Proyecciones ---

    private static Empleado NuevoEmpleado(GuardarEmpleadoRequest r, DatosEmpleado d) => new()
    {
        TipoIdentificacionId = r.TipoIdentificacionId,
        NumeroIdentificacion = d.Numero,
        Nombres = d.Nombres,
        Apellidos = d.Apellidos,
        Genero = d.Genero,
        FechaNacimiento = r.FechaNacimiento,
        Telefono = d.Telefono,
        Email = d.Email,
        Direccion = d.Direccion,
        Ciudad = d.Ciudad,
        CargoId = r.CargoId,
        AreaId = r.AreaId,
        TipoContratoId = r.TipoContratoId,
        FechaIngreso = r.FechaIngreso,
        FechaRetiro = r.FechaRetiro,
        Salario = d.Salario,
    };

    private static EmpleadoDetalle ADetalle(EmpleadoFila e, bool verSalario) => new(
        e.Id, e.TipoIdentificacionId, e.TipoIdentificacionCodigo, e.NumeroIdentificacion, e.Nombres, e.Apellidos,
        e.Genero, e.FechaNacimiento, e.Telefono, e.Email, e.Direccion, e.Ciudad, e.CargoId, e.CargoNombre,
        e.AreaId, e.AreaNombre, e.TipoContratoId, e.TipoContratoNombre, e.FechaIngreso, e.FechaRetiro,
        verSalario ? e.Salario : null, e.Activo, VersionFoto(e.FotoClave), e.CreadoEn);

    private static EmpleadoAuditable Auditable(EmpleadoFila e) => new(
        e.TipoIdentificacionCodigo, e.NumeroIdentificacion, e.Nombres, e.Apellidos, e.Genero, e.FechaNacimiento,
        e.Telefono, e.Email, e.Direccion, e.Ciudad, e.CargoNombre, e.AreaNombre, e.TipoContratoNombre,
        e.FechaIngreso, e.FechaRetiro, e.Salario, e.Activo);

    private static EmpleadoAuditable Auditable(DatosEmpleado d, GuardarEmpleadoRequest r, bool activo) => new(
        d.TipoIdentificacion, d.Numero, d.Nombres, d.Apellidos, d.Genero, r.FechaNacimiento, d.Telefono, d.Email,
        d.Direccion, d.Ciudad, d.Cargo, d.Area, d.TipoContrato, r.FechaIngreso, r.FechaRetiro, d.Salario, activo);

    /// <summary>El id del documento dentro de la clave: cambia con cada foto nueva.</summary>
    internal static string? VersionFoto(string? clave) =>
        clave is null ? null : Path.GetFileNameWithoutExtension(clave);

    internal static int Edad(DateOnly nacimiento, DateOnly hoy)
    {
        var edad = hoy.Year - nacimiento.Year;
        return nacimiento.AddYears(edad) > hoy ? edad - 1 : edad;
    }

    private static string? Limpiar(string? valor)
    {
        var limpio = EspaciosRepetidos().Replace(valor?.Trim() ?? string.Empty, " ");
        return limpio.Length == 0 ? null : limpio;
    }

    private static Error NoExiste() => Error.NoEncontrado("El empleado no existe.");

    private static Error SinPermisoSalario() => Error.Prohibido("No tienes permiso para ver ni modificar el salario.");

    private static string Bool(bool valor) => valor ? "true" : "false";

    [GeneratedRegex(@"[\s.]+")]
    private static partial Regex SeparadoresDocumento();

    [GeneratedRegex(@"^[A-Z0-9][A-Z0-9-]*$")]
    private static partial Regex DocumentoValido();

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspaciosRepetidos();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailValido();
}
