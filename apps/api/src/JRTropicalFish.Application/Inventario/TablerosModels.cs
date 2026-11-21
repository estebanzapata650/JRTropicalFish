namespace JRTropicalFish.Application.Inventario;

// Tableros de inventario: bodega, proveedores, clientes y trazabilidad de lotes.
// Los *Fila se construyen por posición desde las funciones de la migración TablerosInventario.

/// <summary>
/// Tiempos de un lote calculados del kardex: aclimatación (stock sin acuario) y cuarentena (stock enfermo).
/// Se suman los periodos en que hubo al menos un pez en ese estado; «en curso» si todavía lo hay.
/// </summary>
public sealed record LoteTiemposFila(
    Guid LoteId,
    decimal DiasAclimatacion,
    bool AclimatacionEnCurso,
    decimal DiasCuarentena,
    bool CuarentenaEnCurso,
    int EpisodiosCuarentena,
    int Despachos,
    int Clientes,
    DateTime? PrimerDespacho,
    DateTime? UltimoDespacho);

public sealed record DespachoFila(
    DateTime Fecha,
    Guid LoteId,
    string LoteCodigo,
    Guid ProductoId,
    string ProductoCodigo,
    string NombreCientifico,
    Guid ClienteId,
    string ClienteNombre,
    Guid ComercialId,
    Guid PedidoId,
    string PedidoCodigo,
    string? AcuarioCodigo,
    int Cantidad);

public sealed record CompraPendienteFila(
    Guid PedidoId,
    string PedidoCodigo,
    Guid ProveedorId,
    string ProveedorNombre,
    DateOnly Fecha,
    DateOnly? FechaEstimadaLlegada,
    string Estado,
    Guid ProductoId,
    string ProductoCodigo,
    string NombreCientifico,
    string? CentroNombre,
    int Cantidad,
    int Recibida)
{
    public int Pendiente => Cantidad - Recibida;
}

public sealed record CumplimientoCompraFila(
    Guid PedidoId,
    string PedidoCodigo,
    Guid ProveedorId,
    string ProveedorNombre,
    DateOnly Fecha,
    string Estado,
    int Pedidas,
    int Recibidas,
    int RecibidasEnFull)
{
    /// <summary>«In full»: lo recibido hasta lo pedido, por línea, sobre lo pedido (nunca supera 100 %).</summary>
    public decimal Cumplimiento => Tableros.Porcentaje(RecibidasEnFull, Pedidas);
}

public sealed record PedidoClientePendienteFila(
    Guid PedidoId,
    string PedidoCodigo,
    Guid ClienteId,
    string ClienteNombre,
    Guid ComercialId,
    DateOnly Fecha,
    string Estado,
    Guid ProductoId,
    string ProductoCodigo,
    string NombreCientifico,
    int Cantidad,
    int Despachada,
    int StockSano)
{
    public int Pendiente => Cantidad - Despachada;
}

// --- Bodega ---

public sealed record EspecieEnAcuario(string ProductoCodigo, string NombreCientifico, int Cantidad);

public sealed record UbicacionAcuario(
    Guid AcuarioId, string AcuarioCodigo, string Sector, int Sanos, int Enfermos, IReadOnlyList<EspecieEnAcuario> Especies)
{
    public int Total => Sanos + Enfermos;
}

public sealed record TableroBodega(
    int Sanos,
    int Aclimatacion,
    int Enfermos,
    int AcuariosActivos,
    int AcuariosOcupados,
    int AcuariosConEnfermos,
    int LotesActivos,
    int LotesAntiguos,
    IReadOnlyList<InventarioProductoFila> Productos,
    IReadOnlyList<UbicacionAcuario> Ubicaciones,
    IReadOnlyList<ExistenciaFila> ExistenciasEnfermas)
{
    public int AcuariosVacios => AcuariosActivos - AcuariosOcupados;

    public decimal Ocupacion => Tableros.Porcentaje(AcuariosOcupados, AcuariosActivos);
}

// --- Proveedores ---

