namespace JRTropicalFish.Application.Inventario;

// Los records *Fila salen de las funciones fn_* de la migración FuncionesInventario (Dapper construye
// por posición: el orden de los campos = el orden de columnas del RETURNS TABLE).

// --- Acuarios ---

public sealed record AcuarioFila(
    Guid Id, string Codigo, string Sector, int Nivel, int Posicion, decimal? CapacidadLitros, bool Activo, int Sanos, int Enfermos,
    int Lotes);

public sealed record GuardarAcuarioRequest(string? Codigo, string? Sector, int Nivel, int Posicion, decimal? CapacidadLitros, bool Activo);

public sealed record AcuarioAuditable(string Codigo, string Sector, int Nivel, int Posicion, decimal? CapacidadLitros, bool Activo);

// --- Existencias, lotes y kardex ---

public sealed record ExistenciaFila(
    Guid Id,
    Guid LoteId,
    string LoteCodigo,
    Guid ProductoId,
    string ProductoCodigo,
    string NombreCientifico,
    string? NombreEspanol,
    Guid? AcuarioId,
    string? AcuarioCodigo,
    string Condicion,
    int Cantidad,
    DateOnly FechaIngreso,
    int DiasEnBodega,
    string? ProveedorNombre,
    string? CentroNombre);

public sealed record LoteFila(
    Guid Id,
    string Codigo,
    Guid ProductoId,
    string ProductoCodigo,
    string NombreCientifico,
    string? NombreEspanol,
    Guid? ProveedorId,
    string? ProveedorNombre,
    Guid? CentroAcopioId,
    string? CentroNombre,
    string? RecepcionCodigo,
    DateOnly FechaIngreso,
    int DiasEnBodega,
    decimal? CostoUnitario,
    int Recibidos,
    int MuertosLlegada,
    string Origen,
    int Sanos,
    int Enfermos,
    int PorUbicar,
    int Despachados,
    int Muertos,
    DateTime CreadoEn)
{
    /// <summary>Lo que queda del lote (sano ubicado + por ubicar + enfermo).</summary>
    public int Disponible => Sanos + PorUbicar + Enfermos;

    /// <summary>Muertos sobre recibidos (incluye los que llegaron muertos).</summary>
    public decimal PorcentajeMuertos => Recibidos > 0 ? Math.Round(Muertos * 100m / Recibidos, 1) : 0;
}

public sealed record MovimientoFila(
    Guid Id,
    string Tipo,
    Guid LoteId,
    string LoteCodigo,
    string ProductoCodigo,
    string NombreCientifico,
    string? AcuarioOrigenCodigo,
    string? CondicionOrigen,
    string? AcuarioDestinoCodigo,
    string? CondicionDestino,
    int Cantidad,
    string? PedidoCodigo,
    string? RecepcionCodigo,
    string? Nota,
    string? UsuarioNombre,
    DateTime Fecha);

/// <summary>
/// Una operación de stock sobre un lote. Origen o destino nulos según el tipo (ver <c>ReglasStock</c>):
/// ubicar = (por ubicar, sano) → (acuario, sano); enfermo = (acuario, sano) → (acuario, enfermo)…
/// </summary>
public sealed record OperacionStock(
    string? Tipo,
    Guid LoteId,
    Guid? AcuarioOrigenId,
    string? CondicionOrigen,
    Guid? AcuarioDestinoId,
    string? CondicionDestino,
    int Cantidad,
    string? Nota);

public sealed record OperacionesStockRequest(IReadOnlyList<OperacionStock>? Operaciones);

/// <summary>Stock inicial de un lote migrado (por acuario y condición). Acuario nulo = por ubicar.</summary>
public sealed record SaldoInicialExistencia(string? AcuarioCodigo, string? Condicion, int Cantidad);

public sealed record SaldoInicialRequest(
    string? Codigo,
    Guid ProductoId,
    Guid? ProveedorId,
    Guid? CentroAcopioId,
    DateOnly? FechaIngreso,
    decimal? CostoUnitario,
    int Recibidos,
    int Muertos,
    IReadOnlyList<SaldoInicialExistencia>? Existencias);

