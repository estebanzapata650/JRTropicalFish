using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Crm;
using JRTropicalFish.Domain.Inventario;
using static JRTropicalFish.Application.Crm.ValidacionCrm;

namespace JRTropicalFish.Application.Inventario;

/// <summary>
/// Pedidos de cliente con líneas por especie. Nacen de una venta del CRM (heredan cliente y comercial)
/// o sueltos (mostrador). El despacho descuenta stock sano lote por lote y acuario por acuario; al
/// completarse, el pedido queda «despachado» y la venta enlazada «listo para despacho».
/// Un comercial sin <c>crm:ver-todo</c> solo ve y gestiona los suyos.
/// </summary>
public class PedidoClienteService(
    IPedidoClienteRepository pedidos,
    IVentaRepository ventas,
    IClienteRepository clientes,
    IComercialRepository comerciales,
    IProductoRepository productos,
    IConsecutivoRepository consecutivos,
    IUsuarioActual usuario,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    public const string Entidad = "PedidoCliente";
    public const string Prefijo = "PC";

    public Task<IReadOnlyList<PedidoClienteFila>> ListarAsync(AlcanceCrm alcance, CancellationToken ct = default) =>
        pedidos.ListarAsync(alcance.FiltroComercial, ct);

    public async Task<Resultado<PedidoClienteDetalle>> ObtenerAsync(Guid id, AlcanceCrm alcance, CancellationToken ct = default) =>
        await Visible(id, alcance, ct) is { } p ? new PedidoClienteDetalle(p, await pedidos.LineasAsync(id, ct)) : NoExiste();

    /// <summary>El pedido de una venta del CRM, si ya existe (para el botón de la pantalla de ventas).</summary>
    public async Task<PedidoClienteFila?> PorVentaAsync(Guid ventaId, AlcanceCrm alcance, CancellationToken ct = default) =>
        await pedidos.PorVentaAsync(ventaId, ct) is { } p && alcance.PuedeVer(p.ComercialId) ? p : null;

    public async Task<Resultado<Creado>> CrearAsync(GuardarPedidoClienteRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        Guid clienteId;
        ComercialInfo comercial;
        string? ciudad = Limpiar(r.Ciudad);
        if (r.VentaId is { } ventaId)
        {
            if (await ventas.ObtenerAsync(ventaId, ct) is not { } venta || !alcance.PuedeVer(venta.ComercialId))
            {
                return Error.Validacion("La venta no existe.");
            }

            if (await pedidos.PorVentaAsync(ventaId, ct) is { } yaExiste)
            {
                return Error.Conflicto($"La venta {venta.Codigo} ya tiene el pedido {yaExiste.Codigo}.");
            }

            clienteId = venta.ClienteId;
            comercial = new ComercialInfo(venta.ComercialId, venta.ComercialNombre, "");
            ciudad ??= venta.CiudadEntrega;
        }
        else
        {
            if (r.ClienteId is null || await clientes.ObtenerAsync(r.ClienteId.Value, ct) is null)
            {
                return Error.Validacion("Elige un cliente existente.");
            }

            var resuelto = await ComercialesCrm.ResolverAsync(comerciales, alcance, r.ComercialId, null, ct);
            if (!resuelto.Exitoso)
            {
                return resuelto.Error!;
            }

            clienteId = r.ClienteId.Value;
            comercial = resuelto.Valor;
        }

        var lineas = await ValidarLineasAsync(r, null, ct);
        if (!lineas.Exitoso)
        {
            return lineas.Error!;
        }

        var notas = LimpiarTextoLargo(r.Observaciones);
        if (Primero(Opcional(ciudad, "Ciudad", 100), Opcional(notas, "Observaciones", 2000)) is { } error)
        {
            return error;
        }

        var pedido = new PedidoCliente
        {
            Codigo = await consecutivos.SiguienteAsync(Prefijo, ct),
            ClienteId = clienteId,
            ComercialId = comercial.Id,
            VentaId = r.VentaId,
            Fecha = r.Fecha ?? reloj.Hoy,
            Ciudad = ciudad,
            Estado = EstadosPedidoCliente.Borrador,
            Observaciones = notas,
        };
        await pedidos.GuardarAsync(pedido, esNuevo: true, lineas.Valor, reloj.AhoraUtc, ct);
        await auditoria.CreacionAsync(Entidad, pedido.Id, await AuditableAsync(pedido.Id, ct), ct: ct);
        return new Creado(pedido.Id, pedido.Codigo);
    }

    /// <summary>Solo en borrador o confirmado. Una línea con despachos no se quita ni baja de lo despachado.</summary>
    public async Task<Resultado<Guid>> ActualizarAsync(Guid id, GuardarPedidoClienteRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await Visible(id, alcance, ct) is not { } existente)
        {
            return NoExiste();
        }

        if (!ReglasPedido.ClienteEditable(existente.Estado))
        {
            return Error.Conflicto("El pedido ya está en preparación o cerrado: no se puede editar.");
        }

        var lineas = await ValidarLineasAsync(r, existente, ct);
        if (!lineas.Exitoso)
        {
            return lineas.Error!;
        }

        var ciudad = Limpiar(r.Ciudad);
        var notas = LimpiarTextoLargo(r.Observaciones);
        if (Primero(Opcional(ciudad, "Ciudad", 100), Opcional(notas, "Observaciones", 2000)) is { } error)
        {
            return error;
        }

        var antes = await AuditableAsync(id, ct);
        var pedido = new PedidoCliente
        {
            Id = id,
            Codigo = existente.Codigo,
            ClienteId = existente.ClienteId,
            ComercialId = existente.ComercialId,
            VentaId = existente.VentaId,
            Fecha = r.Fecha ?? existente.Fecha,
            Ciudad = ciudad,
            Estado = existente.Estado,
            Observaciones = notas,
        };
        await pedidos.GuardarAsync(pedido, esNuevo: false, lineas.Valor, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(Entidad, id, ComparadorAuditoria.Comparar(antes, await AuditableAsync(id, ct)), ct);
        return id;
    }

    public async Task<Resultado<Guid>> CambiarEstadoAsync(Guid id, CambiarEstadoRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await Visible(id, alcance, ct) is not { } pedido)
        {
            return NoExiste();
        }

        var esperado = r.EstadoActual ?? pedido.Estado;
        if (esperado != pedido.Estado)
        {
            return FallasInventario.AError(new FallaBaseDatos("JR001", ""));
        }

        if (ReglasPedido.ValidarCliente(pedido.Estado, r.Estado, pedido.Despachadas) is { } error)
        {
            return error;
        }

        if (r.Estado == EstadosPedidoCliente.Confirmado && pedido.Unidades == 0)
        {
            return Error.Validacion("Agrega especies al pedido antes de confirmarlo.");
        }

        if (await pedidos.CambiarEstadoAsync(id, esperado, r.Estado!, reloj.AhoraUtc, ct) is { } falla)
        {
            return FallasInventario.AError(falla);
        }

        await auditoria.CambiosAsync(Entidad, id, [new CambioAuditado("Estado", pedido.Estado, r.Estado)], ct);
        return id;
    }

    /// <summary>
    /// Picking y despacho: cada salida descuenta stock sano de un lote en un acuario contra una línea.
    /// No supera lo pendiente de la línea ni el stock (lo valida la base con bloqueo de filas).
    /// </summary>
    public async Task<Resultado<PedidoClienteFila>> DespacharAsync(Guid id, DespacharRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await Visible(id, alcance, ct) is not { } pedido)
        {
            return NoExiste();
        }

        if (!EstadosPedidoCliente.Activos.Contains(pedido.Estado))
        {
            return Error.Conflicto("Solo se despachan pedidos confirmados o en preparación.");
        }

        var salidas = (r.Salidas ?? []).Where(s => s.Cantidad != 0).ToList();
        if (salidas.Count == 0)
        {
            return Error.Validacion("Indica qué se despacha.");
        }

        if (salidas.Any(s => s.Cantidad is < 0 or > ReglasStock.CantidadMaxima))
        {
            return Error.Validacion("Las cantidades despachadas deben ser mayores que cero.");
        }

        var lineas = (await pedidos.LineasAsync(id, ct)).ToDictionary(l => l.Id);
        foreach (var grupo in salidas.GroupBy(s => s.LineaId))
        {
            if (!lineas.TryGetValue(grupo.Key, out var linea))
            {
                return Error.Validacion("Una salida no corresponde a una línea del pedido.");
            }

            if (grupo.Sum(s => s.Cantidad) > linea.Pendiente)
            {
                return Error.Validacion($"{linea.ProductoCodigo}: quedan {linea.Pendiente} por despachar.");
            }
        }

        if (await pedidos.DespacharAsync(id, salidas, new Operador(usuario.UsuarioId, usuario.Nombre), reloj.AhoraUtc, ct) is { } falla)
        {
            return FallasInventario.AError(falla);
        }

        var despues = (await pedidos.ObtenerAsync(id, ct))!;
        await auditoria.CambiosAsync(
            Entidad, id,
            [
                new CambioAuditado("Despacho", null, $"{salidas.Sum(s => s.Cantidad)} unidades"),
                .. despues.Estado != pedido.Estado ? [new CambioAuditado("Estado", pedido.Estado, despues.Estado)] : Array.Empty<CambioAuditado>(),
            ],
            ct);
        return despues;
    }

    public async Task<Resultado<IReadOnlyList<RegistroAuditoriaLectura>>> HistorialAsync(Guid id, AlcanceCrm alcance, CancellationToken ct = default) =>
        await Visible(id, alcance, ct) is null ? NoExiste() : Resultado.Ok(await auditoria.HistorialAsync(Entidad, id, ct));

    private async Task<Resultado<IReadOnlyList<LineaPedidoCliente>>> ValidarLineasAsync(
        GuardarPedidoClienteRequest r, PedidoClienteFila? existente, CancellationToken ct)
    {
        var lineas = r.Lineas ?? [];
        if (lineas.Select(l => l.ProductoId).Distinct().Count() != lineas.Count)
        {
            return Error.Validacion("Hay una especie repetida: súmala en una sola línea.");
        }

        if (lineas.Any(l => l.Cantidad is <= 0 or > ReglasStock.CantidadMaxima || l.PrecioUnitario is < 0))
        {
            return Error.Validacion("Cada línea necesita una cantidad mayor que cero y un precio no negativo.");
        }

        var actuales = existente is null ? [] : (await pedidos.LineasAsync(existente.Id, ct)).ToDictionary(l => l.Id);
        foreach (var conDespacho in actuales.Values.Where(l => l.Despachada > 0))
        {
            var nueva = lineas.FirstOrDefault(l => l.Id == conDespacho.Id);
            if (nueva is null || nueva.ProductoId != conDespacho.ProductoId || nueva.Cantidad < conDespacho.Despachada)
            {
                return Error.Conflicto($"{conDespacho.ProductoCodigo} ya tiene {conDespacho.Despachada} despachados: no se puede quitar ni bajar de ahí.");
            }
        }

        var limpias = new List<LineaPedidoCliente>(lineas.Count);
        foreach (var l in lineas)
        {
            if (await productos.ObtenerAsync(l.ProductoId, ct) is null)
            {
                return Error.Validacion("Una de las especies no existe en el catálogo.");
            }

            var observacion = Limpiar(l.Observacion);
            if (Opcional(observacion, "Observación", 500) is { } error)
            {
                return error;
            }

            // Ids estables: una línea existente conserva el suyo (los despachos apuntan a él).
            var id = l.Id is { } lid && actuales.ContainsKey(lid) ? lid : Guid.CreateVersion7();
            limpias.Add(l with { Id = id, Observacion = observacion });
        }

        return limpias;
    }

    private async Task<PedidoClienteAuditable> AuditableAsync(Guid id, CancellationToken ct)
    {
        var p = (await pedidos.ObtenerAsync(id, ct))!;
        var lineas = await pedidos.LineasAsync(id, ct);
        return new PedidoClienteAuditable(
            p.ClienteNombre, p.ComercialNombre, p.VentaCodigo, p.Fecha, p.Ciudad, p.Estado, p.Observaciones,
            string.Join(", ", lineas.Select(l => $"{l.ProductoCodigo} × {l.Cantidad}")));
    }

    private async Task<PedidoClienteFila?> Visible(Guid id, AlcanceCrm alcance, CancellationToken ct) =>
        await pedidos.ObtenerAsync(id, ct) is { } p && alcance.PuedeVer(p.ComercialId) ? p : null;

    private static Error NoExiste() => Error.NoEncontrado("El pedido no existe.");
}
