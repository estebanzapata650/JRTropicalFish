using System.Text.Json;
using Dapper;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Inventario;
using JRTropicalFish.Domain.Inventario;
using JRTropicalFish.Infrastructure.Persistence;
using Npgsql;

namespace JRTropicalFish.Infrastructure.Repositories;

/// <summary>Base de inventario: traduce los SQLSTATE propios (JR001/JR002/JR003) a <see cref="FallaBaseDatos"/>.</summary>
public abstract class RepositorioInventario(PostgresConnectionFactory conexiones) : RepositorioCrm(conexiones)
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected async Task<FallaBaseDatos?> EjecutarConFallaAsync(string sql, object parametros, CancellationToken ct)
    {
        try
        {
            await EjecutarAsync(sql, parametros, ct);
            return null;
        }
        catch (PostgresException ex) when (ex.SqlState.StartsWith("JR", StringComparison.Ordinal))
        {
            return new FallaBaseDatos(ex.SqlState, ex.MessageText);
        }
    }
}

public class InventarioRepository(PostgresConnectionFactory conexiones) : RepositorioInventario(conexiones), IInventarioRepository
{
    public Task<IReadOnlyList<AcuarioFila>> AcuariosAsync(CancellationToken ct = default) =>
        ListarAsync<AcuarioFila>("SELECT * FROM fn_acuarios_listar()", null, ct);

    public Task<AcuarioFila?> AcuarioAsync(Guid id, CancellationToken ct = default) =>
        UnoAsync<AcuarioFila>("SELECT * FROM fn_acuario_por_id(@Id::uuid)", new { Id = id }, ct);

    public Task<bool> ExisteAcuarioAsync(string codigo, Guid? excluirId, CancellationToken ct = default) =>
        EscalarAsync<bool>("SELECT fn_acuario_codigo_existe(@Codigo::text, @ExcluirId::uuid)", new { Codigo = codigo, ExcluirId = excluirId }, ct);

