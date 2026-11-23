using System.Globalization;
using JRTropicalFish.Application.Crm;
using JRTropicalFish.Application.Informes;
using JRTropicalFish.Domain.Exportaciones;

namespace JRTropicalFish.Application.Exportaciones;

/// <summary>Todo lo que un documento del embarque necesita para dibujarse.</summary>
public sealed record DatosDocumento(
    EmbarqueDetalle Detalle,
    DatosEmpresaInfo Empresa,
    ClienteFila? Cliente,
    ClienteExportacionInfo? ClienteExportacion,
    IReadOnlyDictionary<Guid, TipoCajaFila> TiposCaja,
    byte[]? Logo,
    string GeneradoPor,
    DateTime AhoraUtc)
{
    /// <summary>Producto → especie AUNAP (solo para los documentos para autoridades).</summary>
    public IReadOnlyDictionary<Guid, ProductoAunapFila> Aunap { get; init; } = new Dictionary<Guid, ProductoAunapFila>();
}

/// <summary>
/// Arma el <see cref="InformeModelo"/> de cada documento del embarque, con el contenido de las hojas del
/// libro de formatos. Los que van al cliente están en inglés (como hoy); la orden de cargue, en español
/// para bodega. Las cifras salen de <see cref="ReglasEmbarque"/>: la ficha y el PDF muestran lo mismo.
/// </summary>
public static class DocumentosEmbarque
{
    private static readonly CultureInfo Ingles = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo Espanol = CultureInfo.GetCultureInfo("es-CO");

    private static readonly Alineacion I = Alineacion.Izquierda;
    private static readonly Alineacion D = Alineacion.Derecha;
    private static readonly Alineacion C = Alineacion.Centro;

    public static InformeModelo Construir(string tipo, DatosDocumento d) => tipo switch
    {
        TiposDocumentoEmbarque.Preinvoice => Preinvoice(d),
        TiposDocumentoEmbarque.Flete => Flete(d),
        TiposDocumentoEmbarque.OrdenCargue => OrdenCargue(d),
        TiposDocumentoEmbarque.Empaque => Empaque(d),
        TiposDocumentoEmbarque.Factura => Factura(d),
        TiposDocumentoEmbarque.NotaCredito => NotaCredito(d),
        TiposDocumentoEmbarque.Balance => Balance(d),
        TiposDocumentoEmbarque.SolicitudAunap => SolicitudAunap(d),
        TiposDocumentoEmbarque.PackingAduana => PackingAduana(d),
        TiposDocumentoEmbarque.FacturaAduana => FacturaAduana(d),
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Documento desconocido."),
    };

    // --- Documentos ---

    private static InformeModelo Preinvoice(DatosDocumento d)
    {
        var e = d.Detalle.Embarque;
        var p = d.Detalle.Calculos.Preinvoice;
        var filas = d.Detalle.Lineas
            .Select(l => Fila(Num(l.FraccionCaja), l.ProductoCodigo, l.Talla ?? "NA", l.NombreCientifico, ReglasEmbarque.PrimerNombre(l.NombreComun) ?? "",
                Entero(l.Cantidad), Usd(l.Precio), Usd(l.Total)))
            .ToList();
        var totales = new List<IReadOnlyList<string>> { Fila(Num(p.CajasSugeridas), "", "", "Subtotal fishes", "", Entero(p.Unidades), "", Usd(p.SubtotalPeces)) };
        if (p.Descuento > 0)
        {
            totales.Add(Fila("", "", "", "Fish discount", "", "", Pct(e.DescuentoPeces), Usd(-p.Descuento)));
        }

        totales.AddRange(p.Cargos.Select(c => Fila("", "", "", c.Descripcion, "", Num(c.Cantidad), Usd(c.Precio), Usd(c.Total))));
        totales.Add(Fila("", "", "", "AWB estimated", "", "1", Usd(p.AwbEstimado), Usd(p.AwbEstimado)));
        totales.Add(Fila("", "", "", "TOTAL PREINVOICE", "", "", "", Usd(p.Total)));

        return Documento(d, "PREINVOICE", $"N° {e.PreinvoiceNumero ?? e.Codigo}",
        [
            Encabezado(d, "Preinvoice N°", e.PreinvoiceNumero ?? e.Codigo, e.PreinvoiceFecha),
            new SeccionTabla("", ["BOX", "COD", "SIZE", "SCIENTIFIC NAME", "COMMON NAME", "QUANTITY", "PRICE", "TOTAL"], filas, [5, 6, 6, 17, 17, 7, 7, 8])
            {
                Alineaciones = [C, I, C, I, I, D, D, D],
                Totales = totales,
                Compacta = true,
            },
            new SeccionTabla("Advance payment", ["Concept", "Value"],
                [Fila("50% fishes value", Usd(p.AnticipoPeces)), Fila("100% AWB value", Usd(p.AnticipoAwb))], [3, 1])
            {
                Alineaciones = [I, D],
                Totales = [Fila("TOTAL ADVANCE", Usd(p.Anticipo))],
            },
            DatosBancarios(d.Empresa),
            Firma(d.Empresa),
        ]);
    }