// --- Compras: ofertas, pedidos a proveedor, recepciones ---

public sealed record OfertaFila(
    Guid Id,
    string Codigo,
    Guid ProveedorId,
    string ProveedorNombre,
    DateOnly Fecha,
    DateOnly? FechaEstimadaLlegada,
    string Estado,
    string? Observaciones,
    int Lineas,
    int Unidades,
    DateTime CreadoEn);

public sealed record OfertaLineaFila(
    Guid Id, Guid OfertaId, Guid ProductoId, string ProductoCodigo, string NombreCientifico, string? NombreEspanol, int Cantidad);

public sealed record OfertaDetalle(OfertaFila Oferta, IReadOnlyList<OfertaLineaFila> Lineas);

public sealed record LineaCantidad(Guid ProductoId, int Cantidad);

public sealed record GuardarOfertaRequest(
    Guid ProveedorId,
    DateOnly? Fecha,
    DateOnly? FechaEstimadaLlegada,
    string? Estado,
    string? Observaciones,
    IReadOnlyList<LineaCantidad>? Lineas);

public sealed record PedidoProveedorFila(
    Guid Id,
    string Codigo,
    Guid ProveedorId,
    string ProveedorNombre,
    Guid? OfertaId,
    string? OfertaCodigo,
    DateOnly Fecha,
    DateOnly? FechaEstimadaLlegada,
    string Estado,
    string? Observaciones,
    int Unidades,
    int Recibidas,
    decimal? CostoTotal,
    DateTime CreadoEn)
{
    /// <summary>Cumplimiento del proveedor: recibido sobre pedido (%).</summary>
    public decimal Cumplimiento => Unidades > 0 ? Math.Round(Recibidas * 100m / Unidades, 1) : 0;
}

public sealed record PedidoProveedorLineaFila(
    Guid Id,
    Guid PedidoId,
    Guid ProductoId,
    string ProductoCodigo,
    string NombreCientifico,
    string? NombreEspanol,
    int Cantidad,
    decimal? CostoUnitario,
    string? Observacion,
    int Recibida)
{
    public int Pendiente => Math.Max(0, Cantidad - Recibida);
}

public sealed record PedidoProveedorDetalle(
    PedidoProveedorFila Pedido, IReadOnlyList<PedidoProveedorLineaFila> Lineas, IReadOnlyList<RecepcionFila> Recepciones);

public sealed record LineaPedidoProveedor(Guid ProductoId, int Cantidad, decimal? CostoUnitario, string? Observacion);

public sealed record GuardarPedidoProveedorRequest(
    Guid ProveedorId,
    Guid? OfertaId,
    DateOnly? Fecha,
    DateOnly? FechaEstimadaLlegada,
    string? Observaciones,
    IReadOnlyList<LineaPedidoProveedor>? Lineas);

public sealed record CambiarEstadoRequest(string? EstadoActual, string? Estado);

public sealed record RecepcionFila(
    Guid Id,
    string Codigo,
    Guid PedidoProveedorId,
    string PedidoCodigo,
    string ProveedorNombre,
    DateOnly Fecha,
    string? Observaciones,
    string? UsuarioNombre,
    int Unidades,
    int Enfermas,
    int Muertas,
    decimal CostoTotal,
    DateTime CreadoEn);

public sealed record RecepcionLineaFila(
    Guid Id,
    Guid RecepcionId,
    Guid PedidoLineaId,
    Guid ProductoId,
    string ProductoCodigo,
    string NombreCientifico,
    int Recibida,
    int Enferma,
    int Muerta,
    decimal CostoUnitario,
    decimal CostoTotal,
    Guid LoteId,
    string LoteCodigo);

public sealed record RecepcionDetalle(RecepcionFila Recepcion, IReadOnlyList<RecepcionLineaFila> Lineas);

