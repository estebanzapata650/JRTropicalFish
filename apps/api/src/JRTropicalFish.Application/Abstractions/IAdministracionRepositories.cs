using JRTropicalFish.Application.Administracion;
using JRTropicalFish.Application.Maestros;
using JRTropicalFish.Domain.Administracion;

namespace JRTropicalFish.Application.Abstractions;

/// <summary>
/// Repositorio genérico de maestros. <c>tabla</c> sale siempre de <see cref="RegistroMaestros"/>,
/// nunca de la petición.
/// </summary>
public interface IMaestroRepository
{
    Task<IReadOnlyList<ValorMaestro>> ListarAsync(string tabla, bool soloActivos, CancellationToken ct = default);

    /// <summary>El valor, aunque esté inactivo; <c>null</c> si no existe o fue eliminado.</summary>
    Task<ValorMaestro?> ObtenerAsync(string tabla, Guid id, CancellationToken ct = default);

    Task<bool> ExisteCodigoAsync(string tabla, string codigo, Guid? excluirId, CancellationToken ct = default);

    Task<bool> ExisteNombreAsync(string tabla, string nombre, Guid? excluirId, CancellationToken ct = default);

    Task CrearAsync(string tabla, Guid id, string codigo, string nombre, bool activo, DateTime fecha, CancellationToken ct = default);

    Task ActualizarAsync(string tabla, Guid id, string codigo, string nombre, bool activo, DateTime fecha, CancellationToken ct = default);

    /// <summary>Borrado lógico.</summary>
    Task EliminarAsync(string tabla, Guid id, DateTime fecha, CancellationToken ct = default);
}

public interface IEmpleadoRepository
{
    Task<IReadOnlyList<EmpleadoFila>> ListarAsync(string? busqueda, CancellationToken ct = default);

    Task<EmpleadoFila?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);

    Task<bool> ExisteIdentificacionAsync(Guid tipoIdentificacionId, string numero, Guid? excluirId, CancellationToken ct = default);

    Task CrearAsync(Empleado empleado, CancellationToken ct = default);

    /// <param name="actualizarSalario">Si es false, se conserva el salario guardado (quien edita no puede verlo).</param>
    Task ActualizarAsync(Empleado empleado, bool actualizarSalario, CancellationToken ct = default);

    Task CambiarEstadoAsync(Guid id, bool activo, DateTime fecha, CancellationToken ct = default);

    Task CambiarFotoAsync(Guid id, string? fotoClave, DateTime fecha, CancellationToken ct = default);

    /// <summary>Borrado lógico.</summary>
    Task EliminarAsync(Guid id, DateTime fecha, CancellationToken ct = default);
}