    private static InformeModelo Flete(DatosDocumento d)
    {
        var e = d.Detalle.Embarque;
        var f = d.Detalle.Calculos.Flete;
        var horas = e.SalidaVuelo is { } s && e.LlegadaVuelo is { } l ? (l - s).TotalHours : (double?)null;
        return Documento(d, "ESTIMATED FREIGHT COSTS", e.Awb is null ? e.Codigo : $"AWB {e.Awb}",
        [
            new SeccionBloques("",
            [
                new BloqueInforme("FLIGHT", [new("Airline", e.Aerolinea ?? "To be determined"), new("Route", e.Ruta ?? "—"), new("AWB", e.Awb ?? "—")]),
                new BloqueInforme("SCHEDULE",
                [
                    new("Departure", e.SalidaVuelo?.ToString("MMM dd, yyyy HH:mm", Ingles) ?? "To be determined"),
                    new("Arrival", e.LlegadaVuelo?.ToString("MMM dd, yyyy HH:mm", Ingles) ?? "To be determined"),
                    new("Flight time", horas is { } h ? $"{h.ToString("0.0", Ingles)} h" : "—"),
                ]),
            ]),
            new SeccionTabla("Shipment weight", ["Box Q", "Box description", "Kg per box", "Kgs"],
                [.. f.Pesos.Select(x => Fila(Entero(x.Cajas), x.Descripcion, Num(x.KgPorCaja), Num(x.Kg)))], [2, 8, 3, 3])
            {
                Alineaciones = [D, I, D, D],
                Totales = [Fila("", "Total kgs", "", Num(f.Kg))],
            },
            new SeccionTabla("Costs", ["Charge", "Cost"],
            [
                Fila("Regular fee (weight)", Usd(f.CargoTarifa)),
                Fila("Fuel surcharge (weight)", Usd(f.CargoFuel)),
                Fila("JP fee (weight)", Usd(f.CargoJp)),
                Fila("VSC", Usd(f.Vsc)),
                Fila("DBC", Usd(f.Dbc)),
                Fila("DF", Usd(f.Df)),
                Fila("Due agent", Usd(f.DueAgent)),
                Fila("Due carrier (vet fee, AVI fee, MCC, MYC, bee fee, transfer fee, e-AWB, HAWB…)", Usd(f.DueCarrier)),
            ], [5, 1])
            {
                Alineaciones = [I, D],
                Totales = [Fila("ESTIMATED FREIGHT COSTS", Usd(f.Total))],
            },
            Firma(d.Empresa),
        ]);
    }

