namespace JRTropicalFish.Domain.Exportaciones;

/// <summary>Estados del embarque de exportación (el despacho documental de una venta del CRM).</summary>
public static class EstadosEmbarque
{
    public const string Borrador = "borrador";

    /// <summary>Preinvoice enviada al cliente; espera confirmación y anticipo.</summary>
    public const string PreinvoiceEnviada = "preinvoice_enviada";

    public const string Confirmado = "confirmado";

    /// <summary>Bodega terminó el empaque real (cajas y bolsas).</summary>
    public const string Empacado = "empacado";

    /// <summary>Factura emitida: número asignado y PDF guardado (inmutable).</summary>
    public const string Facturado = "facturado";

    /// <summary>DOA liquidado (nota crédito) y balance cerrado.</summary>
    public const string Cerrado = "cerrado";

    public const string Cancelado = "cancelado";

    public static readonly IReadOnlyList<string> Todos = [Borrador, PreinvoiceEnviada, Confirmado, Empacado, Facturado, Cerrado, Cancelado];

    /// <summary>Antes de facturar se pueden editar líneas, empaque y cargos.</summary>
    public static readonly IReadOnlyList<string> Editables = [Borrador, PreinvoiceEnviada, Confirmado, Empacado];
}

/// <summary>Formato de factura comercial preferido por el cliente (hojas del Excel de formatos).</summary>
public static class FormatosFactura
{
    /// <summary>"Invoice JRCH Form": por caja, con el empaque ("2 (Expo) * 45") y columnas de DOA.</summary>
    public const string Jrch = "jrch";

    /// <summary>"Factura V2": por caja con la fracción de caja (box sum).</summary>
    public const string V2 = "v2";

    public static readonly IReadOnlyList<string> Todos = [Jrch, V2];
}

/// <summary>Documentos que el embarque genera en PDF.</summary>
public static class TiposDocumentoEmbarque
{
    public const string Preinvoice = "preinvoice";
    public const string Flete = "flete";
    public const string OrdenCargue = "orden_cargue";
    public const string Empaque = "empaque";
    public const string Factura = "factura";
    public const string NotaCredito = "nota_credito";
    public const string Balance = "balance";

    public static readonly IReadOnlyList<string> Todos = [Preinvoice, Flete, OrdenCargue, Empaque, Factura, NotaCredito, Balance];
}

/// <summary>Cargos fijos del despacho (hoja Parametros del Excel). Los códigos son estables.</summary>
public static class CodigosCargo
{
    public const string Zoosanitario = "zoosanitario";
    public const string Origen = "origen";
    public const string Cites = "cites";
    public const string AirportFee = "airport_fee";

    /// <summary>Uno por piso de caja; su cantidad se calcula de las cajas.</summary>
    public const string HeatPack = "heat_pack";

    public static readonly IReadOnlyList<string> Todos = [Zoosanitario, Origen, Cites, AirportFee, HeatPack];
}
