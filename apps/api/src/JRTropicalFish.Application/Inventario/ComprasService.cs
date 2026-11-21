using System.Globalization;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Crm;
using JRTropicalFish.Domain.Inventario;
using static JRTropicalFish.Application.Crm.ValidacionCrm;

namespace JRTropicalFish.Application.Inventario;

/// <summary>
/// Compras: disponibilidad que ofrecen los proveedores (pre-disponibilidad), pedidos a proveedor y su
/// recepción, que crea un lote por línea y mete el stock vivo "por ubicar". El costo es reservado:
/// sin <c>catalogos:ver-costos</c> no viaja y, al editar, se conserva.
/// </summary>
public class ComprasService(
    IComprasRepository compras,
    IProveedorRepository proveedores,
    IProductoRepository productos,
    IConsecutivoRepository consecutivos,
    IUsuarioActual usuario,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    public const string EntidadOferta = "OfertaProveedor";
    public const string EntidadPedido = "PedidoProveedor";
    public const string EntidadRecepcion = "Recepcion";

    // --- Ofertas ---

    public Task<IReadOnlyList<OfertaFila>> OfertasAsync(CancellationToken ct = default) => compras.OfertasAsync(ct);

    public async Task<Resultado<OfertaDetalle>> OfertaAsync(Guid id, CancellationToken ct = default) =>
        await compras.OfertaAsync(id, ct) is { } o ? new OfertaDetalle(o, await compras.OfertaLineasAsync(id, ct)) : Error.NoEncontrado("La oferta no existe.");

    public async Task<Resultado<Creado>> GuardarOfertaAsync(Guid? id, GuardarOfertaRequest r, CancellationToken ct = default)
    {
        OfertaFila? existente = null;
        if (id is { } oid && (existente = await compras.OfertaAsync(oid, ct)) is null)
        {
            return Error.NoEncontrado("La oferta no existe.");
        }

        var estado = r.Estado ?? EstadosOferta.Abierta;
        var lineas = r.Lineas ?? [];
        var notas = LimpiarTextoLargo(r.Observaciones);
        var error = Primero(
            EnLista(estado, EstadosOferta.Todos, "Estado"),
            lineas.Count == 0 ? Error.Validacion("Agrega al menos una especie con su cantidad.") : null,
            await ValidarLineasAsync(lineas.Select(l => (l.ProductoId, l.Cantidad)), ct),
            r.FechaEstimadaLlegada < (r.Fecha ?? reloj.Hoy) ? Error.Validacion("La llegada estimada no puede ser antes de la fecha de la oferta.") : null,
            Opcional(notas, "Observaciones", 2000),
            await ValidarProveedorAsync(r.ProveedorId, ct));
        if (error is not null)
        {
            return error;
        }

        var oferta = new OfertaProveedor
        {
            Id = id ?? Guid.CreateVersion7(),
            Codigo = existente?.Codigo ?? await consecutivos.SiguienteAsync("DISP", ct),
            ProveedorId = r.ProveedorId,
            Fecha = r.Fecha ?? existente?.Fecha ?? reloj.Hoy,
            FechaEstimadaLlegada = r.FechaEstimadaLlegada,
            Estado = estado,
            Observaciones = notas,
        };
        await compras.GuardarOfertaAsync(oferta, existente is null, lineas, reloj.AhoraUtc, ct);
        var resumen = new CambioAuditado("Resumen", null, $"{lineas.Count} especies, {lineas.Sum(l => l.Cantidad)} unidades, {estado}");
        if (existente is null)
        {
            await auditoria.CreacionAsync(EntidadOferta, oferta.Id, new { oferta.Codigo, Proveedor = r.ProveedorId }, [resumen], ct);
        }
        else
        {
            await auditoria.CambiosAsync(EntidadOferta, oferta.Id, [resumen], ct);
        }

        return new Creado(oferta.Id, oferta.Codigo);
    }

    // --- Pedidos a proveedor ---

    public async Task<IReadOnlyList<PedidoProveedorFila>> PedidosAsync(bool verCostos, CancellationToken ct = default) =>
        [.. (await compras.PedidosAsync(ct)).Select(p => verCostos ? p : p with { CostoTotal = null })];

    public async Task<Resultado<PedidoProveedorDetalle>> PedidoAsync(Guid id, bool verCostos, CancellationToken ct = default)
    {
        if (await compras.PedidoAsync(id, ct) is not { } pedido)
        {
            return Error.NoEncontrado("El pedido a proveedor no existe.");
        }

        var lineas = await compras.PedidoLineasAsync(id, ct);
        var recepciones = await compras.RecepcionesAsync(id, ct);
        return verCostos
            ? new PedidoProveedorDetalle(pedido, lineas, recepciones)
            : new PedidoProveedorDetalle(
                pedido with { CostoTotal = null },
                [.. lineas.Select(l => l with { CostoUnitario = null })],
                [.. recepciones.Select(x => x with { CostoTotal = 0 })]);
    }

    public async Task<Resultado<Creado>> GuardarPedidoAsync(
        Guid? id, GuardarPedidoProveedorRequest r, bool verCostos, CancellationToken ct = default)
    {
        PedidoProveedorFila? existente = null;
        if (id is { } pid)
        {
            if ((existente = await compras.PedidoAsync(pid, ct)) is null)
            {
                return Error.NoEncontrado("El pedido a proveedor no existe.");
            }

            if (!ReglasPedido.ProveedorEditable(existente.Estado, existente.Recibidas))
            {
                return Error.Conflicto("El pedido ya tiene recepciones o está cerrado: no se puede editar.");
            }
        }

        var lineas = r.Lineas ?? [];
        var notas = LimpiarTextoLargo(r.Observaciones);
        var error = Primero(
            lineas.Count == 0 ? Error.Validacion("Agrega al menos una especie con su cantidad.") : null,
            await ValidarLineasAsync(lineas.Select(l => (l.ProductoId, l.Cantidad)), ct),
            lineas.Any(l => l.CostoUnitario is < 0) ? Error.Validacion("El costo unitario no puede ser negativo.") : null,
            !verCostos && lineas.Any(l => l.CostoUnitario is not null)
                ? Error.Prohibido("No tienes permiso para ver ni editar costos de compra.")
                : null,
            lineas.Any(l => Limpiar(l.Observacion) is { Length: > 500 }) ? Error.Validacion("Observación: máximo 500 caracteres.") : null,
            Opcional(notas, "Observaciones", 2000),
            await ValidarProveedorAsync(r.ProveedorId, ct));
        if (error is not null)
        {
            return error;
        }

        var pedido = new PedidoProveedor
        {
            Id = id ?? Guid.CreateVersion7(),
            Codigo = existente?.Codigo ?? await consecutivos.SiguienteAsync("PP", ct),
            ProveedorId = r.ProveedorId,
            OfertaId = r.OfertaId,
            Fecha = r.Fecha ?? existente?.Fecha ?? reloj.Hoy,
            FechaEstimadaLlegada = r.FechaEstimadaLlegada,
            Estado = existente?.Estado ?? EstadosPedidoProveedor.Borrador,
            Observaciones = notas,
        };
        var limpias = lineas.Select(l => l with { Observacion = Limpiar(l.Observacion) }).ToList();
        await compras.GuardarPedidoAsync(pedido, existente is null, limpias, verCostos, reloj.AhoraUtc, ct);
        var resumen = new CambioAuditado("Resumen", null, $"{limpias.Count} especies, {limpias.Sum(l => l.Cantidad)} unidades");
        if (existente is null)
        {
            await auditoria.CreacionAsync(EntidadPedido, pedido.Id, new { pedido.Codigo, pedido.Estado }, [resumen], ct);
        }
        else
        {
            await auditoria.CambiosAsync(EntidadPedido, pedido.Id, [resumen], ct);
        }

        return new Creado(pedido.Id, pedido.Codigo);
    }

    public async Task<Resultado<Guid>> CambiarEstadoPedidoAsync(Guid id, CambiarEstadoRequest r, CancellationToken ct = default)
    {
        if (await compras.PedidoAsync(id, ct) is not { } pedido)
        {
            return Error.NoEncontrado("El pedido a proveedor no existe.");
        }

        var esperado = r.EstadoActual ?? pedido.Estado;
        if (esperado != pedido.Estado)
        {
            return FallasInventario.AError(new FallaBaseDatos("JR001", ""));
        }

        if (ReglasPedido.ValidarProveedor(pedido.Estado, r.Estado) is { } error)
        {
            return error;
        }

        if (await compras.CambiarEstadoPedidoAsync(id, esperado, r.Estado!, reloj.AhoraUtc, ct) is { } falla)
        {
            return FallasInventario.AError(falla);
        }

        await auditoria.CambiosAsync(EntidadPedido, id, [new CambioAuditado("Estado", pedido.Estado, r.Estado)], ct);
        return id;
    }

    // --- Recepciones ---

    public async Task<IReadOnlyList<RecepcionFila>> RecepcionesAsync(bool verCostos, CancellationToken ct = default) =>
        [.. (await compras.RecepcionesAsync(null, ct)).Select(x => verCostos ? x : x with { CostoTotal = 0 })];

    public async Task<Resultado<RecepcionDetalle>> RecepcionAsync(Guid id, bool verCostos, CancellationToken ct = default)
    {
        var recepcion = (await compras.RecepcionesAsync(null, ct)).FirstOrDefault(x => x.Id == id);
        if (recepcion is null)
        {
            return Error.NoEncontrado("La recepción no existe.");
        }

        var lineas = await compras.RecepcionLineasAsync(id, ct);
        return verCostos
            ? new RecepcionDetalle(recepcion, lineas)
            : new RecepcionDetalle(recepcion with { CostoTotal = 0 }, [.. lineas.Select(l => l with { CostoUnitario = 0, CostoTotal = 0 })]);
    }

    /// <summary>
    /// Registra la llegada de un pedido a proveedor: por línea, recibida (total que llegó), enferma y
    /// muerta, y el costo unitario. Crea un lote por línea con el stock vivo por ubicar (sano y enfermo)
    /// y deja el pedido en «recibido» o «recibido parcial». Recibir de más se permite con aviso.
    /// Sin permiso de costos, el costo es el pactado en el pedido.
    /// </summary>
    public async Task<Resultado<RecepcionRegistrada>> RecibirAsync(RegistrarRecepcionRequest r, bool verCostos, CancellationToken ct = default)
    {
        if (await compras.PedidoAsync(r.PedidoProveedorId, ct) is not { } pedido)
        {
            return Error.NoEncontrado("El pedido a proveedor no existe.");
        }

        if (!EstadosPedidoProveedor.Pendientes.Contains(pedido.Estado))
        {
            return Error.Conflicto("Solo se reciben pedidos enviados, en tránsito o recibidos en parte.");
        }

        if (r.EstadoPedido is not null && r.EstadoPedido != pedido.Estado)
        {
            return FallasInventario.AError(new FallaBaseDatos("JR001", ""));
        }

        var lineasPedido = (await compras.PedidoLineasAsync(pedido.Id, ct)).ToDictionary(l => l.Id);
        var lineas = (r.Lineas ?? []).Where(l => l.Recibida > 0).ToList();
        if (lineas.Count == 0)
        {
            return Error.Validacion("Indica lo recibido en al menos una especie.");
        }

        if (lineas.Select(l => l.PedidoLineaId).Distinct().Count() != lineas.Count)
        {
            return Error.Validacion("Hay una especie repetida en la recepción.");
        }

        var notas = LimpiarTextoLargo(r.Observaciones);
        var fecha = r.Fecha ?? reloj.Hoy;
        if (Primero(Opcional(notas, "Observaciones", 2000), fecha > reloj.Hoy ? Error.Validacion("La fecha de recepción no puede ser futura.") : null) is { } errorGeneral)
        {
            return errorGeneral;
        }

        var avisos = new List<string>();
        var aRegistrar = new List<(Guid, Guid, Guid, int, int, int, decimal, Guid, string)>();
        foreach (var l in lineas)
        {
            if (!lineasPedido.TryGetValue(l.PedidoLineaId, out var lp))
            {
                return Error.Validacion("Una línea no pertenece al pedido.");
            }

            if (l.Enferma < 0 || l.Muerta < 0 || l.Enferma + l.Muerta > l.Recibida)
            {
                return Error.Validacion($"{lp.ProductoCodigo}: enfermos + muertos no pueden superar lo recibido.");
            }

            if (!verCostos && l.CostoUnitario is not null)
            {
                return Error.Prohibido("No tienes permiso para ver ni editar costos de compra.");
            }

            var costo = verCostos ? l.CostoUnitario ?? lp.CostoUnitario ?? 0 : lp.CostoUnitario ?? 0;
            if (costo < 0)
            {
                return Error.Validacion("El costo unitario no puede ser negativo.");
            }

            if (l.Recibida > lp.Pendiente)
            {
                avisos.Add($"{lp.ProductoCodigo}: llegaron {l.Recibida} y quedaban {lp.Pendiente} por recibir.");
            }

            aRegistrar.Add((Guid.CreateVersion7(), lp.Id, lp.ProductoId, l.Recibida, l.Enferma, l.Muerta, costo, Guid.CreateVersion7(),
                await consecutivos.SiguienteAsync(InventarioService.PrefijoLote, ct)));
        }

        var recepcion = new Recepcion
        {
            Codigo = await consecutivos.SiguienteAsync("REC", ct),
            PedidoProveedorId = pedido.Id,
            Fecha = fecha,
            Observaciones = notas,
            UsuarioId = usuario.UsuarioId,
        };
        var falla = await compras.RegistrarRecepcionAsync(
            recepcion, pedido.Estado, aRegistrar, new Operador(usuario.UsuarioId, usuario.Nombre), reloj.AhoraUtc, ct);
        if (falla is not null)
        {
            return FallasInventario.AError(falla);
        }

        var resumen = string.Join("; ", aRegistrar.Select(x =>
            string.Create(CultureInfo.InvariantCulture, $"{lineasPedido[x.Item2].ProductoCodigo} {x.Item4} (enf. {x.Item5}, muertos {x.Item6}) → {x.Item9}")));
        await auditoria.CreacionAsync(EntidadRecepcion, recepcion.Id, new { recepcion.Codigo, Pedido = pedido.Codigo }, [new CambioAuditado("Lineas", null, resumen)], ct);
        await auditoria.CambiosAsync(EntidadPedido, pedido.Id, [new CambioAuditado("Recepcion", null, recepcion.Codigo)], ct);
        return new RecepcionRegistrada(recepcion.Id, recepcion.Codigo, [.. aRegistrar.Select(x => x.Item9)], avisos);
    }

    private async Task<Error?> ValidarProveedorAsync(Guid proveedorId, CancellationToken ct) =>
        await proveedores.ObtenerAsync(proveedorId, ct) is { Activo: true } ? null : Error.Validacion("Elige un proveedor activo.");

    private async Task<Error?> ValidarLineasAsync(IEnumerable<(Guid ProductoId, int Cantidad)> lineas, CancellationToken ct)
    {
        var lista = lineas.ToList();
        if (lista.Select(l => l.ProductoId).Distinct().Count() != lista.Count)
        {
            return Error.Validacion("Hay una especie repetida: súmala en una sola línea.");
        }

        if (lista.Any(l => l.Cantidad is <= 0 or > ReglasStock.CantidadMaxima))
        {
            return Error.Validacion("Cada cantidad debe ser mayor que cero.");
        }

        foreach (var (productoId, _) in lista)
        {
            if (await productos.ObtenerAsync(productoId, ct) is null)
            {
                return Error.Validacion("Una de las especies no existe en el catálogo.");
            }
        }

        return null;
    }
}