    /// <summary>Orden de cargue para bodega (en español): qué especie va en qué caja y cómo se empaca.</summary>
    private static InformeModelo OrdenCargue(DatosDocumento d)
    {
        var e = d.Detalle.Embarque;
        var filas = d.Detalle.Calculos.OrdenCargue
            .Select(o => Fila(o.Cajas, o.ProductoCodigo, o.Especie, o.Cantidad.ToString("N0", Espanol), o.Empaque ?? "", o.Nota ?? ""))
            .ToList();
        var cajas = d.Detalle.Calculos.Preinvoice.CajasSugeridas;
        return new InformeModelo("Orden de cargue", $"{e.Codigo} · {e.ClienteNombre}", d.Empresa.RazonSocial, d.GeneradoPor, d.AhoraUtc,
        [
            new SeccionCampos("",
            [
                new("Cliente", e.ClienteNombre), new("Fecha de despacho", e.FechaDespacho?.ToString("dd/MM/yyyy") ?? "—"),
                new("Aerolínea", e.Aerolinea), new("AWB", e.Awb), new("Especies", filas.Count.ToString(Espanol)),
                new("Cajas (aprox.)", Math.Ceiling(cajas).ToString(Espanol)),
            ]),
            new SeccionTabla("", ["CAJA N°", "CÓDIGO", "ESPECIE", "CANTIDAD", "EMPAQUE", "NOTAS"], filas, [3, 3, 12, 3, 4, 6])
            {
                Alineaciones = [C, I, I, D, C, I],
                Compacta = true,
            },
        ])
        { Logo = d.Logo };
    }

    private static InformeModelo Empaque(DatosDocumento d)
    {
        var e = d.Detalle.Embarque;
        var empaque = d.Detalle.Empaque;
        var filas = empaque
            .Select(x => Fila(Entero(x.NumeroCaja), x.ProductoCodigo, x.NombreCientifico, x.NombreFactura ?? ReglasEmbarque.PrimerNombre(x.NombreComun) ?? "",
                x.TipoBolsaNombre ?? "", Entero(x.Bolsas), Entero(x.PecesPorBolsa), Entero(x.Cantidad)))
            .ToList();
        return Documento(d, "PACKING LIST", NumeroOBorrador(e),
        [
            Encabezado(d, "Packing list N°", NumeroOBorrador(e), e.FechaDespacho),
            new SeccionTabla("", ["BOX #", "CODE", "SCIENTIFIC NAME", "COMMON NAME", "BAG", "BAGS", "QTY/BAG", "TOTAL"], filas, [3, 4, 12, 12, 4, 3, 4, 4])
            {
                Alineaciones = [C, I, I, I, C, D, D, D],
                Totales = [Fila($"{empaque.Select(x => x.NumeroCaja).Distinct().Count()} boxes", "", "", "", "", Entero(empaque.Sum(x => x.Bolsas)), "", Entero(empaque.Sum(x => x.Cantidad)))],
                Compacta = true,
            },
        ]);
    }

    /// <summary>Factura comercial en el formato del cliente: JRCH (con el empaque por fila) o V2 (con la fracción de caja).</summary>
    private static InformeModelo Factura(DatosDocumento d)
    {
        var e = d.Detalle.Embarque;
        var f = d.Detalle.Calculos.Factura;
        var jrch = e.FormatoFactura == FormatosFactura.Jrch;
        var empaque = d.Detalle.Empaque;
        string Nombre(EmbarqueEmpaqueFila x) => x.NombreFactura ?? ReglasEmbarque.PrimerNombre(x.NombreComun) ?? "";

        var filas = empaque.Select(x => jrch
                ? Fila(Entero(x.NumeroCaja), x.NombreCientifico, x.EmpaqueTexto, Nombre(x), Entero(x.Cantidad), Usd(x.Precio), Usd(x.Total))
                : Fila(Entero(x.NumeroCaja), Num(x.FraccionCaja), Entero(x.Cantidad), x.NombreCientifico, Nombre(x), Usd(x.Precio), Usd(x.Total)))
            .ToList();
        var encabezados = jrch
            ? new[] { "BOX #", "SCIENTIFIC NAME", "PACKING", "COMMON NAME", "QTY", "PRICE", "TOTAL" }
            : ["BOX #", "BOX SUM", "QTY", "SCIENTIFIC NAME", "COMMON NAME", "PRICE", "TOTAL"];
        IReadOnlyList<Alineacion> alineaciones = jrch ? [C, I, C, I, D, D, D] : [C, D, D, I, I, D, D];
        IReadOnlyList<int> pesos = jrch ? [3, 12, 6, 12, 4, 4, 5] : [3, 3, 4, 12, 12, 4, 5];

        // Filas de totales con las columnas de la tabla: descripción en la columna de nombre, cantidad, precio y total.
        IReadOnlyList<string> Total(string descripcion, string cantidad, string precio, string total) => jrch
            ? Fila("", descripcion, "", "", cantidad, precio, total)
            : Fila("", "", cantidad, descripcion, "", precio, total);

        var totales = new List<IReadOnlyList<string>> { Total("Subtotal", Entero(f.Unidades), "", Usd(f.SubtotalPeces)) };
        if (f.Descuento > 0)
        {
            totales.Add(Total("Fish discount", "", Pct(e.DescuentoPeces), Usd(-f.Descuento)));
        }

        totales.AddRange(f.Cargos.Select(c => Total(c.Descripcion, Num(c.Cantidad), Usd(c.Precio), Usd(c.Total))));
        totales.Add(Total($"AWB {e.Awb ?? ""}".Trim(), "1", Usd(f.Awb), Usd(f.Awb)));
        totales.Add(Total("TOTAL", "", "", Usd(f.Total)));

        var titulo = e.FacturaNumero is { } n ? $"INVOICE No. {n}" : "INVOICE (DRAFT)";
        return Documento(d, titulo, e.FacturaNumero is null ? "Draft — not a valid invoice until issued" : null,
        [
            Encabezado(d, "Invoice N°", e.FacturaNumero?.ToString() ?? "Draft", e.FacturaFecha ?? e.FechaDespacho),
            new SeccionTabla("", encabezados, filas, pesos) { Alineaciones = alineaciones, Totales = totales, Compacta = true },
            Firma(d.Empresa),
        ]);
    }

