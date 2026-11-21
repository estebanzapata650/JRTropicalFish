using JRTropicalFish.Application.Inventario;
using JRTropicalFish.Domain.Inventario;

namespace JRTropicalFish.Application.Abstractions;

/// <summary>
/// Error de negocio que detectó la base al escribir (SQLSTATE propio): <c>JR001</c> cambió mientras
/// tanto, <c>JR002</c> no hay stock suficiente, <c>JR003</c> supera lo pendiente de la línea.
/// </summary>
public sealed record FallaBaseDatos(string SqlState, string Mensaje);

/// <summary>Quién hace la operación (queda en cada movimiento del kardex).</summary>
public sealed record Operador(Guid? UsuarioId, string? Nombre);

public interface IInventarioRepository
{
    Task<IReadOnlyList<AcuarioFila>> AcuariosAsync(CancellationToken ct = default);

    Task<AcuarioFila?> AcuarioAsync(Guid id, CancellationToken ct = default);

    Task<bool> ExisteAcuarioAsync(string codigo, Guid? excluirId, CancellationToken ct = default);

    Task GuardarAcuarioAsync(Acuario acuario, bool esNuevo, DateTime fecha, CancellationToken ct = default);

    Task<IReadOnlyList<ExistenciaFila>> ExistenciasAsync(
        DateOnly hoy, Guid? productoId, Guid? loteId, Guid? acuarioId, CancellationToken ct = default);

    Task<IReadOnlyList<LoteFila>> LotesAsync(DateOnly hoy, Guid? productoId, bool soloConStock, CancellationToken ct = default);

    Task<LoteFila?> LoteAsync(Guid id, DateOnly hoy, CancellationToken ct = default);

    Task<bool> ExisteLoteAsync(string codigo, CancellationToken ct = default);

    Task<IReadOnlyList<MovimientoFila>> MovimientosAsync(Guid? loteId, int limite, CancellationToken ct = default);

    /// <summary>Aplica las operaciones en una transacción (todo o nada).</summary>
    Task<FallaBaseDatos?> MoverAsync(IReadOnlyList<OperacionStock> operaciones, Operador operador, DateTime fecha, CancellationToken ct = default);

    /// <summary>Crea un lote del saldo inicial y su stock (movimientos <c>saldo_inicial</c>) en una transacción.</summary>
    Task<FallaBaseDatos?> CrearSaldoInicialAsync(
        Lote lote, IReadOnlyList<(Guid? AcuarioId, string Condicion, int Cantidad)> existencias, Operador operador, DateTime fecha,
        CancellationToken ct = default);

    Task<IReadOnlyList<InventarioProductoFila>> PorProductoAsync(CancellationToken ct = default);
}

/// <summary>Lecturas de los tableros de inventario (funciones de la migración TablerosInventario).</summary>
public interface ITablerosInventarioRepository
{
    /// <summary>Tiempos en aclimatación y cuarentena; <paramref name="loteId"/> nulo = todos los lotes.</summary>
    Task<IReadOnlyList<LoteTiemposFila>> TiemposAsync(Guid? loteId, DateTime ahora, CancellationToken ct = default);

    Task<IReadOnlyList<DespachoFila>> DespachosAsync(DateTime? desde, DateTime? hasta, Guid? loteId, CancellationToken ct = default);

    Task<IReadOnlyList<CompraPendienteFila>> ComprasPendientesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<CumplimientoCompraFila>> CumplimientoComprasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default);

    Task<IReadOnlyList<PedidoClientePendienteFila>> PedidosClientePendientesAsync(CancellationToken ct = default);
}

public interface IComprasRepository
{
    Task<IReadOnlyList<OfertaFila>> OfertasAsync(CancellationToken ct = default);

    Task<OfertaFila?> OfertaAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<OfertaLineaFila>> OfertaLineasAsync(Guid ofertaId, CancellationToken ct = default);

    Task GuardarOfertaAsync(OfertaProveedor oferta, bool esNueva, IReadOnlyList<LineaCantidad> lineas, DateTime fecha, CancellationToken ct = default);

    Task<IReadOnlyList<PedidoProveedorFila>> PedidosAsync(CancellationToken ct = default);

    Task<PedidoProveedorFila?> PedidoAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<PedidoProveedorLineaFila>> PedidoLineasAsync(Guid pedidoId, CancellationToken ct = default);

    Task GuardarPedidoAsync(
        PedidoProveedor pedido, bool esNuevo, IReadOnlyList<LineaPedidoProveedor> lineas, bool actualizarCostos, DateTime fecha,
        CancellationToken ct = default);

    Task<FallaBaseDatos?> CambiarEstadoPedidoAsync(Guid id, string esperado, string estado, DateTime fecha, CancellationToken ct = default);

    Task<IReadOnlyList<RecepcionFila>> RecepcionesAsync(Guid? pedidoId, CancellationToken ct = default);

    Task<IReadOnlyList<RecepcionLineaFila>> RecepcionLineasAsync(Guid recepcionId, CancellationToken ct = default);

    /// <param name="lineas">Por línea: ids nuevos de la línea y del lote y el código del lote.</param>
    Task<FallaBaseDatos?> RegistrarRecepcionAsync(
        Recepcion recepcion,
        string estadoEsperado,
        IReadOnlyList<(Guid Id, Guid PedidoLineaId, Guid ProductoId, int Recibida, int Enferma, int Muerta, decimal Costo, Guid LoteId, string LoteCodigo)> lineas,
        Operador operador,
        DateTime fecha,
        CancellationToken ct = default);
}

public interface IPedidoClienteRepository
{
    Task<IReadOnlyList<PedidoClienteFila>> ListarAsync(Guid? comercialId, CancellationToken ct = default);

    Task<PedidoClienteFila?> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task<PedidoClienteFila?> PorVentaAsync(Guid ventaId, CancellationToken ct = default);

    Task<IReadOnlyList<PedidoClienteLineaFila>> LineasAsync(Guid pedidoId, CancellationToken ct = default);

    Task GuardarAsync(PedidoCliente pedido, bool esNuevo, IReadOnlyList<LineaPedidoCliente> lineas, DateTime fecha, CancellationToken ct = default);

    Task<FallaBaseDatos?> CambiarEstadoAsync(Guid id, string esperado, string estado, DateTime fecha, CancellationToken ct = default);

    Task<FallaBaseDatos?> DespacharAsync(Guid id, IReadOnlyList<SalidaDespacho> salidas, Operador operador, DateTime fecha, CancellationToken ct = default);
}