    public Task GuardarAcuarioAsync(Acuario a, bool esNuevo, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_acuario_guardar(@Id::uuid, @EsNuevo::boolean, @Codigo::text, @Sector::text, @Nivel::integer, @Posicion::integer,
                                    @CapacidadLitros::numeric, @Activo::boolean, @Fecha::timestamptz)
            """,
            new { a.Id, EsNuevo = esNuevo, a.Codigo, a.Sector, a.Nivel, a.Posicion, a.CapacidadLitros, a.Activo, Fecha = fecha }, ct);

    public Task<IReadOnlyList<ExistenciaFila>> ExistenciasAsync(
        DateOnly hoy, Guid? productoId, Guid? loteId, Guid? acuarioId, CancellationToken ct = default) =>
        ListarAsync<ExistenciaFila>(
            "SELECT * FROM fn_existencias_listar(@Hoy::date, @ProductoId::uuid, @LoteId::uuid, @AcuarioId::uuid)",
            new { Hoy = hoy, ProductoId = productoId, LoteId = loteId, AcuarioId = acuarioId }, ct);

    public Task<IReadOnlyList<LoteFila>> LotesAsync(DateOnly hoy, Guid? productoId, bool soloConStock, CancellationToken ct = default) =>
        ListarAsync<LoteFila>(
            "SELECT * FROM fn_lotes_listar(@Hoy::date, @ProductoId::uuid, @SoloConStock::boolean)",
            new { Hoy = hoy, ProductoId = productoId, SoloConStock = soloConStock }, ct);

    public Task<LoteFila?> LoteAsync(Guid id, DateOnly hoy, CancellationToken ct = default) =>
        UnoAsync<LoteFila>("SELECT * FROM fn_lote_por_id(@Id::uuid, @Hoy::date)", new { Id = id, Hoy = hoy }, ct);

    public Task<bool> ExisteLoteAsync(string codigo, CancellationToken ct = default) =>
        EscalarAsync<bool>("SELECT fn_lote_codigo_existe(@Codigo::text)", new { Codigo = codigo }, ct);

    public Task<IReadOnlyList<MovimientoFila>> MovimientosAsync(Guid? loteId, int limite, CancellationToken ct = default) =>
        ListarAsync<MovimientoFila>("SELECT * FROM fn_movimientos_listar(@LoteId::uuid, @Limite::integer)", new { LoteId = loteId, Limite = limite }, ct);

    public Task<FallaBaseDatos?> MoverAsync(IReadOnlyList<OperacionStock> operaciones, Operador operador, DateTime fecha, CancellationToken ct = default) =>
        EjecutarConFallaAsync(
            "CALL sp_inventario_mover(@Movimientos::jsonb, @UsuarioId::uuid, @UsuarioNombre::text, @Fecha::timestamptz)",
            new
            {
                Movimientos = JsonSerializer.Serialize(operaciones.Select(o => new
                {
                    lote = o.LoteId,
                    origenAcuario = o.AcuarioOrigenId,
                    origenCondicion = o.CondicionOrigen,
                    destinoAcuario = o.AcuarioDestinoId,
                    destinoCondicion = o.CondicionDestino,
                    cantidad = o.Cantidad,
                    tipo = o.Tipo,
                    nota = o.Nota,
                })),
                operador.UsuarioId,
                UsuarioNombre = operador.Nombre,
                Fecha = fecha,
            },
            ct);

    /// <summary>El lote y sus entradas en una transacción explícita: quedan juntos o no quedan.</summary>
    public async Task<FallaBaseDatos?> CrearSaldoInicialAsync(
        Lote l, IReadOnlyList<(Guid? AcuarioId, string Condicion, int Cantidad)> existencias, Operador operador, DateTime fecha,
        CancellationToken ct = default)
    {
        await using var connection = await AbrirConexionAsync(ct);
        await using var transaccion = await connection.BeginTransactionAsync(ct);
        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                """
                CALL sp_lote_crear(@Id::uuid, @Codigo::text, @ProductoId::uuid, @ProveedorId::uuid, @CentroAcopioId::uuid, NULL,
                                   @FechaIngreso::date, @CostoUnitario::numeric, @Recibidos::integer, @MuertosLlegada::integer,
                                   'saldo_inicial', @Fecha::timestamptz)
                """,
                new
                {
                    l.Id, l.Codigo, l.ProductoId, l.ProveedorId, l.CentroAcopioId, l.FechaIngreso, l.CostoUnitario, l.Recibidos,
                    l.MuertosLlegada, Fecha = fecha,
                },
                transaccion, cancellationToken: ct));

            if (existencias.Count > 0)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    "CALL sp_inventario_mover(@Movimientos::jsonb, @UsuarioId::uuid, @UsuarioNombre::text, @Fecha::timestamptz)",
                    new
                    {
                        Movimientos = JsonSerializer.Serialize(existencias.Select(e => new
                        {
                            lote = l.Id,
                            destinoAcuario = e.AcuarioId,
                            destinoCondicion = e.Condicion,
                            cantidad = e.Cantidad,
                            tipo = TiposMovimiento.SaldoInicial,
                            nota = "Saldo inicial migrado de la app de inventario",
                        })),
                        operador.UsuarioId,
                        UsuarioNombre = operador.Nombre,
                        Fecha = fecha,
                    },
                    transaccion, cancellationToken: ct));
            }

            await transaccion.CommitAsync(ct);
            return null;
        }
        catch (PostgresException ex) when (ex.SqlState.StartsWith("JR", StringComparison.Ordinal))
        {
            return new FallaBaseDatos(ex.SqlState, ex.MessageText);
        }
    }

    public Task<IReadOnlyList<InventarioProductoFila>> PorProductoAsync(CancellationToken ct = default) =>
        ListarAsync<InventarioProductoFila>("SELECT * FROM fn_inventario_por_producto()", null, ct);
}

public class TablerosInventarioRepository(PostgresConnectionFactory conexiones) : RepositorioInventario(conexiones), ITablerosInventarioRepository
{
    public Task<IReadOnlyList<LoteTiemposFila>> TiemposAsync(Guid? loteId, DateTime ahora, CancellationToken ct = default) =>
        ListarAsync<LoteTiemposFila>("SELECT * FROM fn_lotes_tiempos(@LoteId::uuid, @Ahora::timestamptz)", new { LoteId = loteId, Ahora = ahora }, ct);

    public Task<IReadOnlyList<DespachoFila>> DespachosAsync(DateTime? desde, DateTime? hasta, Guid? loteId, CancellationToken ct = default) =>
        ListarAsync<DespachoFila>(
            "SELECT * FROM fn_despachos_listar(@Desde::timestamptz, @Hasta::timestamptz, @LoteId::uuid)",
            new { Desde = desde, Hasta = hasta, LoteId = loteId }, ct);

    public Task<IReadOnlyList<CompraPendienteFila>> ComprasPendientesAsync(CancellationToken ct = default) =>
        ListarAsync<CompraPendienteFila>("SELECT * FROM fn_compras_lineas_pendientes()", null, ct);

    public Task<IReadOnlyList<CumplimientoCompraFila>> CumplimientoComprasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default) =>
        ListarAsync<CumplimientoCompraFila>(
            "SELECT * FROM fn_compras_cumplimiento(@Desde::date, @Hasta::date)", new { Desde = desde, Hasta = hasta }, ct);

    public Task<IReadOnlyList<PedidoClientePendienteFila>> PedidosClientePendientesAsync(CancellationToken ct = default) =>
        ListarAsync<PedidoClientePendienteFila>("SELECT * FROM fn_pedidos_cliente_lineas_pendientes()", null, ct);
}

public class ComprasRepository(PostgresConnectionFactory conexiones) : RepositorioInventario(conexiones), IComprasRepository
{
    public Task<IReadOnlyList<OfertaFila>> OfertasAsync(CancellationToken ct = default) =>
        ListarAsync<OfertaFila>("SELECT * FROM fn_ofertas_listar()", null, ct);

    public Task<OfertaFila?> OfertaAsync(Guid id, CancellationToken ct = default) =>
        UnoAsync<OfertaFila>("SELECT * FROM fn_oferta_por_id(@Id::uuid)", new { Id = id }, ct);

    public Task<IReadOnlyList<OfertaLineaFila>> OfertaLineasAsync(Guid ofertaId, CancellationToken ct = default) =>
        ListarAsync<OfertaLineaFila>("SELECT * FROM fn_oferta_lineas(@Id::uuid)", new { Id = ofertaId }, ct);

    public Task GuardarOfertaAsync(OfertaProveedor o, bool esNueva, IReadOnlyList<LineaCantidad> lineas, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_oferta_guardar(@Id::uuid, @EsNueva::boolean, @Codigo::text, @ProveedorId::uuid, @Fecha::date,
                                   @FechaEstimadaLlegada::date, @Estado::text, @Observaciones::text, @Lineas::jsonb,
                                   @FechaRegistro::timestamptz)
            """,
            new
            {
                o.Id, EsNueva = esNueva, o.Codigo, o.ProveedorId, o.Fecha, o.FechaEstimadaLlegada, o.Estado, o.Observaciones,
                Lineas = JsonSerializer.Serialize(lineas.Select(l => new { producto = l.ProductoId, cantidad = l.Cantidad })),
                FechaRegistro = fecha,
            },
            ct);

