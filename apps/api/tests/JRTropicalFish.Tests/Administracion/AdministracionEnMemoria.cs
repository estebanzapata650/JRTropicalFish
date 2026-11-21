using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Administracion;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Maestros;
using JRTropicalFish.Domain.Administracion;
using JRTropicalFish.Domain.Auditoria;

namespace JRTropicalFish.Tests.Administracion;

/// <summary>
/// Repositorios de maestros y empleados, almacén de archivos y auditoría en memoria, con las mismas
/// reglas que las funciones PL/pgSQL (borrado lógico, unicidad sin distinguir mayúsculas).
/// </summary>
public sealed class AdministracionEnMemoria :
    IMaestroRepository, IEmpleadoRepository, IAlmacenArchivos, IAuditoriaRepository, IReloj, IUsuarioActual
{
    public sealed class ValorMem
    {
        public Guid Id { get; init; }
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public bool Activo { get; set; } = true;
        public DateTime? EliminadoEn { get; set; }
    }

    public Dictionary<string, List<ValorMem>> Maestros { get; } =
        RegistroMaestros.Todos.ToDictionary(m => m.Tabla, _ => new List<ValorMem>());

    public Dictionary<Guid, Empleado> Empleados { get; } = [];
    public Dictionary<string, (byte[] Datos, string Tipo)> Archivos { get; } = [];
    public List<RegistroAuditoria> Auditoria { get; } = [];

    public DateTime Ahora { get; set; } = new(2026, 11, 20, 15, 0, 0, DateTimeKind.Utc);

    public ValorMem Cc { get; }
    public ValorMem Operario { get; }
    public ValorMem Indefinido { get; }

    public AdministracionEnMemoria()
    {
        Cc = AgregarValor(RegistroMaestros.TiposIdentificacion, "CC", "Cédula de ciudadanía");
        Operario = AgregarValor(RegistroMaestros.Cargos, "OPE", "Operario");
        Indefinido = AgregarValor(RegistroMaestros.TiposContrato, "INDEFINIDO", "Término indefinido");
    }

    public ValorMem AgregarValor(string clave, string codigo, string nombre, bool activo = true)
    {
        var valor = new ValorMem { Id = Guid.CreateVersion7(), Codigo = codigo, Nombre = nombre, Activo = activo };
        Maestros[RegistroMaestros.Obtener(clave).Tabla].Add(valor);
        return valor;
    }

    public MaestroService MaestroService() => new(this, this, new ServicioAuditoria(this, this, this));

    public EmpleadoService EmpleadoService() => new(this, this, this, this, new ServicioAuditoria(this, this, this));

    public GuardarEmpleadoRequest Solicitud(string numero = "1020304050", decimal? salario = 2_000_000m) => new(
        Cc.Id, numero, "Ana María", "Gómez", "Femenino", new DateOnly(1990, 5, 1), "3001234567", "Ana@Correo.com",
        "Calle 1", "Bogotá", Operario.Id, null, Indefinido.Id, new DateOnly(2024, 1, 15), null, salario);

    // --- IReloj / IUsuarioActual ---
    public DateTime AhoraUtc => Ahora;
    public DateOnly Hoy => DateOnly.FromDateTime(Ahora);
    public Guid? UsuarioId => null;
    public string? Nombre => "Pruebas";

    // --- IMaestroRepository ---
    private IEnumerable<ValorMem> Vigentes(string tabla) => Maestros[tabla].Where(v => v.EliminadoEn is null);

    private static ValorMaestro Lectura(ValorMem v) => new(v.Id, v.Codigo, v.Nombre, v.Activo, default);

    public Task<IReadOnlyList<ValorMaestro>> ListarAsync(string tabla, bool soloActivos, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ValorMaestro>>([.. Vigentes(tabla).Where(v => !soloActivos || v.Activo).Select(Lectura)]);

    public Task<ValorMaestro?> ObtenerAsync(string tabla, Guid id, CancellationToken ct = default) =>
        Task.FromResult(Vigentes(tabla).FirstOrDefault(v => v.Id == id) is { } v ? Lectura(v) : null);

    public Task<bool> ExisteCodigoAsync(string tabla, string codigo, Guid? excluirId, CancellationToken ct = default) =>
        Task.FromResult(Vigentes(tabla).Any(v => v.Id != excluirId && string.Equals(v.Codigo, codigo.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExisteNombreAsync(string tabla, string nombre, Guid? excluirId, CancellationToken ct = default) =>
        Task.FromResult(Vigentes(tabla).Any(v => v.Id != excluirId && string.Equals(v.Nombre, nombre.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task CrearAsync(string tabla, Guid id, string codigo, string nombre, bool activo, DateTime fecha, CancellationToken ct = default)
    {
        Maestros[tabla].Add(new ValorMem { Id = id, Codigo = codigo, Nombre = nombre, Activo = activo });
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(string tabla, Guid id, string codigo, string nombre, bool activo, DateTime fecha, CancellationToken ct = default)
    {
        var valor = Maestros[tabla].Single(v => v.Id == id);
        (valor.Codigo, valor.Nombre, valor.Activo) = (codigo, nombre, activo);
        return Task.CompletedTask;
    }

    public Task EliminarAsync(string tabla, Guid id, DateTime fecha, CancellationToken ct = default)
    {
        Maestros[tabla].Single(v => v.Id == id).EliminadoEn = fecha;
        return Task.CompletedTask;
    }

    // --- IEmpleadoRepository ---
    private ValorMem Valor(string clave, Guid id) => Maestros[RegistroMaestros.Obtener(clave).Tabla].Single(v => v.Id == id);

    private EmpleadoFila Fila(Empleado e) => new(
        e.Id, e.TipoIdentificacionId, Valor(RegistroMaestros.TiposIdentificacion, e.TipoIdentificacionId).Codigo,
        e.NumeroIdentificacion, e.Nombres, e.Apellidos, e.Genero, e.FechaNacimiento, e.Telefono, e.Email, e.Direccion,
        e.Ciudad, e.CargoId, Valor(RegistroMaestros.Cargos, e.CargoId).Nombre, e.AreaId,
        e.AreaId is { } a ? Valor(RegistroMaestros.Areas, a).Nombre : null, e.TipoContratoId,
        Valor(RegistroMaestros.TiposContrato, e.TipoContratoId).Nombre, e.FechaIngreso, e.FechaRetiro, e.Salario,
        e.Activo, e.FotoClave, e.CreadoEn);

    public Task<IReadOnlyList<EmpleadoFila>> ListarAsync(string? busqueda, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<EmpleadoFila>>([.. Empleados.Values.Where(e => e.EliminadoEn is null).Select(Fila)]);

    public Task<EmpleadoFila?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Empleados.TryGetValue(id, out var e) && e.EliminadoEn is null ? Fila(e) : null);

    public Task<bool> ExisteIdentificacionAsync(Guid tipoIdentificacionId, string numero, Guid? excluirId, CancellationToken ct = default) =>
        Task.FromResult(Empleados.Values.Any(e =>
            e.EliminadoEn is null && e.Id != excluirId && e.TipoIdentificacionId == tipoIdentificacionId && e.NumeroIdentificacion == numero));

    public Task CrearAsync(Empleado empleado, CancellationToken ct = default)
    {
        Empleados[empleado.Id] = empleado;
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(Empleado empleado, bool actualizarSalario, CancellationToken ct = default)
    {
        var actual = Empleados[empleado.Id];
        if (!actualizarSalario)
        {
            empleado.Salario = actual.Salario;
        }

        empleado.FotoClave = actual.FotoClave;
        Empleados[empleado.Id] = empleado;
        return Task.CompletedTask;
    }

    public Task CambiarEstadoAsync(Guid id, bool activo, DateTime fecha, CancellationToken ct = default)
    {
        Empleados[id].Activo = activo;
        return Task.CompletedTask;
    }

    public Task CambiarFotoAsync(Guid id, string? fotoClave, DateTime fecha, CancellationToken ct = default)
    {
        Empleados[id].FotoClave = fotoClave;
        return Task.CompletedTask;
    }

    public Task EliminarAsync(Guid id, DateTime fecha, CancellationToken ct = default)
    {
        Empleados[id].EliminadoEn = fecha;
        return Task.CompletedTask;
    }

    // --- IAlmacenArchivos ---
    public async Task<ArchivoSubido> SubirAsync(string clave, Stream contenido, string tipoMime, CancellationToken ct = default)
    {
        using var copia = new MemoryStream();
        await contenido.CopyToAsync(copia, ct);
        Archivos[clave] = (copia.ToArray(), tipoMime);
        return new ArchivoSubido(clave, copia.Length, "");
    }

    public Task<ArchivoDescargado?> DescargarAsync(string clave, CancellationToken ct = default) =>
        Task.FromResult(Archivos.TryGetValue(clave, out var a)
            ? new ArchivoDescargado(new MemoryStream(a.Datos), a.Tipo, a.Datos.Length)
            : null);

    public Task EliminarAsync(string clave, CancellationToken ct = default)
    {
        Archivos.Remove(clave);
        return Task.CompletedTask;
    }

    // --- IAuditoriaRepository ---
    public Task RegistrarAsync(RegistroAuditoria registro, CancellationToken ct = default)
    {
        Auditoria.Add(registro);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RegistroAuditoriaLectura>> ListarPorEntidadAsync(
        string entidad, Guid entidadId, int limite, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<RegistroAuditoriaLectura>>([.. Auditoria
            .Where(a => a.Entidad == entidad && a.EntidadId == entidadId)
            .Select(a => new RegistroAuditoriaLectura(a.Id, a.Entidad, a.EntidadId, a.Accion.ToString(), a.CambiosJson, a.UsuarioId, a.UsuarioNombre, a.FechaUtc))]);
}
