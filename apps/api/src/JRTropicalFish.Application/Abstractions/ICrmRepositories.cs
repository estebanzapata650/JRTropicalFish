using JRTropicalFish.Application.Crm;
using JRTropicalFish.Domain.Crm;

namespace JRTropicalFish.Application.Abstractions;

/// <summary>Consecutivos legibles por prefijo (<c>CLI</c>, <c>OPT</c>, <c>VTA</c>, <c>TRK</c>, <c>TAR</c>).</summary>
public interface IConsecutivoRepository
{
    /// <summary>Reserva el siguiente código (ej. <c>OPT-102</c>). Si la operación falla después, queda un hueco.</summary>
    Task<string> SiguienteAsync(string prefijo, CancellationToken ct = default);

    /// <summary>Sube el consecutivo a <paramref name="numero"/> si estaba por debajo (códigos importados).</summary>
    Task AjustarAsync(string prefijo, int numero, CancellationToken ct = default);
}

public interface IComercialRepository
{
    /// <summary>Usuarios activos con <c>crm:vender</c> en un perfil activo.</summary>
    Task<IReadOnlyList<ComercialInfo>> ListarAsync(CancellationToken ct = default);
}

public interface IClienteRepository
{
    Task<IReadOnlyList<ClienteFila>> ListarAsync(string? busqueda, CancellationToken ct = default);

    Task<ClienteFila?> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task<bool> ExisteCodigoAsync(string codigo, Guid? excluirId, CancellationToken ct = default);

    Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId, CancellationToken ct = default);

    Task<bool> TieneMovimientosAsync(Guid id, CancellationToken ct = default);

    Task CrearAsync(Cliente cliente, CancellationToken ct = default);

    Task ActualizarAsync(Cliente cliente, CancellationToken ct = default);

    Task EliminarAsync(Guid id, DateTime fecha, CancellationToken ct = default);
}

public interface IOportunidadRepository
{
    Task<IReadOnlyList<OportunidadFila>> ListarAsync(Guid? comercialId, Guid? clienteId, CancellationToken ct = default);

    Task<OportunidadFila?> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task CrearAsync(Oportunidad oportunidad, CancellationToken ct = default);

    Task ActualizarAsync(Oportunidad oportunidad, CancellationToken ct = default);

    /// <returns><c>false</c> si la etapa ya no era <paramref name="etapaEsperada"/> (otro la cambió).</returns>
    Task<bool> CambiarEtapaAsync(
        Guid id, string etapaEsperada, string etapa, string? motivoPerdida, DateTime fecha, CancellationToken ct = default);

    /// <summary>Cierra como ganada y crea la venta (y el tracking si se indica) en una transacción.</summary>
    /// <returns><c>false</c> si la etapa ya no era <paramref name="etapaEsperada"/>.</returns>
    Task<bool> GanarAsync(
        Guid id,
        string etapaEsperada,
        DateOnly fechaPagoConfirmado,
        Creado venta,
        string origenCanal,
        decimal? trm,
        string estadoPago,
        Creado? tracking,
        DateTime fecha,
        CancellationToken ct = default);

    Task EliminarAsync(Guid id, DateTime fecha, CancellationToken ct = default);
}

public interface IVentaRepository
{
    Task<IReadOnlyList<VentaFila>> ListarAsync(
        Guid? comercialId, Guid? clienteId, DateOnly? desde, DateOnly? hasta, CancellationToken ct = default);

    Task<VentaFila?> ObtenerAsync(Guid id, CancellationToken ct = default);

    /// <param name="tracking">Si es exportación, el tracking que nace con la venta.</param>
    Task CrearAsync(Venta venta, Creado? tracking, CancellationToken ct = default);

    Task ActualizarAsync(Venta venta, CancellationToken ct = default);
}

public interface ITrackingRepository
{
    Task<IReadOnlyList<TrackingFila>> ListarAsync(Guid? comercialId, Guid? clienteId, CancellationToken ct = default);

    Task<TrackingFila?> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task ActualizarAsync(TrackingVuelo tracking, CancellationToken ct = default);

    /// <returns><c>false</c> si la fase ya no era <paramref name="faseEsperada"/>.</returns>
    Task<bool> CambiarFaseAsync(Guid id, string faseEsperada, string fase, DateTime fecha, CancellationToken ct = default);
}

public interface ITareaRepository
{
    Task<IReadOnlyList<TareaFila>> ListarAsync(DateOnly hoy, Guid? comercialId, Guid? clienteId, CancellationToken ct = default);

    Task<TareaFila?> ObtenerAsync(Guid id, DateOnly hoy, CancellationToken ct = default);

    Task CrearAsync(Tarea tarea, CancellationToken ct = default);

    Task ActualizarAsync(Tarea tarea, CancellationToken ct = default);

    /// <returns><c>false</c> si el estado ya no era <paramref name="estadoEsperado"/>.</returns>
    Task<bool> CambiarEstadoAsync(
        Guid id, string estadoEsperado, string estado, DateTime? completadaEn, DateTime fecha, CancellationToken ct = default);

    Task EliminarAsync(Guid id, DateTime fecha, CancellationToken ct = default);
}

public interface IMetricasRepository
{
    Task<ParametrosComercialesInfo> ObtenerParametrosAsync(CancellationToken ct = default);

    Task ActualizarParametrosAsync(GuardarParametrosRequest parametros, DateTime fecha, CancellationToken ct = default);

    Task<IReadOnlyList<MetaMesFila>> MetasDelMesAsync(DateOnly mes, CancellationToken ct = default);

    Task GuardarMetaAsync(Guid id, DateOnly mes, Guid comercialId, decimal metaCop, DateTime fecha, CancellationToken ct = default);

    Task<IReadOnlyList<VentasComercialFila>> VentasPorComercialAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default);

    Task<IReadOnlyList<EmbudoFila>> EmbudoAsync(DateTime desdeUtc, DateTime hastaUtc, Guid? comercialId, CancellationToken ct = default);

    Task<ResumenPeriodoFila> ResumenPeriodoAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default);
}