/// <param name="Lotes">Lotes que ingresaron en el periodo (recepciones y saldo inicial).</param>
/// <param name="MuertosLlegada">Llegaron muertos.</param>
/// <param name="MuertosDespues">Murieron ya en bodega (mortalidad registrada sobre esos lotes).</param>
public sealed record ProveedorResumen(
    Guid? ProveedorId,
    string ProveedorNombre,
    int Pedidos,
    int Pedidas,
    int RecibidasEnFull,
    int Lotes,
    int Recibidos,
    int MuertosLlegada,
    int MuertosDespues)
{
    public decimal Cumplimiento => Tableros.Porcentaje(RecibidasEnFull, Pedidas);

    public decimal PorcentajeMuertos => Tableros.Porcentaje(MuertosLlegada + MuertosDespues, Recibidos);
}

/// <param name="Semana">Lunes de la semana.</param>
public sealed record SemanaRecepcion(DateOnly Semana, int Recibidos, int Muertos)
{
    public decimal PorcentajeMuertos => Tableros.Porcentaje(Muertos, Recibidos);
}

public sealed record TableroProveedores(
    DateOnly Desde,
    DateOnly Hasta,
    int PedidosEnTransito,
    int UnidadesEnTransito,
    int CentrosEnTransito,
    int Pedidas,
    int RecibidasEnFull,
    int Recibidos,
    int Muertos,
    IReadOnlyList<CompraPendienteFila> EnTransito,
    IReadOnlyList<ProveedorResumen> Proveedores,
    IReadOnlyList<CumplimientoCompraFila> Pedidos,
    IReadOnlyList<SemanaRecepcion> Semanas)
{
    public decimal Cumplimiento => Tableros.Porcentaje(RecibidasEnFull, Pedidas);

    public decimal PorcentajeMuertos => Tableros.Porcentaje(Muertos, Recibidos);
}

// --- Clientes ---

/// <param name="Solicitadas">De los pedidos del periodo (sin borradores ni cancelados).</param>
/// <param name="DespachadasPedidos">Lo despachado de esos mismos pedidos (para el cumplimiento).</param>
/// <param name="DespachadoPeriodo">Todo lo que salió hacia el cliente en el periodo, sea del pedido que sea.</param>
public sealed record ClienteResumen(
    Guid ClienteId, string ClienteNombre, int Pedidos, int Solicitadas, int DespachadasPedidos, int DespachadoPeriodo)
{
    public decimal Cumplimiento => Tableros.Porcentaje(DespachadasPedidos, Solicitadas);
}

public sealed record EspecieVendida(Guid ProductoId, string ProductoCodigo, string NombreCientifico, int Cantidad, int Clientes);

public sealed record TableroClientes(
    DateOnly Desde,
    DateOnly Hasta,
    int PedidosPendientes,
    int UnidadesPendientes,
    int UnidadesDespachadas,
    int Solicitadas,
    int DespachadasPedidos,
    IReadOnlyList<PedidoClientePendienteFila> Pendientes,
    IReadOnlyList<ClienteResumen> Clientes,
    IReadOnlyList<EspecieVendida> Especies)
{
    public decimal Cumplimiento => Tableros.Porcentaje(DespachadasPedidos, Solicitadas);
}

// --- Trazabilidad ---

public sealed record LoteTrazable(LoteFila Lote, LoteTiemposFila Tiempos);

/// <summary>El recorrido completo de un lote: de dónde vino, dónde está, a quién se envió y su kardex.</summary>
public sealed record RecorridoLote(
    LoteFila Lote,
    LoteTiemposFila Tiempos,
    IReadOnlyList<ExistenciaFila> Existencias,
    IReadOnlyList<DespachoFila> Despachos,
    IReadOnlyList<MovimientoFila> Movimientos);

public static class Tableros
{
    /// <summary>Porcentaje con un decimal; 0 si no hay base.</summary>
    public static decimal Porcentaje(int parte, int total) => total > 0 ? Math.Round(parte * 100m / total, 1) : 0;
}