public sealed record LineaRecepcion(Guid PedidoLineaId, int Recibida, int Enferma, int Muerta, decimal? CostoUnitario);

public sealed record RegistrarRecepcionRequest(
    Guid PedidoProveedorId,
    string? EstadoPedido,
    DateOnly? Fecha,
    string? Observaciones,
    IReadOnlyList<LineaRecepcion>? Lineas);

/// <summary>Lo creado por una recepción. <see cref="Avisos"/>: líneas que llegaron por encima de lo pedido.</summary>
public sealed record RecepcionRegistrada(Guid Id, string Codigo, IReadOnlyList<string> Lotes, IReadOnlyList<string> Avisos);

// --- Pedidos de cliente ---

public sealed record PedidoClienteFila(
    Guid Id,
    string Codigo,
    Guid ClienteId,
    string ClienteNombre,
    Guid ComercialId,
    string ComercialNombre,
    Guid? VentaId,
    string? VentaCodigo,
    string? Moneda,
    DateOnly Fecha,
    string? Ciudad,
    string Estado,
    string? Observaciones,
    int Unidades,
    int Despachadas,
    decimal? Total,
    DateTime CreadoEn)
{
    /// <summary>Cumplimiento con el cliente: despachado sobre solicitado (%).</summary>
    public decimal Cumplimiento => Unidades > 0 ? Math.Round(Despachadas * 100m / Unidades, 1) : 0;
}

public sealed record PedidoClienteLineaFila(
    Guid Id,
    Guid PedidoId,
    Guid ProductoId,
    string ProductoCodigo,
    string NombreCientifico,
    string? NombreEspanol,
    int Cantidad,
    decimal? PrecioUnitario,
    string? Observacion,
    int Despachada,
    string? Talla)
{
    public int Pendiente => Math.Max(0, Cantidad - Despachada);
}

public sealed record PedidoClienteDetalle(PedidoClienteFila Pedido, IReadOnlyList<PedidoClienteLineaFila> Lineas);

/// <param name="Id">De la línea existente al editar; nulo para una nueva.</param>
public sealed record LineaPedidoCliente(Guid? Id, Guid ProductoId, int Cantidad, decimal? PrecioUnitario, string? Observacion);

/// <param name="VentaId">Al crear: la venta del CRM de la que nace (hereda cliente y comercial).</param>
public sealed record GuardarPedidoClienteRequest(
    Guid? VentaId,
    Guid? ClienteId,
    Guid? ComercialId,
    DateOnly? Fecha,
    string? Ciudad,
    string? Observaciones,
    IReadOnlyList<LineaPedidoCliente>? Lineas);

public sealed record SalidaDespacho(Guid LineaId, Guid LoteId, Guid AcuarioId, int Cantidad);

public sealed record DespacharRequest(IReadOnlyList<SalidaDespacho>? Salidas);

public sealed record PedidoClienteAuditable(
    string Cliente, string Comercial, string? Venta, DateOnly Fecha, string? Ciudad, string Estado, string? Observaciones, string Lineas);

// --- Resúmenes: necesidades y dashboard ---

public sealed record InventarioProductoFila(
    Guid ProductoId,
    string ProductoCodigo,
    string NombreCientifico,
    string? NombreEspanol,
    Guid? CentroAcopioId,
    string? CentroNombre,
    int Sanos,
    int PorUbicar,
    int Enfermos,
    int PendienteClientes,
    int EnTransito,
    string? UltimaOfertaProveedor,
    int? UltimaOfertaCantidad,
    DateOnly? UltimaOfertaFecha)
{
    /// <summary>Lo que falta comprar: demanda pendiente − stock vendible − lo que ya viene en camino.</summary>
    public int Faltante => Math.Max(0, PendienteClientes - Sanos - PorUbicar - EnTransito);

    /// <summary>"Ya pedido": la demanda queda cubierta por stock y pedidos a proveedor en tránsito.</summary>
    public bool Cubierto => PendienteClientes > 0 && Faltante == 0;
}

