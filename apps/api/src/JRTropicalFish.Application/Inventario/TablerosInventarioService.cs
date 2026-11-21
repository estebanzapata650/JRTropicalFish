using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Domain.Inventario;

namespace JRTropicalFish.Application.Inventario;

/// <summary>
/// Tableros de inventario (reemplazan los de Looker): bodega, proveedores, clientes y trazabilidad del lote.
/// Solo lectura. Las agregaciones por proveedor, cliente o semana se arman aquí sobre filas ya calculadas
/// en la base (las cantidades y los tiempos salen de las funciones SQL, nunca se recalculan del stock).
/// </summary>
public class TablerosInventarioService(
    IInventarioRepository inventario,
    ITablerosInventarioRepository tableros,
    IPedidoClienteRepository pedidos,
    IReloj reloj)
{
    /// <summary>Colombia no tiene horario de verano: el día de negocio empieza a las 05:00 UTC.</summary>
    private static readonly TimeSpan DesfaseBogota = TimeSpan.FromHours(5);

    private const string SinProveedor = "Sin proveedor";

    // --- Bodega ---

    public async Task<TableroBodega> BodegaAsync(CancellationToken ct = default)
    {
        var hoy = reloj.Hoy;
        var porProducto = await inventario.PorProductoAsync(ct);
        var lotes = await inventario.LotesAsync(hoy, null, soloConStock: true, ct);
        var existencias = await inventario.ExistenciasAsync(hoy, null, null, null, ct);
        var acuarios = (await inventario.AcuariosAsync(ct)).Where(a => a.Activo).ToList();
        var sectores = acuarios.ToDictionary(a => a.Id, a => a.Sector);

        var ubicaciones = existencias
            .Where(e => e.AcuarioId is not null)
            .GroupBy(e => (Id: e.AcuarioId!.Value, Codigo: e.AcuarioCodigo!))
            .Select(g => new UbicacionAcuario(
                g.Key.Id,
                g.Key.Codigo,
                sectores.GetValueOrDefault(g.Key.Id, ""),
                g.Where(e => e.Condicion == Condiciones.Sano).Sum(e => e.Cantidad),
                g.Where(e => e.Condicion == Condiciones.Enfermo).Sum(e => e.Cantidad),
                [.. g.GroupBy(e => (e.ProductoCodigo, e.NombreCientifico))
                    .Select(p => new EspecieEnAcuario(p.Key.ProductoCodigo, p.Key.NombreCientifico, p.Sum(e => e.Cantidad)))
                    .OrderByDescending(p => p.Cantidad)]))
            .OrderByDescending(u => u.Total)
            .ThenBy(u => u.AcuarioCodigo, StringComparer.Ordinal)
            .ToList();

        return new TableroBodega(
            porProducto.Sum(p => p.Sanos),
            porProducto.Sum(p => p.PorUbicar),
            porProducto.Sum(p => p.Enfermos),
            acuarios.Count,
            acuarios.Count(a => a.Sanos + a.Enfermos > 0),
            acuarios.Count(a => a.Enfermos > 0),
            lotes.Count,
            lotes.Count(l => l.DiasEnBodega > InventarioService.DiasLoteAntiguo),
            [.. porProducto.Where(p => p.Sanos + p.PorUbicar + p.Enfermos > 0)],
            ubicaciones,
            [.. existencias.Where(e => e.Condicion == Condiciones.Enfermo).OrderByDescending(e => e.Cantidad)]);
    }

    // --- Proveedores ---

    public async Task<TableroProveedores> ProveedoresAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default)
    {
        var enTransito = await tableros.ComprasPendientesAsync(ct);
        var cumplimiento = await tableros.CumplimientoComprasAsync(desde, hasta, ct);
        var lotes = (await inventario.LotesAsync(reloj.Hoy, null, soloConStock: false, ct))
            .Where(l => l.FechaIngreso >= desde && l.FechaIngreso <= hasta)
            .ToList();

        // Guid.Empty agrupa los lotes sin proveedor (algunos del saldo inicial).
        var porProveedorPedidos = cumplimiento.GroupBy(c => c.ProveedorId).ToDictionary(g => g.Key);
        var porProveedorLotes = lotes.GroupBy(l => l.ProveedorId ?? Guid.Empty).ToDictionary(g => g.Key);
        var claves = porProveedorPedidos.Keys.Union(porProveedorLotes.Keys);

        var proveedores = claves
            .Select(clave =>
            {
                var ped = porProveedorPedidos.GetValueOrDefault(clave);
                var lot = porProveedorLotes.GetValueOrDefault(clave);
                var nombre = ped?.First().ProveedorNombre ?? lot?.First().ProveedorNombre ?? SinProveedor;
                return new ProveedorResumen(
                    clave == Guid.Empty ? null : clave,
                    nombre,
                    ped?.Count() ?? 0,
                    ped?.Sum(c => c.Pedidas) ?? 0,
                    ped?.Sum(c => c.RecibidasEnFull) ?? 0,
                    lot?.Count() ?? 0,
                    lot?.Sum(l => l.Recibidos) ?? 0,
                    lot?.Sum(l => l.MuertosLlegada) ?? 0,
                    lot?.Sum(l => l.Muertos - l.MuertosLlegada) ?? 0);
            })
            .OrderByDescending(p => p.Recibidos)
            .ThenByDescending(p => p.Pedidas)
            .ToList();

        return new TableroProveedores(
            desde,
            hasta,
            enTransito.Select(l => l.PedidoId).Distinct().Count(),
            enTransito.Sum(l => l.Pendiente),
            enTransito.Select(l => l.CentroNombre).Where(c => c is not null).Distinct().Count(),
            cumplimiento.Sum(c => c.Pedidas),
            cumplimiento.Sum(c => c.RecibidasEnFull),
            lotes.Sum(l => l.Recibidos),
            lotes.Sum(l => l.Muertos),
            enTransito,
            proveedores,
            cumplimiento,
            Semanas(desde, hasta, lotes));
    }

    /// <summary>Recibidos y muertos por semana (lunes) del periodo, con las semanas vacías en cero.</summary>
    public static IReadOnlyList<SemanaRecepcion> Semanas(DateOnly desde, DateOnly hasta, IEnumerable<LoteFila> lotes)
    {
        var porSemana = lotes.GroupBy(l => Lunes(l.FechaIngreso)).ToDictionary(g => g.Key);
        var semanas = new List<SemanaRecepcion>();
        for (var lunes = Lunes(desde); lunes <= hasta; lunes = lunes.AddDays(7))
        {
            var g = porSemana.GetValueOrDefault(lunes);
            semanas.Add(new SemanaRecepcion(lunes, g?.Sum(l => l.Recibidos) ?? 0, g?.Sum(l => l.Muertos) ?? 0));
        }

        return semanas;
    }

    public static DateOnly Lunes(DateOnly fecha) => fecha.AddDays(-(((int)fecha.DayOfWeek + 6) % 7));

    // --- Clientes ---

    /// <param name="comercialFiltro">Comercial sin <c>crm:ver-todo</c>: solo sus pedidos y despachos.</param>
    public async Task<TableroClientes> ClientesAsync(DateOnly desde, DateOnly hasta, Guid? comercialFiltro, CancellationToken ct = default)
    {
        bool Visible(Guid comercialId) => comercialFiltro is null || comercialFiltro == comercialId;

        var pendientes = (await tableros.PedidosClientePendientesAsync(ct)).Where(l => Visible(l.ComercialId)).ToList();
        var despachos = (await tableros.DespachosAsync(InicioDia(desde), InicioDia(hasta.AddDays(1)), null, ct))
            .Where(d => Visible(d.ComercialId))
            .ToList();
        var pedidosPeriodo = (await pedidos.ListarAsync(comercialFiltro, ct))
            .Where(p => p.Fecha >= desde && p.Fecha <= hasta
                        && p.Estado is not (EstadosPedidoCliente.Borrador or EstadosPedidoCliente.Cancelado))
            .ToList();

        var porClientePedidos = pedidosPeriodo.GroupBy(p => p.ClienteId).ToDictionary(g => g.Key);
        var porClienteDespachos = despachos.GroupBy(d => d.ClienteId).ToDictionary(g => g.Key);
        var clientes = porClientePedidos.Keys.Union(porClienteDespachos.Keys)
            .Select(id =>
            {
                var ped = porClientePedidos.GetValueOrDefault(id);
                var des = porClienteDespachos.GetValueOrDefault(id);
                return new ClienteResumen(
                    id,
                    ped?.First().ClienteNombre ?? des!.First().ClienteNombre,
                    ped?.Count() ?? 0,
                    ped?.Sum(p => p.Unidades) ?? 0,
                    ped?.Sum(p => p.Despachadas) ?? 0,
                    des?.Sum(d => d.Cantidad) ?? 0);
            })
            .OrderByDescending(c => c.DespachadoPeriodo)
            .ThenByDescending(c => c.Solicitadas)
            .ToList();

        var especies = despachos
            .GroupBy(d => (d.ProductoId, d.ProductoCodigo, d.NombreCientifico))
            .Select(g => new EspecieVendida(
                g.Key.ProductoId, g.Key.ProductoCodigo, g.Key.NombreCientifico, g.Sum(d => d.Cantidad), g.Select(d => d.ClienteId).Distinct().Count()))
            .OrderByDescending(e => e.Cantidad)
            .Take(20)
            .ToList();

        return new TableroClientes(
            desde,
            hasta,
            pendientes.Select(l => l.PedidoId).Distinct().Count(),
            pendientes.Sum(l => l.Pendiente),
            despachos.Sum(d => d.Cantidad),
            pedidosPeriodo.Sum(p => p.Unidades),
            pedidosPeriodo.Sum(p => p.Despachadas),
            pendientes,
            clientes,
            especies);
    }

    // --- Trazabilidad ---

    public async Task<IReadOnlyList<LoteTrazable>> TrazabilidadAsync(bool soloConStock, CancellationToken ct = default)
    {
        var lotes = await inventario.LotesAsync(reloj.Hoy, null, soloConStock, ct);
        var tiempos = (await tableros.TiemposAsync(null, reloj.AhoraUtc, ct)).ToDictionary(t => t.LoteId);
        return [.. lotes.Select(l => new LoteTrazable(l, tiempos.GetValueOrDefault(l.Id) ?? SinTiempos(l.Id)))];
    }

    public async Task<Resultado<RecorridoLote>> RecorridoAsync(Guid id, CancellationToken ct = default)
    {
        var hoy = reloj.Hoy;
        if (await inventario.LoteAsync(id, hoy, ct) is not { } lote)
        {
            return Error.NoEncontrado("El lote no existe.");
        }

        var tiempos = (await tableros.TiemposAsync(id, reloj.AhoraUtc, ct)).FirstOrDefault() ?? SinTiempos(id);
        return new RecorridoLote(
            lote,
            tiempos,
            await inventario.ExistenciasAsync(hoy, null, id, null, ct),
            await tableros.DespachosAsync(null, null, id, ct),
            await inventario.MovimientosAsync(id, 500, ct));
    }

    /// <summary>Periodo por defecto de los tableros: los últimos 30 días hasta hoy.</summary>
    public (DateOnly Desde, DateOnly Hasta) Periodo(DateOnly? desde, DateOnly? hasta)
    {
        var fin = hasta ?? reloj.Hoy;
        var inicio = desde ?? fin.AddDays(-29);
        return inicio > fin ? (fin, inicio) : (inicio, fin);
    }

    private static DateTime InicioDia(DateOnly dia) => dia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) + DesfaseBogota;

    private static LoteTiemposFila SinTiempos(Guid loteId) => new(loteId, 0, false, 0, false, 0, 0, 0, null, null);
}