    private static InformeModelo NotaCredito(DatosDocumento d)
    {
        var e = d.Detalle.Embarque;
        var n = d.Detalle.Calculos.NotaCredito;
        var numero = e.FacturaNumero is { } f ? $"{f}-01" : e.Codigo;
        var filas = n.Lineas.Select(l => Fila(l.ProductoCodigo, l.NombreCientifico, l.NombreComun ?? "", Entero(l.Muertos), Usd(l.Precio), Usd(l.Total))).ToList();
        var totales = new List<IReadOnlyList<string>> { Fila("", "Subtotal DOA fish", "", Entero(n.Muertos), "", Usd(n.Subtotal)) };
        if (n.Descuento > 0)
        {
            totales.Add(Fila("", "Discount from DOA fish", "", "", Pct(e.DescuentoPeces), Usd(-n.Descuento)));
        }

        totales.Add(Fila("", "TOTAL CREDIT NOTE", "", "", "", Usd(n.Total)));
        return Documento(d, "CREDIT NOTE", $"N° {numero}",
        [
            Encabezado(d, "Credit note N°", numero, e.NotaCreditoFecha),
            new SeccionTabla("", ["COD", "SCIENTIFIC NAME", "COMMON NAME", "DOA", "PRICE", "TOTAL"], filas, [3, 10, 10, 3, 3, 4])
            {
                Alineaciones = [I, I, I, D, D, D],
                Totales = totales,
                Compacta = true,
            },
            Firma(d.Empresa),
        ]);
    }

    private static InformeModelo Balance(DatosDocumento d)
    {
        var e = d.Detalle.Embarque;
        var b = d.Detalle.Calculos.Balance;
        var filas = new List<IReadOnlyList<string>> { Fila($"Invoice N° {e.FacturaNumero?.ToString() ?? "(draft)"}", Usd(b.Factura)) };
        if (b.NotaCredito > 0)
        {
            filas.Add(Fila($"Credit note N° {e.FacturaNumero}-01", Usd(-b.NotaCredito)));
        }

        filas.Add(Fila("Subtotal", Usd(b.Factura - b.NotaCredito)));
        filas.Add(Fila("Advance payment", Usd(-b.Anticipo)));
        var aFavor = b.Saldo >= 0 ? $"BALANCE IN FAVOR OF {d.Empresa.RazonSocial}" : $"BALANCE IN FAVOR OF {e.ClienteNombre}";
        return Documento(d, "BALANCE", $"Invoice {e.FacturaNumero?.ToString() ?? e.Codigo}",
        [
            Encabezado(d, "Invoice N°", e.FacturaNumero?.ToString() ?? "Draft", e.FacturaFecha),
            new SeccionTabla("", ["Concept", "Value"], filas, [4, 1]) { Alineaciones = [I, D], Totales = [Fila(aFavor, Usd(Math.Abs(b.Saldo)))] },
            DatosBancarios(d.Empresa),
            Firma(d.Empresa),
        ]);
    }