    public Task<IReadOnlyList<PedidoProveedorFila>> PedidosAsync(CancellationToken ct = default) =>
        ListarAsync<PedidoProveedorFila>("SELECT * FROM fn_pedidos_proveedor_listar()", null, ct);

    public Task<PedidoProveedorFila?> PedidoAsync(Guid id, CancellationToken ct = default) =>
        UnoAsync<PedidoProveedorFila>("SELECT * FROM fn_pedido_proveedor_por_id(@Id::uuid)", new { Id = id }, ct);

    public Task<IReadOnlyList<PedidoProveedorLineaFila>> PedidoLineasAsync(Guid pedidoId, CancellationToken ct = default) =>
        ListarAsync<PedidoProveedorLineaFila>("SELECT * FROM fn_pedido_proveedor_lineas(@Id::uuid)", new { Id = pedidoId }, ct);

    public Task GuardarPedidoAsync(
        PedidoProveedor p, bool esNuevo, IReadOnlyList<LineaPedidoProveedor> lineas, bool actualizarCostos, DateTime fecha,
        CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_pedido_proveedor_guardar(@Id::uuid, @EsNuevo::boolean, @Codigo::text, @ProveedorId::uuid, @OfertaId::uuid,
                                             @Fecha::date, @FechaEstimadaLlegada::date, @Estado::text, @Observaciones::text,
                                             @Lineas::jsonb, @ActualizarCostos::boolean, @FechaRegistro::timestamptz)
            """,
            new
            {
                p.Id, EsNuevo = esNuevo, p.Codigo, p.ProveedorId, p.OfertaId, p.Fecha, p.FechaEstimadaLlegada, p.Estado, p.Observaciones,
                Lineas = JsonSerializer.Serialize(lineas.Select(l => new
                {
                    producto = l.ProductoId, cantidad = l.Cantidad, costo = l.CostoUnitario, observacion = l.Observacion,
                })),
                ActualizarCostos = actualizarCostos,
                FechaRegistro = fecha,
            },
            ct);

    public Task<FallaBaseDatos?> CambiarEstadoPedidoAsync(Guid id, string esperado, string estado, DateTime fecha, CancellationToken ct = default) =>
        EjecutarConFallaAsync(
            "CALL sp_pedido_proveedor_cambiar_estado(@Id::uuid, @Esperado::text, @Estado::text, @Fecha::timestamptz)",
            new { Id = id, Esperado = esperado, Estado = estado, Fecha = fecha }, ct);

    public Task<IReadOnlyList<RecepcionFila>> RecepcionesAsync(Guid? pedidoId, CancellationToken ct = default) =>
        ListarAsync<RecepcionFila>("SELECT * FROM fn_recepciones_listar(@Id::uuid)", new { Id = pedidoId }, ct);

    public Task<IReadOnlyList<RecepcionLineaFila>> RecepcionLineasAsync(Guid recepcionId, CancellationToken ct = default) =>
        ListarAsync<RecepcionLineaFila>("SELECT * FROM fn_recepcion_lineas(@Id::uuid)", new { Id = recepcionId }, ct);

    public Task<FallaBaseDatos?> RegistrarRecepcionAsync(
        Recepcion r,
        string estadoEsperado,
        IReadOnlyList<(Guid Id, Guid PedidoLineaId, Guid ProductoId, int Recibida, int Enferma, int Muerta, decimal Costo, Guid LoteId, string LoteCodigo)> lineas,
        Operador operador,
        DateTime fecha,
        CancellationToken ct = default) =>
        EjecutarConFallaAsync(
            """
            CALL sp_recepcion_registrar(@Id::uuid, @Codigo::text, @PedidoId::uuid, @EstadoEsperado::text, @Fecha::date,
                                        @Observaciones::text, @UsuarioId::uuid, @UsuarioNombre::text, @Lineas::jsonb,
                                        @FechaRegistro::timestamptz)
            """,
            new
            {
                r.Id, r.Codigo, PedidoId = r.PedidoProveedorId, EstadoEsperado = estadoEsperado, r.Fecha, r.Observaciones,
                operador.UsuarioId, UsuarioNombre = operador.Nombre,
                Lineas = JsonSerializer.Serialize(lineas.Select(l => new
                {
                    id = l.Id, pedidoLinea = l.PedidoLineaId, producto = l.ProductoId, recibida = l.Recibida, enferma = l.Enferma,
                    muerta = l.Muerta, costo = l.Costo, lote = l.LoteId, loteCodigo = l.LoteCodigo,
                })),
                FechaRegistro = fecha,
            },
            ct);
}

public class PedidoClienteRepository(PostgresConnectionFactory conexiones) : RepositorioInventario(conexiones), IPedidoClienteRepository
{
    public Task<IReadOnlyList<PedidoClienteFila>> ListarAsync(Guid? comercialId, CancellationToken ct = default) =>
        ListarAsync<PedidoClienteFila>("SELECT * FROM fn_pedidos_cliente_listar(@ComercialId::uuid)", new { ComercialId = comercialId }, ct);

    public Task<PedidoClienteFila?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        UnoAsync<PedidoClienteFila>("SELECT * FROM fn_pedido_cliente_por_id(@Id::uuid)", new { Id = id }, ct);

    public Task<PedidoClienteFila?> PorVentaAsync(Guid ventaId, CancellationToken ct = default) =>
        UnoAsync<PedidoClienteFila>("SELECT * FROM fn_pedido_cliente_por_venta(@Id::uuid)", new { Id = ventaId }, ct);

    public Task<IReadOnlyList<PedidoClienteLineaFila>> LineasAsync(Guid pedidoId, CancellationToken ct = default) =>
        ListarAsync<PedidoClienteLineaFila>("SELECT * FROM fn_pedido_cliente_lineas(@Id::uuid)", new { Id = pedidoId }, ct);

    public Task GuardarAsync(PedidoCliente p, bool esNuevo, IReadOnlyList<LineaPedidoCliente> lineas, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_pedido_cliente_guardar(@Id::uuid, @EsNuevo::boolean, @Codigo::text, @ClienteId::uuid, @ComercialId::uuid,
                                           @VentaId::uuid, @Fecha::date, @Ciudad::text, @Estado::text, @Observaciones::text,
                                           @Lineas::jsonb, @FechaRegistro::timestamptz)
            """,
            new
            {
                p.Id, EsNuevo = esNuevo, p.Codigo, p.ClienteId, p.ComercialId, p.VentaId, p.Fecha, p.Ciudad, p.Estado, p.Observaciones,
                Lineas = JsonSerializer.Serialize(lineas.Select(l => new
                {
                    id = l.Id, producto = l.ProductoId, cantidad = l.Cantidad, precio = l.PrecioUnitario, observacion = l.Observacion,
                })),
                FechaRegistro = fecha,
            },
            ct);

    public Task<FallaBaseDatos?> CambiarEstadoAsync(Guid id, string esperado, string estado, DateTime fecha, CancellationToken ct = default) =>
        EjecutarConFallaAsync(
            "CALL sp_pedido_cliente_cambiar_estado(@Id::uuid, @Esperado::text, @Estado::text, @Fecha::timestamptz)",
            new { Id = id, Esperado = esperado, Estado = estado, Fecha = fecha }, ct);

    public Task<FallaBaseDatos?> DespacharAsync(Guid id, IReadOnlyList<SalidaDespacho> salidas, Operador operador, DateTime fecha, CancellationToken ct = default) =>
        EjecutarConFallaAsync(
            "CALL sp_pedido_cliente_despachar(@Id::uuid, @Salidas::jsonb, @UsuarioId::uuid, @UsuarioNombre::text, @Fecha::timestamptz)",
            new
            {
                Id = id,
                Salidas = JsonSerializer.Serialize(salidas.Select(s => new { linea = s.LineaId, lote = s.LoteId, acuario = s.AcuarioId, cantidad = s.Cantidad })),
                operador.UsuarioId,
                UsuarioNombre = operador.Nombre,
                Fecha = fecha,
            },
            ct);
}
