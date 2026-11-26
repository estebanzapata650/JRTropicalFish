using System.Text.Json;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Archivos;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Catalogos;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Crm;
using JRTropicalFish.Application.Informes;
using JRTropicalFish.Application.Inventario;
using JRTropicalFish.Domain.Crm;
using JRTropicalFish.Domain.Exportaciones;
using JRTropicalFish.Domain.Inventario;
using static JRTropicalFish.Application.Crm.ValidacionCrm;

namespace JRTropicalFish.Application.Exportaciones;

/// <summary>
/// Embarque de exportación: nace de una venta de exportación del CRM y reúne lo que el Excel de formatos
/// pedía re-digitar (especies y precios, cajas, cargos, flete, empaque real, DOA). Sus líneas son las del
/// pedido de cliente (se sincronizan), y la factura emitida actualiza la venta y la torre de vuelos.
/// Un comercial sin <c>crm:ver-todo</c> solo ve y gestiona los suyos.
/// </summary>
public class EmbarqueService(
    IEmbarqueRepository embarques,
    IConfiguracionExportacionRepository configuracion,
    ConfiguracionExportacionService servicioConfiguracion,
    AunapService aunap,
    IVentaRepository ventas,
    IClienteRepository clientes,
    IPedidoClienteRepository pedidos,
    PedidoClienteService servicioPedidos,
    IProductoRepository productos,
    IPrecioRepository precios,
    IConsecutivoRepository consecutivos,
    IGeneradorInformePdf generadorPdf,
    IAlmacenArchivos almacen,
    IServicioCorreo correo,
    IUsuarioActual usuario,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    public const string Entidad = "Embarque";
    public const string Prefijo = "EXP";
    public const string CodigoListaEstandar = "STANDARD";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<IReadOnlyList<EmbarqueFila>> ListarAsync(AlcanceCrm alcance, CancellationToken ct = default) =>
        embarques.ListarAsync(alcance.FiltroComercial, ct);

    public async Task<Resultado<EmbarqueDetalle>> ObtenerAsync(Guid id, AlcanceCrm alcance, CancellationToken ct = default) =>
        await Visible(id, alcance, ct) is { } e ? await DetalleAsync(e, ct) : NoExiste();

    public async Task<Guid?> PorVentaAsync(Guid ventaId, AlcanceCrm alcance, CancellationToken ct = default) =>
        await embarques.IdPorVentaAsync(ventaId, ct) is { } id && await Visible(id, alcance, ct) is not null ? id : null;

    public async Task<Resultado<IReadOnlyList<RegistroAuditoriaLectura>>> HistorialAsync(Guid id, AlcanceCrm alcance, CancellationToken ct = default) =>
        await Visible(id, alcance, ct) is null ? NoExiste() : Resultado.Ok(await auditoria.HistorialAsync(Entidad, id, ct));

    /// <summary>
    /// Crea el embarque de una venta de exportación: hereda cliente, comercial, aerolínea y AWB; toma lista,
    /// descuento y formato del cliente; copia tarifas y cargos vigentes; y trae las especies del pedido de
    /// cliente si ya existe.
    /// </summary>
    public async Task<Resultado<Creado>> CrearAsync(CrearEmbarqueRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await ventas.ObtenerAsync(r.VentaId, ct) is not { } venta || !alcance.PuedeVer(venta.ComercialId))
        {
            return Error.Validacion("La venta no existe.");
        }

        if (venta.TipoVenta != TiposVenta.Exportacion)
        {
            return Error.Validacion("Solo las ventas de exportación llevan embarque y documentos de exportación.");
        }

        if (await embarques.IdPorVentaAsync(venta.Id, ct) is { } existente)
        {
            var otro = await embarques.ObtenerAsync(existente, ct);
            return Error.Conflicto($"La venta {venta.Codigo} ya tiene el embarque {otro?.Codigo}.");
        }

        var cliente = await configuracion.ClienteAsync(venta.ClienteId, ct);
        var listaId = cliente?.ListaPreciosId ?? (await ListaEstandarAsync(ct))?.Id;
        var cargos = (await configuracion.CargosAsync(ct)).Select(c => new CargoEmbarque(c.Codigo, c.Nombre, 0, c.PrecioUsd)).ToList();
        var pedido = await pedidos.PorVentaAsync(venta.Id, ct);
        var ahora = reloj.AhoraUtc;
        var embarque = new Embarque
        {
            Codigo = await consecutivos.SiguienteAsync(Prefijo, ct),
            VentaId = venta.Id,
            PedidoClienteId = pedido?.Id,
            ClienteId = venta.ClienteId,
            ComercialId = venta.ComercialId,
            Estado = EstadosEmbarque.Borrador,
            ListaPreciosId = listaId,
            DescuentoPeces = cliente?.DescuentoPeces ?? 0,
            Aerolinea = venta.TransportadoraAerolinea,
            Awb = venta.NoGuiaAwb,
            FormatoFactura = cliente?.FormatoFactura ?? FormatosFactura.Jrch,
            Tarifas = JsonSerializer.Serialize(await configuracion.TarifasAsync(ct), Json),
            Cargos = JsonSerializer.Serialize(cargos, Json),
            CajasEstimadas = "[]",
            CajasReales = "[]",
            CreadoEn = ahora,
        };
        await embarques.CrearAsync(embarque, ahora, ct);

        // Las especies del pedido de cliente (si la venta ya lo tiene) son el punto de partida.
        if (pedido is not null)
        {
            var lineasPedido = await pedidos.LineasAsync(pedido.Id, ct);
            var preciosLista = await PreciosAsync(listaId, ct);
            var lineas = lineasPedido.Select((l, i) => new EmbarqueLinea
            {
                EmbarqueId = embarque.Id,
                Orden = i + 1,
                ProductoId = l.ProductoId,
                Cantidad = l.Cantidad,
                Precio = l.PrecioUnitario ?? preciosLista.GetValueOrDefault(l.ProductoId),
            }).ToList();
            if (lineas.Count > 0)
            {
                await embarques.GuardarLineasAsync(embarque.Id, lineas, ahora, ct);
            }
        }

        await auditoria.CreacionAsync(Entidad, embarque.Id, new { embarque.Codigo, Venta = venta.Codigo, Cliente = venta.ClienteNombre }, ct: ct);
        return new Creado(embarque.Id, embarque.Codigo);
    }

    /// <summary>Datos generales: vuelo, AWB, lista, descuento, tarifas, cargos y cajas. Facturado: solo anticipo y notas.</summary>
    public async Task<Resultado<Guid>> ActualizarAsync(Guid id, GuardarEmbarqueRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await Visible(id, alcance, ct) is not { } e)
        {
            return NoExiste();
        }

        if (e.Estado == EstadosEmbarque.Cancelado)
        {
            return Error.Conflicto("El embarque está cancelado.");
        }

        var tipos = await TiposCajaAsync(ct);
        var editable = EstadosEmbarque.Editables.Contains(e.Estado);
        var tarifas = r.Tarifas ?? Deserializar<TarifasFleteInfo>(e.Tarifas)!;
        var cargos = r.Cargos ?? Deserializar<List<CargoEmbarque>>(e.Cargos)!;
        var estimadas = r.CajasEstimadas ?? Deserializar<List<CajaCantidad>>(e.CajasEstimadas)!;
        var reales = r.CajasReales ?? Deserializar<List<CajaCantidad>>(e.CajasReales)!;
        var formato = r.FormatoFactura ?? e.FormatoFactura;
        var error = Primero(
            r.DescuentoPeces is < 0 or > 100 ? Error.Validacion("El descuento va de 0 a 100 %.") : null,
            r.AnticipoRecibido is < 0 or > 10_000_000 ? Error.Validacion("El anticipo no es válido.") : null,
            r.AwbValor is < 0 or > 10_000_000 ? Error.Validacion("El valor del AWB no es válido.") : null,
            FormatosFactura.Todos.Contains(formato) ? null : Error.Validacion("El formato de factura no es válido."),
            Opcional(Limpiar(r.Aerolinea), "Aerolínea", 150),
            Opcional(Limpiar(r.Ruta), "Ruta", 150),
            Opcional(Limpiar(r.Awb), "AWB", 50),
            Opcional(Limpiar(r.Destino), "Destino", 200),
            Opcional(LimpiarTextoLargo(r.Notas), "Notas", 2000),
            r.SalidaVuelo is { } s && r.LlegadaVuelo is { } l && l < s ? Error.Validacion("La llegada del vuelo no puede ser antes de la salida.") : null,
            ConfiguracionExportacionService.ValidarTarifas(tarifas) is { } et ? Error.Validacion(et) : null,
            ReglasEmbarque.ValidarCargos(cargos) is { } ec ? Error.Validacion(ec) : null,
            ReglasEmbarque.ValidarCajas(estimadas, tipos) is { } ee ? Error.Validacion(ee) : null,
            ReglasEmbarque.ValidarCajas(reales, tipos) is { } er ? Error.Validacion(er) : null);
        if (error is not null)
        {
            return error;
        }

        if (r.ListaPreciosId is { } lista && lista != e.ListaPreciosId && await precios.ObtenerListaAsync(lista, ct) is not { EsCosto: false })
        {
            return Error.Validacion("Elige una lista de precios de venta existente.");
        }

        var actualizado = Entidad_(e);
        if (editable)
        {
            actualizado.ListaPreciosId = r.ListaPreciosId;
            actualizado.DescuentoPeces = r.DescuentoPeces;
            actualizado.Aerolinea = Limpiar(r.Aerolinea);
            actualizado.Ruta = Limpiar(r.Ruta);
            actualizado.Awb = Limpiar(r.Awb);
            actualizado.Destino = Limpiar(r.Destino);
            actualizado.SalidaVuelo = r.SalidaVuelo;
            actualizado.LlegadaVuelo = r.LlegadaVuelo;
            actualizado.FechaDespacho = r.FechaDespacho;
            actualizado.FormatoFactura = formato;
            actualizado.AwbValor = r.AwbValor;
            actualizado.Tarifas = JsonSerializer.Serialize(tarifas, Json);
            actualizado.Cargos = JsonSerializer.Serialize(cargos, Json);
            actualizado.CajasEstimadas = JsonSerializer.Serialize(estimadas.Where(c => c.Cantidad > 0), Json);
            actualizado.CajasReales = JsonSerializer.Serialize(reales.Where(c => c.Cantidad > 0), Json);
        }

        // Después de facturar, la factura no cambia: solo el anticipo recibido y las notas.
        actualizado.AnticipoRecibido = r.AnticipoRecibido;
        actualizado.Notas = LimpiarTextoLargo(r.Notas);

        await embarques.ActualizarAsync(actualizado, reloj.AhoraUtc, ct);
        var nuevo = await embarques.ObtenerAsync(id, ct);
        await auditoria.CambiosAsync(Entidad, id, ComparadorAuditoria.Comparar(Auditable(e), Auditable(nuevo!)), ct);
        if (e.Estado is EstadosEmbarque.Facturado or EstadosEmbarque.Cerrado)
        {
            await SincronizarCrmAsync(nuevo!, ct);
        }

        return id;
    }

    /// <summary>
    /// Especies pedidas (preinvoice y orden de cargue). Sin precio se toma el de la lista del embarque. Se
    /// sincronizan con el pedido de cliente (lo crea si la venta no tenía) para que bodega despache contra ellas.
    /// </summary>
    public async Task<Resultado<IReadOnlyList<string>>> GuardarLineasAsync(
        Guid id, GuardarLineasRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await Visible(id, alcance, ct) is not { } e)
        {
            return NoExiste();
        }

        if (!EstadosEmbarque.Editables.Contains(e.Estado))
        {
            return Error.Conflicto("El embarque ya está facturado: las especies no cambian.");
        }

        var lineas = r.Lineas ?? [];
        if (ReglasEmbarque.ValidarLineas(lineas) is { } error)
        {
            return Error.Validacion(error);
        }

        var catalogo = (await productos.ListarAsync(ct)).ToDictionary(p => p.Id);
        if (lineas.FirstOrDefault(l => !catalogo.ContainsKey(l.ProductoId)) is not null)
        {
            return Error.Validacion("Hay una especie que no existe en el catálogo.");
        }

        var preciosLista = await PreciosAsync(e.ListaPreciosId, ct);
        var entidades = lineas.Select((l, i) => new EmbarqueLinea
        {
            EmbarqueId = id,
            Orden = i + 1,
            ProductoId = l.ProductoId,
            Cantidad = l.Cantidad,
            Precio = l.Precio ?? preciosLista.GetValueOrDefault(l.ProductoId),
            FraccionCaja = l.FraccionCaja,
            Empaque = Limpiar(l.Empaque),
            Nota = Limpiar(l.Nota),
        }).ToList();
        var anteriores = await embarques.LineasAsync(id, ct);
        await embarques.GuardarLineasAsync(id, entidades, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(
            Entidad, id,
            [new CambioAuditado("Especies", ResumenLineas(anteriores.Select(l => (l.ProductoCodigo, l.Cantidad, l.Precio))),
                ResumenLineas(entidades.Select(l => (catalogo[l.ProductoId].Codigo, l.Cantidad, l.Precio))))],
            ct);

        var avisos = new List<string>();
        if (entidades.Any(l => l.Precio == 0))
        {
            avisos.Add("Hay especies sin precio en la lista: escribe el precio.");
        }

        if (await SincronizarPedidoAsync(e, entidades, alcance, ct) is { } aviso)
        {
            avisos.Add(aviso);
        }

        // Una especie sin código AUNAP no se puede declarar: mejor saberlo al armar el pedido que al despachar.
        var mapaAunap = await aunap.MapaAsync(ct);
        var sinAunap = entidades.Where(l => mapaAunap.GetValueOrDefault(l.ProductoId)?.EspecieAunapId is null)
            .Select(l => $"{catalogo[l.ProductoId].Codigo} {catalogo[l.ProductoId].NombreCientifico}").ToList();
        if (sinAunap.Count > 0)
        {
            avisos.Add($"Sin código AUNAP (no se pueden declarar ni exportar): {string.Join(", ", sinAunap)}.");
        }

        return avisos;
    }

    /// <summary>Empaque real caja por caja (bodega). Sin precio se toma el de la línea del embarque o el de la lista.</summary>
    public async Task<Resultado<Guid>> GuardarEmpaqueAsync(Guid id, GuardarEmpaqueRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await Visible(id, alcance, ct) is not { } e)
        {
            return NoExiste();
        }

        if (!EstadosEmbarque.Editables.Contains(e.Estado))
        {
            return Error.Conflicto("El embarque ya está facturado: el empaque no cambia.");
        }

        var filas = r.Filas ?? [];
        if (ReglasEmbarque.ValidarEmpaque(filas) is { } error)
        {
            return Error.Validacion(error);
        }

        var catalogo = (await productos.ListarAsync(ct)).Select(p => p.Id).ToHashSet();
        var bolsas = (await configuracion.TiposBolsaAsync(ct)).Select(b => b.Id).ToHashSet();
        if (filas.Any(f => !catalogo.Contains(f.ProductoId)))
        {
            return Error.Validacion("Hay una especie que no existe en el catálogo.");
        }

        if (filas.Any(f => f.TipoBolsaId is { } b && !bolsas.Contains(b)))
        {
            return Error.Validacion("Hay un tipo de bolsa que no existe.");
        }

        var preciosLinea = (await embarques.LineasAsync(id, ct)).GroupBy(l => l.ProductoId).ToDictionary(g => g.Key, g => g.First().Precio);
        var preciosLista = await PreciosAsync(e.ListaPreciosId, ct);
        var entidades = filas.Select((f, i) => new EmbarqueEmpaque
        {
            EmbarqueId = id,
            NumeroCaja = f.NumeroCaja,
            Orden = i + 1,
            ProductoId = f.ProductoId,
            NombreFactura = Limpiar(f.NombreFactura),
            TipoBolsaId = f.TipoBolsaId,
            Bolsas = f.Bolsas,
            PecesPorBolsa = f.PecesPorBolsa,
            Precio = f.Precio ?? (preciosLinea.TryGetValue(f.ProductoId, out var pl) ? pl : preciosLista.GetValueOrDefault(f.ProductoId)),
            Nota = Limpiar(f.Nota),
        }).ToList();
        await embarques.GuardarEmpaqueAsync(id, entidades, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(
            Entidad, id,
            [new CambioAuditado("Empaque", null, $"{entidades.Select(f => f.NumeroCaja).Distinct().Count()} cajas · {entidades.Sum(f => f.Bolsas * f.PecesPorBolsa)} peces")],
            ct);
        return id;
    }

    /// <summary>Muertos reportados por el cliente (DOA, ADD, FINAL) por fila del empaque. Actualiza la nota crédito y el tracking.</summary>
    public async Task<Resultado<Guid>> GuardarDoaAsync(Guid id, GuardarDoaRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await Visible(id, alcance, ct) is not { } e)
        {
            return NoExiste();
        }

        if (e.Estado is not (EstadosEmbarque.Facturado or EstadosEmbarque.Cerrado))
        {
            return Error.Conflicto("El DOA se registra después de emitir la factura.");
        }

        var filas = r.Filas ?? [];
        var empaque = (await embarques.EmpaqueAsync(id, ct)).ToDictionary(f => f.Id);
        foreach (var f in filas)
        {
            if (!empaque.TryGetValue(f.Id, out var fila))
            {
                return Error.Validacion("Hay una fila de empaque que no pertenece al embarque.");
            }

            if (f.Doa < 0 || f.DoaAdicional < 0 || f.DoaFinal < 0)
            {
                return Error.Validacion("Los muertos no pueden ser negativos.");
            }

            if (f.Doa + f.DoaAdicional + f.DoaFinal > fila.Cantidad)
            {
                return Error.Validacion($"Caja {fila.NumeroCaja}, {fila.ProductoCodigo}: los muertos superan los {fila.Cantidad} enviados.");
            }
        }

        var antes = empaque.Values.Sum(f => f.Muertos);
        await embarques.GuardarDoaAsync(id, filas, reloj.AhoraUtc, ct);
        if (e.NotaCreditoFecha is null && filas.Any(f => f.Doa + f.DoaAdicional + f.DoaFinal > 0))
        {
            await embarques.RegistrarNotaCreditoAsync(id, reloj.Hoy, reloj.AhoraUtc, ct);
        }

        await auditoria.CambiosAsync(Entidad, id, [new CambioAuditado("DOA", antes.ToString(), filas.Sum(f => f.Doa + f.DoaAdicional + f.DoaFinal).ToString())], ct);
        await SincronizarCrmAsync((await embarques.ObtenerAsync(id, ct))!, ct);
        return id;
    }

    public async Task<Resultado<Guid>> CambiarEstadoAsync(Guid id, CambiarEstadoEmbarqueRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await Visible(id, alcance, ct) is not { } e)
        {
            return NoExiste();
        }

        if (r.Estado is null || !EstadosEmbarque.Todos.Contains(r.Estado))
        {
            return Error.Validacion("El estado no es válido.");
        }

        var actual = r.EstadoActual ?? e.Estado;
        if (!ReglasEmbarque.TransicionManualValida(actual, r.Estado))
        {
            return Error.Validacion($"El embarque no puede pasar de «{actual}» a «{r.Estado}».");
        }

        if (r.Estado == EstadosEmbarque.Empacado && (await embarques.EmpaqueAsync(id, ct)).Count == 0)
        {
            return Error.Validacion("Registra el empaque (cajas y bolsas) antes de marcarlo como empacado.");
        }

        if (await embarques.CambiarEstadoAsync(id, actual, r.Estado, reloj.AhoraUtc, ct) is { } falla)
        {
            return FallasInventario.AError(falla);
        }

        await auditoria.CambiosAsync(Entidad, id, [new CambioAuditado("Estado", actual, r.Estado)], ct);
        return id;
    }

    /// <summary>
    /// Emite la factura: asigna el número (consecutivo propio), guarda el PDF en el almacén (no cambia más)
    /// y lleva el total, las cajas, el FOB y el flete a la venta y a la torre de vuelos.
    /// </summary>
    public async Task<Resultado<int>> FacturarAsync(Guid id, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await Visible(id, alcance, ct) is not { } e)
        {
            return NoExiste();
        }

        if (e.Estado != EstadosEmbarque.Empacado)
        {
            return Error.Validacion("La factura se emite cuando el embarque está empacado.");
        }

        if (string.IsNullOrWhiteSpace(e.Awb) || e.AwbValor is null)
        {
            return Error.Validacion("Antes de facturar registra el número de guía (AWB) y su valor.");
        }

        if (!Deserializar<List<CajaCantidad>>(e.CajasReales)!.Any(c => c.Cantidad > 0))
        {
            return Error.Validacion("Indica las cajas reales despachadas (por tipo) antes de facturar.");
        }

        var (numero, falla) = await embarques.FacturarAsync(id, e.Estado, reloj.Hoy, reloj.AhoraUtc, ct);
        if (falla is not null)
        {
            return FallasInventario.AError(falla);
        }

        var facturado = (await embarques.ObtenerAsync(id, ct))!;
        var pdf = await GenerarAsync(facturado, TiposDocumentoEmbarque.Factura, ct);
        await GuardarDocumentoAsync(facturado, TiposDocumentoEmbarque.Factura, numero!.Value.ToString(), pdf, ct);
        await auditoria.CambiosAsync(Entidad, id, [new CambioAuditado("Factura", null, numero.Value.ToString()), new CambioAuditado("Estado", e.Estado, EstadosEmbarque.Facturado)], ct);
        await SincronizarCrmAsync(facturado, ct);
        return numero.Value;
    }

    /// <summary>
    /// PDF de un documento. La factura emitida se sirve tal como se guardó; antes de emitirla sale como
    /// borrador. La primera vez que se genera la preinvoice recibe su número (año-día).
    /// </summary>
    public async Task<Resultado<InformePdf>> DocumentoAsync(Guid id, string tipo, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await Visible(id, alcance, ct) is not { } e)
        {
            return NoExiste();
        }

        if (!TiposDocumentoEmbarque.Todos.Contains(tipo))
        {
            return Error.NoEncontrado("El documento no existe.");
        }

        if (tipo == TiposDocumentoEmbarque.Factura && e.FacturaNumero is { } numero
            && (await embarques.DocumentosAsync(id, ct)).FirstOrDefault(d => d.Tipo == TiposDocumentoEmbarque.Factura) is { } guardado
            && await almacen.DescargarAsync(guardado.ArchivoClave, ct) is { } archivo)
        {
            await using var contenido = archivo.Contenido;
            using var buffer = new MemoryStream();
            await contenido.CopyToAsync(buffer, ct);
            return new InformePdf(buffer.ToArray(), NombreArchivo(e, tipo, numero.ToString()));
        }

        if (TiposDocumentoEmbarque.Autoridades.Contains(tipo) && await ValidarAutoridadesAsync(e, tipo, ct) is { } invalido)
        {
            return invalido;
        }

        if (tipo == TiposDocumentoEmbarque.Preinvoice && e.PreinvoiceNumero is null)
        {
            await embarques.RegistrarPreinvoiceAsync(id, ReglasEmbarque.NumeroPreinvoice(reloj.Hoy), reloj.Hoy, reloj.AhoraUtc, ct);
            e = (await embarques.ObtenerAsync(id, ct))!;
        }

        var bytes = await GenerarAsync(e, tipo, ct);
        var numeroArchivo = tipo == TiposDocumentoEmbarque.SolicitudAunap ? (await aunap.TramitesAsync(e.Id, ct))?.ProformaNumero : null;
        return new InformePdf(bytes, NombreArchivo(e, tipo, numeroArchivo));
    }

    /// <summary>Envía un documento por correo (al email del cliente si no se indica otro) y deja registro.</summary>
    public async Task<Resultado<EnvioDocumentoFila>> EnviarAsync(
        Guid id, string tipo, EnviarDocumentoRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await Visible(id, alcance, ct) is not { } e)
        {
            return NoExiste();
        }

        var para = Limpiar(r.Para)?.ToLowerInvariant() ?? (await clientes.ObtenerAsync(e.ClienteId, ct))?.Email?.ToLowerInvariant();
        if (para is null || !EmailValido(para))
        {
            return Error.Validacion("Indica un correo válido (el cliente no tiene correo registrado).");
        }

        var documento = await DocumentoAsync(id, tipo, alcance, ct);
        if (!documento.Exitoso)
        {
            return documento.Error!;
        }

        e = (await embarques.ObtenerAsync(id, ct))!;
        var empresa = await configuracion.DatosEmpresaAsync(ct);
        var asunto = $"{NombreDocumentoIngles(tipo)} {NumeroDocumento(e, tipo)} · {empresa.RazonSocial}";
        var cuerpo = $"""
            Dear {e.ClienteNombre},

            Please find attached the {NombreDocumentoIngles(tipo).ToLowerInvariant()} {NumeroDocumento(e, tipo)}.
            {(string.IsNullOrWhiteSpace(r.Mensaje) ? "" : Environment.NewLine + r.Mensaje!.Trim() + Environment.NewLine)}
            Sincerely,
            {empresa.FirmanteNombre ?? empresa.RazonSocial}
            {empresa.RazonSocial}
            """;
        var resultado = await correo.EnviarAsync(
            new MensajeCorreo(para, e.ClienteNombre, asunto, cuerpo, Adjuntos: [new AdjuntoCorreo(documento.Valor.NombreArchivo, "application/pdf", documento.Valor.Contenido)]),
            ct);
        var envio = new EnvioDocumento
        {
            EmbarqueId = id,
            TipoDocumento = tipo,
            Para = para,
            Asunto = asunto,
            Proveedor = correo.Proveedor,
            Exitoso = resultado.Exitoso,
            LlegoAlDestinatario = resultado.LlegoAlDestinatario,
            MensajeId = resultado.MensajeId,
            Error = resultado.Error,
            EnviadoPor = usuario.Nombre,
        };
        await embarques.RegistrarEnvioAsync(envio, reloj.AhoraUtc, ct);

        // Enviar la preinvoice al cliente la deja «enviada».
        if (resultado.Exitoso && tipo == TiposDocumentoEmbarque.Preinvoice && e.Estado == EstadosEmbarque.Borrador)
        {
            await embarques.CambiarEstadoAsync(id, e.Estado, EstadosEmbarque.PreinvoiceEnviada, reloj.AhoraUtc, ct);
            await auditoria.CambiosAsync(Entidad, id, [new CambioAuditado("Estado", e.Estado, EstadosEmbarque.PreinvoiceEnviada)], ct);
        }

        if (!resultado.Exitoso)
        {
            return Error.Validacion($"No se pudo enviar el correo: {resultado.Error}");
        }

        return new EnvioDocumentoFila(
            envio.Id, tipo, para, asunto, envio.Proveedor, true, resultado.LlegoAlDestinatario, null, usuario.Nombre, reloj.AhoraUtc);
    }

    // --- Internos ---

    private async Task<EmbarqueDetalle> DetalleAsync(EmbarqueFila e, CancellationToken ct)
    {
        var tarifas = Deserializar<TarifasFleteInfo>(e.Tarifas)!;
        var cargos = Deserializar<List<CargoEmbarque>>(e.Cargos)!;
        var estimadas = Deserializar<List<CajaCantidad>>(e.CajasEstimadas)!;
        var reales = Deserializar<List<CajaCantidad>>(e.CajasReales)!;
        var lineas = await embarques.LineasAsync(e.Id, ct);
        var empaque = await embarques.EmpaqueAsync(e.Id, ct);
        var tipos = await TiposCajaAsync(ct);
        return new EmbarqueDetalle(
            e, tarifas, cargos, estimadas, reales, lineas, empaque,
            await embarques.DocumentosAsync(e.Id, ct),
            await embarques.EnviosAsync(e.Id, ct),
            ReglasEmbarque.Calcular(e, tarifas, cargos, estimadas, reales, lineas, empaque, tipos),
            (await clientes.ObtenerAsync(e.ClienteId, ct))?.Email);
    }

    private async Task<byte[]> GenerarAsync(EmbarqueFila e, string tipo, CancellationToken ct)
    {
        var detalle = await DetalleAsync(e, ct);
        var empresa = await configuracion.DatosEmpresaAsync(ct);
        var cliente = await clientes.ObtenerAsync(e.ClienteId, ct);
        var exportacion = await configuracion.ClienteAsync(e.ClienteId, ct);
        var logo = await servicioConfiguracion.LogoAsync(ct);
        var datos = new DatosDocumento(detalle, empresa, cliente, exportacion, await TiposCajaAsync(ct), logo, usuario.Nombre ?? "", reloj.AhoraUtc)
        {
            Aunap = TiposDocumentoEmbarque.Autoridades.Contains(tipo) ? await aunap.MapaAsync(ct) : new Dictionary<Guid, ProductoAunapFila>(),
            Tramites = TiposDocumentoEmbarque.Autoridades.Contains(tipo) ? await aunap.TramitesAsync(e.Id, ct) : null,
        };
        return generadorPdf.Generar(DocumentosEmbarque.Construir(tipo, datos));
    }

    /// <summary>
    /// Los documentos para autoridades salen de lo real: la solicitud, de las especies del pedido; los de aduana y
    /// aeropuerto, del empaque. Toda especie debe tener código AUNAP (no se declara una especie por otra).
    /// </summary>
    private async Task<Error?> ValidarAutoridadesAsync(EmbarqueFila e, string tipo, CancellationToken ct)
    {
        var mapa = await aunap.MapaAsync(ct);
        if (tipo == TiposDocumentoEmbarque.SolicitudAunap)
        {
            var lineas = await embarques.LineasAsync(e.Id, ct);
            if (lineas.Count == 0)
            {
                return Error.Validacion("El embarque no tiene especies.");
            }

            if ((await aunap.TramitesAsync(e.Id, ct))?.ProformaNumero is null)
            {
                return Error.Validacion("Primero genera la proforma en Trámites (cuando el cliente aprueba la preinvoice).");
            }

            var (_, error) = ReglasAunap.Agrupar(lineas.Select(l => (l.ProductoId, l.ProductoCodigo, l.NombreCientifico, l.Cantidad, l.Total)), mapa);
            return error is null ? null : Error.Validacion(error);
        }

        var empaque = await embarques.EmpaqueAsync(e.Id, ct);
        if (empaque.Count == 0)
        {
            return Error.Validacion("Primero se registra el empaque real: la lista de aduana y aeropuerto sale de él.");
        }

        var (_, errorEmpaque) = ReglasAunap.Agrupar(empaque.Select(x => (x.ProductoId, x.ProductoCodigo, x.NombreCientifico, x.Cantidad, x.Total)), mapa);
        return errorEmpaque is null ? null : Error.Validacion(errorEmpaque);
    }

    private async Task GuardarDocumentoAsync(EmbarqueFila e, string tipo, string numero, byte[] pdf, CancellationToken ct)
    {
        var documentoId = Guid.CreateVersion7();
        var clave = ClaveArchivo.ParaDocumento("embarques", e.Id, documentoId, "application/pdf");
        using var contenido = new MemoryStream(pdf);
        await almacen.SubirAsync(clave, contenido, "application/pdf", ct);
        await embarques.RegistrarDocumentoAsync(
            new DocumentoEmbarque { Id = documentoId, EmbarqueId = e.Id, Tipo = tipo, Numero = numero, ArchivoClave = clave, GeneradoPor = usuario.Nombre },
            reloj.AhoraUtc, ct);
    }

    /// <summary>La factura y el DOA llevados a la venta (total, cajas) y a la torre de vuelos (FOB, flete, DOA, NC).</summary>
    private async Task SincronizarCrmAsync(EmbarqueFila e, CancellationToken ct)
    {
        if (e.FacturaNumero is null)
        {
            return;
        }

        var c = (await DetalleAsync(e, ct)).Calculos;
        var notaCredito = c.NotaCredito.Total > 0 ? $"{e.FacturaNumero}-01" : null;
        await embarques.SincronizarCrmAsync(
            e.Id, c.Factura.Total, c.Factura.Cajas, c.Factura.SubtotalPeces - c.Factura.Descuento, c.Factura.Awb, c.NotaCredito.Total,
            notaCredito, reloj.AhoraUtc, ct);
    }

    /// <summary>Lleva las especies al pedido de cliente de la venta (lo crea si no existe) para que bodega despache.</summary>
    private async Task<string?> SincronizarPedidoAsync(EmbarqueFila e, IReadOnlyList<EmbarqueLinea> lineas, AlcanceCrm alcance, CancellationToken ct)
    {
        var pedido = e.PedidoClienteId is { } pid ? await pedidos.ObtenerAsync(pid, ct) : await pedidos.PorVentaAsync(e.VentaId, ct);
        if (pedido is not null && pedido.Estado is EstadosPedidoCliente.Despachado or EstadosPedidoCliente.Cancelado)
        {
            return $"El pedido {pedido.Codigo} ya está {pedido.Estado}: sus especies no se actualizaron.";
        }

        var existentes = pedido is null ? [] : await pedidos.LineasAsync(pedido.Id, ct);
        var lineasPedido = lineas
            .Select(l => new LineaPedidoCliente(existentes.FirstOrDefault(x => x.ProductoId == l.ProductoId)?.Id, l.ProductoId, l.Cantidad, l.Precio, l.Nota))
            .ToList();
        // Lo ya despachado no se puede quitar del pedido: se conserva aunque el embarque ya no lo tenga.
        lineasPedido.AddRange(existentes
            .Where(x => x.Despachada > 0 && lineas.All(l => l.ProductoId != x.ProductoId))
            .Select(x => new LineaPedidoCliente(x.Id, x.ProductoId, x.Cantidad, x.PrecioUnitario, x.Observacion)));

        var solicitud = new GuardarPedidoClienteRequest(pedido is null ? e.VentaId : null, null, null, null, null, $"Embarque {e.Codigo}", lineasPedido);
        if (pedido is null)
        {
            var creado = await servicioPedidos.CrearAsync(solicitud, alcance, ct);
            if (!creado.Exitoso)
            {
                return $"No se pudo crear el pedido de cliente: {creado.Error!.Mensaje}";
            }

            var actualizado = Entidad_(e);
            actualizado.PedidoClienteId = creado.Valor.Id;
            await embarques.ActualizarAsync(actualizado, reloj.AhoraUtc, ct);
            return null;
        }

        var resultado = await servicioPedidos.ActualizarAsync(pedido.Id, solicitud with { Observaciones = pedido.Observaciones ?? $"Embarque {e.Codigo}" }, alcance, ct);
        return resultado.Exitoso ? null : $"El pedido {pedido.Codigo} no se actualizó: {resultado.Error!.Mensaje}";
    }

    private async Task<ListaPreciosFila?> ListaEstandarAsync(CancellationToken ct) =>
        (await precios.ListarListasAsync(ct)).FirstOrDefault(l => l.Codigo == CodigoListaEstandar);

    private async Task<Dictionary<Guid, decimal>> PreciosAsync(Guid? listaId, CancellationToken ct) =>
        listaId is { } id ? (await precios.PreciosAsync(null, id, ct)).ToDictionary(p => p.ProductoId, p => p.Precio) : [];

    private async Task<Dictionary<Guid, TipoCajaFila>> TiposCajaAsync(CancellationToken ct) =>
        (await configuracion.TiposCajaAsync(ct)).ToDictionary(t => t.Id);

    private async Task<EmbarqueFila?> Visible(Guid id, AlcanceCrm alcance, CancellationToken ct) =>
        await embarques.ObtenerAsync(id, ct) is { } e && alcance.PuedeVer(e.ComercialId) ? e : null;

    private static Error NoExiste() => Error.NoEncontrado("El embarque no existe.");

    private static T? Deserializar<T>(string json) => JsonSerializer.Deserialize<T>(json, Json);

    /// <summary>La fila leída como entidad, para reescribirla completa.</summary>
    private static Embarque Entidad_(EmbarqueFila e) => new()
    {
        Id = e.Id,
        Codigo = e.Codigo,
        VentaId = e.VentaId,
        PedidoClienteId = e.PedidoClienteId,
        ClienteId = e.ClienteId,
        ComercialId = e.ComercialId,
        Estado = e.Estado,
        ListaPreciosId = e.ListaPreciosId,
        DescuentoPeces = e.DescuentoPeces,
        Aerolinea = e.Aerolinea,
        Ruta = e.Ruta,
        Awb = e.Awb,
        Destino = e.Destino,
        SalidaVuelo = e.SalidaVuelo,
        LlegadaVuelo = e.LlegadaVuelo,
        FechaDespacho = e.FechaDespacho,
        PreinvoiceNumero = e.PreinvoiceNumero,
        PreinvoiceFecha = e.PreinvoiceFecha,
        FormatoFactura = e.FormatoFactura,
        FacturaNumero = e.FacturaNumero,
        FacturaFecha = e.FacturaFecha,
        NotaCreditoFecha = e.NotaCreditoFecha,
        AwbValor = e.AwbValor,
        AnticipoRecibido = e.AnticipoRecibido,
        Tarifas = e.Tarifas,
        Cargos = e.Cargos,
        CajasEstimadas = e.CajasEstimadas,
        CajasReales = e.CajasReales,
        Notas = e.Notas,
    };

    private static EmbarqueAuditable Auditable(EmbarqueFila e) =>
        new(e.Estado, e.ListaNombre, e.DescuentoPeces, e.Aerolinea, e.Ruta, e.Awb, e.Destino, e.SalidaVuelo, e.LlegadaVuelo, e.FechaDespacho,
            e.FormatoFactura, e.AwbValor, e.AnticipoRecibido, e.Tarifas, e.Cargos, e.CajasEstimadas, e.CajasReales, e.Notas);

    private static string ResumenLineas(IEnumerable<(string Codigo, int Cantidad, decimal Precio)> lineas) =>
        string.Join(", ", lineas.Select(l => $"{l.Codigo} × {l.Cantidad} @ {l.Precio:0.##}"));

    public static string NumeroDocumento(EmbarqueFila e, string tipo) => tipo switch
    {
        TiposDocumentoEmbarque.Preinvoice or TiposDocumentoEmbarque.SolicitudAunap => e.PreinvoiceNumero ?? e.Codigo,
        TiposDocumentoEmbarque.Factura => e.FacturaNumero?.ToString() ?? $"{e.Codigo} (draft)",
        TiposDocumentoEmbarque.NotaCredito => e.FacturaNumero is { } n ? $"{n}-01" : e.Codigo,
        _ => e.FacturaNumero?.ToString() ?? e.Codigo,
    };

    public static string NombreDocumentoIngles(string tipo) => tipo switch
    {
        TiposDocumentoEmbarque.Preinvoice => "Preinvoice",
        TiposDocumentoEmbarque.Flete => "Estimated freight costs",
        TiposDocumentoEmbarque.OrdenCargue => "Orden de cargue",
        TiposDocumentoEmbarque.Empaque => "Packing list",
        TiposDocumentoEmbarque.Factura => "Invoice",
        TiposDocumentoEmbarque.NotaCredito => "Credit note",
        TiposDocumentoEmbarque.SolicitudAunap => "Proforma",
        TiposDocumentoEmbarque.PackingAduana => "Customs packing list",
        TiposDocumentoEmbarque.FacturaAduana => "Customs invoice",
        _ => "Balance",
    };

    private static string NombreArchivo(EmbarqueFila e, string tipo, string? numero) =>
        DetectorTipoArchivo.SanearNombre($"{NombreDocumentoIngles(tipo)} {numero ?? NumeroDocumento(e, tipo)} {e.ClienteNombre}.pdf");
}