    // --- Documentos para autoridades: siempre desde lo real, con el código y el nombre de la Resolución AUNAP ---

    /// <summary>
    /// Proforma para solicitar el permiso a la AUNAP: las especies del pedido agrupadas por especie AUNAP, con sus
    /// cantidades y precios reales (los mismos de la preinvoice).
    /// </summary>
    private static InformeModelo SolicitudAunap(DatosDocumento d)
    {
        var e = d.Detalle.Embarque;
        var p = d.Detalle.Calculos.Preinvoice;
        var lineas = Declaradas(d.Detalle.Lineas.Select(l => (l.ProductoId, l.ProductoCodigo, l.NombreCientifico, l.Cantidad, l.Total)), d.Aunap);
        var filas = lineas.Select(l => Fila(l.Codigo, l.Familia, l.NombreCientifico, l.NombreComun, Entero(l.Cantidad), Usd4(l.PrecioPromedio), Usd(l.Total))).ToList();
        var totales = new List<IReadOnlyList<string>> { Fila("", "", "Subtotal", "", Entero(lineas.Sum(l => l.Cantidad)), "", Usd(lineas.Sum(l => l.Total))) };
        if (p.Descuento > 0)
        {
            totales.Add(Fila("", "", "Descuento", "", "", Pct(e.DescuentoPeces), Usd(-p.Descuento)));
        }

        totales.Add(Fila("", "", "TOTAL PECES", "", "", "", Usd(p.SubtotalPeces - p.Descuento)));
        var numero = e.PreinvoiceNumero ?? e.Codigo;
        return Documento(d, "PROFORMA · SOLICITUD DE PERMISO AUNAP", $"N° {numero}",
        [
            new SeccionBloques("",
            [
                new BloqueInforme("DESTINATARIO",
                [
                    new("Cliente", e.ClienteNombre),
                    new("Dirección", d.ClienteExportacion?.Direccion),
                    new("Ciudad / país", d.Cliente is null ? null : string.Join(" - ", new[] { d.Cliente.Ciudad, d.Cliente.Pais }.Where(v => !string.IsNullOrWhiteSpace(v)))),
                ]),
                new BloqueInforme("ENVÍO",
                [
                    new("Proforma N°", numero),
                    new("Fecha", (e.PreinvoiceFecha ?? DateOnly.FromDateTime(d.AhoraUtc)).ToString("dd/MM/yyyy")),
                    new("Destino", e.Destino),
                    new("Aerolínea", e.Aerolinea),
                    new("Despacho previsto", e.FechaDespacho?.ToString("dd/MM/yyyy")),
                ]),
            ]),
            new SeccionTabla("", ["CÓDIGO AUNAP", "FAMILIA", "NOMBRE CIENTÍFICO", "NOMBRE COMÚN", "CANTIDAD", "V. UNIT. USD", "VALOR USD"], filas, [4, 7, 11, 9, 5, 5, 5])
            {
                Alineaciones = [C, I, I, I, D, D, D],
                Totales = totales,
                Compacta = true,
            },
            new SeccionTexto("", $"Especies según la Resolución AUNAP 1924 de 2015. Cantidades y valores del pedido {e.Codigo} (preinvoice {numero})."),
            Firma(d.Empresa),
        ]);
    }

    /// <summary>Packing list para aduana y aeropuerto: el empaque real caja por caja, con el código y nombre AUNAP.</summary>
    private static InformeModelo PackingAduana(DatosDocumento d)
    {
        var e = d.Detalle.Embarque;
        var empaque = d.Detalle.Empaque;
        _ = Declaradas(empaque.Select(x => (x.ProductoId, x.ProductoCodigo, x.NombreCientifico, x.Cantidad, x.Total)), d.Aunap);
        var filas = empaque
            .Select(x =>
            {
                var a = d.Aunap[x.ProductoId];
                return Fila(Entero(x.NumeroCaja), a.AunapCodigo!, a.AunapFamilia ?? "", a.AunapNombreCientifico!, a.AunapNombreComun ?? "", Entero(x.Cantidad));
            })
            .ToList();
        var cajas = empaque.Select(x => x.NumeroCaja).Distinct().Count();
        return Documento(d, "PACKING LIST", "Customs & airport",
        [
            Encabezado(d, "Packing list N°", NumeroOBorrador(e), e.FechaDespacho),
            new SeccionTabla("", ["BOX #", "AUNAP CODE", "FAMILY", "SCIENTIFIC NAME", "COMMON NAME", "TOTAL"], filas, [3, 4, 8, 12, 10, 4])
            {
                Alineaciones = [C, C, I, I, I, D],
                Totales = [Fila($"{cajas} boxes", "", "", "Total", "", Entero(empaque.Sum(x => x.Cantidad)))],
                Compacta = true,
            },
            Firma(d.Empresa),
        ]);
    }

    /// <summary>Custom invoice: la factura real agrupada por especie AUNAP. Mismos totales que la factura comercial.</summary>
    private static InformeModelo FacturaAduana(DatosDocumento d)
    {
        var e = d.Detalle.Embarque;
        var f = d.Detalle.Calculos.Factura;
        var lineas = Declaradas(d.Detalle.Empaque.Select(x => (x.ProductoId, x.ProductoCodigo, x.NombreCientifico, x.Cantidad, x.Total)), d.Aunap);
        var filas = lineas.Select(l => Fila(l.Codigo, l.Familia, l.NombreCientifico, l.NombreComun, Entero(l.Cantidad), Usd4(l.PrecioPromedio), Usd(l.Total))).ToList();
        IReadOnlyList<string> Total(string descripcion, string cantidad, string precio, string total) => Fila("", "", descripcion, "", cantidad, precio, total);
        var totales = new List<IReadOnlyList<string>> { Total("Subtotal", Entero(f.Unidades), "", Usd(f.SubtotalPeces)) };
        if (f.Descuento > 0)
        {
            totales.Add(Total("Fish discount", "", Pct(e.DescuentoPeces), Usd(-f.Descuento)));
        }

        totales.AddRange(f.Cargos.Select(c => Total(c.Descripcion, Num(c.Cantidad), Usd(c.Precio), Usd(c.Total))));
        totales.Add(Total($"AWB {e.Awb ?? ""}".Trim(), "1", Usd(f.Awb), Usd(f.Awb)));
        totales.Add(Total("TOTAL", "", "", Usd(f.Total)));
        var numero = e.FacturaNumero?.ToString() ?? "Draft";
        return Documento(d, e.FacturaNumero is { } n ? $"CUSTOMS INVOICE No. {n}" : "CUSTOMS INVOICE (DRAFT)", "Same quantities and values as the commercial invoice",
        [
            Encabezado(d, "Invoice N°", numero, e.FacturaFecha ?? e.FechaDespacho),
            new SeccionTabla("", ["AUNAP CODE", "FAMILY", "SCIENTIFIC NAME", "COMMON NAME", "QTY", "UNIT PRICE", "TOTAL"], filas, [4, 7, 11, 10, 4, 5, 5])
            {
                Alineaciones = [C, I, I, I, D, D, D],
                Totales = totales,
                Compacta = true,
            },
            Firma(d.Empresa),
        ]);
    }

    private static IReadOnlyList<LineaDeclarada> Declaradas(
        IEnumerable<(Guid ProductoId, string ProductoCodigo, string NombreCientifico, int Cantidad, decimal Total)> filas,
        IReadOnlyDictionary<Guid, ProductoAunapFila> mapa)
    {
        var (lineas, error) = ReglasAunap.Agrupar(filas, mapa);
        return error is null ? lineas : throw new InvalidOperationException(error);
    }


    // --- Piezas comunes ---

    /// <summary>Documento al cliente: logo y datos de la empresa arriba, pie con el nombre (sin "generado por").</summary>
    private static InformeModelo Documento(DatosDocumento d, string titulo, string? subtitulo, IReadOnlyList<SeccionInforme> secciones) =>
        new(titulo, subtitulo, d.Empresa.RazonSocial, d.GeneradoPor, d.AhoraUtc, secciones)
        {
            Logo = d.Logo,
            EmpresaDatos =
            [
                .. new[]
                {
                    d.Empresa.Nit is { } nit ? $"NIT: {nit}" : null,
                    d.Empresa.Direccion,
                    d.Empresa.CiudadPais,
                    d.Empresa.Telefono is { } tel ? $"Tel: {tel}" : null,
                    d.Empresa.Email is { } mail ? $"Email: {mail}" : null,
                    d.Empresa.SitioWeb,
                }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!),
            ],
            Pie = string.Join(" · ", new[] { d.Empresa.RazonSocial, d.Empresa.SitioWeb }.Where(x => !string.IsNullOrWhiteSpace(x))),
        };

    /// <summary>Cliente a la izquierda; número, fecha, aerolínea, AWB y destino a la derecha.</summary>
    private static SeccionBloques Encabezado(DatosDocumento d, string etiquetaNumero, string numero, DateOnly? fecha)
    {
        var e = d.Detalle.Embarque;
        var c = d.Cliente;
        var x = d.ClienteExportacion;
        var cliente = new List<CampoInforme>
        {
            new("Client", e.ClienteNombre),
            new("Address", x?.Direccion),
            new("City", c is null ? null : string.Join(" - ", new[] { c.Ciudad?.ToUpperInvariant(), c.Pais.ToUpperInvariant() }.Where(v => !string.IsNullOrWhiteSpace(v)))),
            new("Phone", c?.TelefonoWhatsapp),
            new("Email", c?.Email),
        };
        if (!string.IsNullOrWhiteSpace(x?.Eori))
        {
            cliente.Add(new("EORI", x.Eori));
        }

        var documento = new List<CampoInforme>
        {
            new(etiquetaNumero, numero),
            new("Date", (fecha ?? DateOnly.FromDateTime(d.AhoraUtc)).ToString("MMMM d, yyyy", Ingles)),
            new("Airline", e.Aerolinea),
            new("AWB", e.Awb),
        };
        if (!string.IsNullOrWhiteSpace(e.Destino))
        {
            documento.Add(new("Destination", e.Destino));
        }

        return new SeccionBloques("", [new BloqueInforme("CUSTOMER", cliente), new BloqueInforme("DOCUMENT", documento)]);
    }

    private static SeccionTexto DatosBancarios(DatosEmpresaInfo e) =>
        new("Bank data", string.Join(Environment.NewLine, new[]
        {
            e.Banco is { } banco ? $"BANK: {banco}" : null,
            e.Swift is { } swift ? $"SWIFT OR BIC CODE: {swift} (if they ask for 11 digits add XXX: {swift}XXX)" : null,
            e.DireccionBanco is { } dir ? $"OFFICE ADDRESS: {dir}" : null,
            e.Beneficiario is { } ben ? $"BENEFICIARY: {ben}" : null,
            e.Nit is { } nit ? $"NIT: {nit}" : null,
            e.CuentaBancaria is { } cta ? $"CURRENT ACCOUNT N°: {cta}" : null,
        }.Where(x => x is not null).DefaultIfEmpty("Bank data not configured yet.")));

    private static SeccionTexto Firma(DatosEmpresaInfo e) =>
        new("", string.Join(Environment.NewLine, new[]
        {
            "Sincerely,",
            "",
            e.FirmanteNombre ?? e.RazonSocial,
            e.FirmanteCargo,
            e.FirmanteTelefono is { } tel ? $"Cellphone & WhatsApp: {tel}" : null,
            e.Email is { } mail ? $"Email: {mail}" : null,
            e.SitioWeb is { } web ? $"Website: {web}" : null,
        }.Where(x => x is not null)));

    private static string NumeroOBorrador(EmbarqueFila e) => e.FacturaNumero?.ToString() ?? e.Codigo;

    private static IReadOnlyList<string> Fila(params string[] celdas) => celdas;

    private static string Usd(decimal v) => v.ToString("#,##0.00", Ingles);

    private static string Usd4(decimal v) => v.ToString("#,##0.00##", Ingles);

    private static string Entero(int v) => v.ToString("#,##0", Ingles);

    private static string Num(decimal v) => v.ToString("#,##0.##", Ingles);

    private static string Pct(decimal v) => $"{v.ToString("0.##", Ingles)} %";
}
