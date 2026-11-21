namespace JRTropicalFish.Application.Exportaciones;

// Los *Fila / *Info se construyen por posición desde las funciones de la migración FuncionesExportaciones:
// el orden de los parámetros = el orden de columnas de su RETURNS TABLE.

// --- Configuración ---

public sealed record DatosEmpresaInfo(
    string RazonSocial,
    string? Nit,
    string? Direccion,
    string? CiudadPais,
    string? Telefono,
    string? Email,
    string? SitioWeb,
    string? Banco,
    string? Swift,
    string? CuentaBancaria,
    string? Beneficiario,
    string? DireccionBanco,
    string? FirmanteNombre,
    string? FirmanteCargo,
    string? FirmanteTelefono,
    string? LogoClave,
    DateTime? ActualizadoEn);

public sealed record GuardarDatosEmpresaRequest(
    string? RazonSocial,
    string? Nit,
    string? Direccion,
    string? CiudadPais,
    string? Telefono,
    string? Email,
    string? SitioWeb,
    string? Banco,
    string? Swift,
    string? CuentaBancaria,
    string? Beneficiario,
    string? DireccionBanco,
    string? FirmanteNombre,
    string? FirmanteCargo,
    string? FirmanteTelefono);

/// <summary>Tarifas de flete (USD). También es la copia que guarda cada embarque.</summary>
public sealed record TarifasFleteInfo(
    decimal TarifaKgJr,
    decimal TarifaKgCliente,
    decimal FuelKg,
    decimal JpFeeKg,
    decimal VscKg,
    decimal VscMinimo,
    decimal DbcPorcentaje,
    decimal DbcMinimo,
    decimal DfPorcentaje,
    decimal DfMinimo,
    decimal DueAgentJr,
    decimal DueAgentCliente,
    decimal DueCarrier);

public sealed record TipoCajaFila(
    Guid Id, string Nombre, string DescripcionFactura, decimal CostoCop, decimal PrecioUsd, decimal KgPorCaja, int Pisos, int Orden, bool Activo);

public sealed record TipoBolsaFila(Guid Id, string Nombre, string? Tamano, decimal Porcion, decimal CostoCop, int Orden, bool Activo);

public sealed record CargoFila(string Codigo, string Nombre, decimal PrecioUsd, int Orden);

public sealed record ConfiguracionExportacion(
    DatosEmpresaInfo Empresa,
    TarifasFleteInfo Tarifas,
    IReadOnlyList<TipoCajaFila> Cajas,
    IReadOnlyList<TipoBolsaFila> Bolsas,
    IReadOnlyList<CargoFila> Cargos);

/// <param name="Id">Nulo para crear uno nuevo.</param>
public sealed record GuardarTipoCaja(
    Guid? Id, string? Nombre, string? DescripcionFactura, decimal CostoCop, decimal PrecioUsd, decimal KgPorCaja, int Pisos, bool Activo);

public sealed record GuardarTipoBolsa(Guid? Id, string? Nombre, string? Tamano, decimal Porcion, decimal CostoCop, bool Activo);

public sealed record GuardarCargo(string? Codigo, string? Nombre, decimal PrecioUsd);

public sealed record ClienteExportacionInfo(
    Guid ClienteId, string? Direccion, string? Eori, string FormatoFactura, Guid? ListaPreciosId, decimal DescuentoPeces);

public sealed record GuardarClienteExportacionRequest(
    string? Direccion, string? Eori, string? FormatoFactura, Guid? ListaPreciosId, decimal DescuentoPeces);

// --- Embarque ---

public sealed record CajaCantidad(Guid TipoCajaId, int Cantidad);

/// <summary>Cargo del embarque (copia editable del catálogo): cantidad y precio propios.</summary>
public sealed record CargoEmbarque(string Codigo, string Nombre, int Cantidad, decimal Precio);

public sealed record EmbarqueFila(
    Guid Id,
    string Codigo,
    Guid VentaId,
    string VentaCodigo,
    Guid? PedidoClienteId,
    string? PedidoCodigo,
    Guid ClienteId,
    string ClienteNombre,
    Guid ComercialId,
    string ComercialNombre,
    string Estado,
    Guid? ListaPreciosId,
    string? ListaNombre,
    decimal DescuentoPeces,
    string? Aerolinea,
    string? Ruta,
    string? Awb,
    string? Destino,
    DateTime? SalidaVuelo,
    DateTime? LlegadaVuelo,
    DateOnly? FechaDespacho,
    string? PreinvoiceNumero,
    DateOnly? PreinvoiceFecha,
    string FormatoFactura,
    int? FacturaNumero,
    DateOnly? FacturaFecha,
    DateOnly? NotaCreditoFecha,
    decimal? AwbValor,
    decimal AnticipoRecibido,
    string Tarifas,
    string Cargos,
    string CajasEstimadas,
    string CajasReales,
    string? Notas,
    int Lineas,
    int Unidades,
    int CajasEmpacadas,
    DateTime CreadoEn);

public sealed record EmbarqueLineaFila(
    Guid Id,
    int Orden,
    Guid ProductoId,
    string ProductoCodigo,
    string NombreCientifico,
    string? NombreComun,
    string? NombreEspanol,
    string? Talla,
    int Cantidad,
    decimal Precio,
    decimal FraccionCaja,
    string? Empaque,
    string? Nota)
{
    public decimal Total => Math.Round(Cantidad * Precio, 2);
}

public sealed record EmbarqueEmpaqueFila(
    Guid Id,
    int NumeroCaja,
    int Orden,
    Guid ProductoId,
    string ProductoCodigo,
    string NombreCientifico,
    string? NombreComun,
    string? NombreFactura,
    Guid? TipoBolsaId,
    string? TipoBolsaNombre,
    decimal? TipoBolsaPorcion,
    decimal? TipoBolsaCosto,
    int Bolsas,
    int PecesPorBolsa,
    decimal Precio,
    int Doa,
    int DoaAdicional,
    int DoaFinal,
    string? Nota)
{
    public int Cantidad => Bolsas * PecesPorBolsa;

    public decimal Total => Math.Round(Cantidad * Precio, 2);

    /// <summary>Muertos reportados por el cliente (DOA + ADD + FINAL de la factura JRCH).</summary>
    public int Muertos => Doa + DoaAdicional + DoaFinal;

    /// <summary>"2 (Expo) * 45", como en la lista de empaque del Excel.</summary>
    public string EmpaqueTexto => ReglasEmbarque.TextoEmpaque(Bolsas, TipoBolsaNombre, PecesPorBolsa);

    /// <summary>Fracción de caja que ocupa (box sum de la factura V2).</summary>
    public decimal FraccionCaja => (TipoBolsaPorcion ?? 0) * Bolsas;
}

public sealed record DocumentoEmbarqueFila(Guid Id, string Tipo, string Numero, string ArchivoClave, string? GeneradoPor, DateTime CreadoEn);

public sealed record EnvioDocumentoFila(
    Guid Id, string TipoDocumento, string Para, string Asunto, string Proveedor, bool Exitoso, bool LlegoAlDestinatario,
    string? Error, string? EnviadoPor, DateTime CreadoEn);

public sealed record EmbarqueDetalle(
    EmbarqueFila Embarque,
    TarifasFleteInfo Tarifas,
    IReadOnlyList<CargoEmbarque> Cargos,
    IReadOnlyList<CajaCantidad> CajasEstimadas,
    IReadOnlyList<CajaCantidad> CajasReales,
    IReadOnlyList<EmbarqueLineaFila> Lineas,
    IReadOnlyList<EmbarqueEmpaqueFila> Empaque,
    IReadOnlyList<DocumentoEmbarqueFila> Documentos,
    IReadOnlyList<EnvioDocumentoFila> Envios,
    CalculosEmbarque Calculos,
    string? ClienteEmail);

// --- Requests ---

public sealed record CrearEmbarqueRequest(Guid VentaId);

public sealed record GuardarEmbarqueRequest(
    Guid? ListaPreciosId,
    decimal DescuentoPeces,
    string? Aerolinea,
    string? Ruta,
    string? Awb,
    string? Destino,
    DateTime? SalidaVuelo,
    DateTime? LlegadaVuelo,
    DateOnly? FechaDespacho,
    string? FormatoFactura,
    decimal? AwbValor,
    decimal AnticipoRecibido,
    TarifasFleteInfo? Tarifas,
    IReadOnlyList<CargoEmbarque>? Cargos,
    IReadOnlyList<CajaCantidad>? CajasEstimadas,
    IReadOnlyList<CajaCantidad>? CajasReales,
    string? Notas);

public sealed record LineaEmbarqueRequest(Guid ProductoId, int Cantidad, decimal? Precio, decimal FraccionCaja, string? Empaque, string? Nota);

public sealed record GuardarLineasRequest(IReadOnlyList<LineaEmbarqueRequest>? Lineas);

public sealed record FilaEmpaqueRequest(
    int NumeroCaja, Guid ProductoId, string? NombreFactura, Guid? TipoBolsaId, int Bolsas, int PecesPorBolsa, decimal? Precio, string? Nota);

public sealed record GuardarEmpaqueRequest(IReadOnlyList<FilaEmpaqueRequest>? Filas);

public sealed record DoaFilaRequest(Guid Id, int Doa, int DoaAdicional, int DoaFinal);

public sealed record GuardarDoaRequest(IReadOnlyList<DoaFilaRequest>? Filas);

public sealed record CambiarEstadoEmbarqueRequest(string? EstadoActual, string? Estado);

public sealed record EnviarDocumentoRequest(string? Para, string? Mensaje);

// --- Cálculos (ReglasEmbarque) ---

public sealed record LineaCargo(string Descripcion, decimal Cantidad, decimal Precio)
{
    public decimal Total => Math.Round(Cantidad * Precio, 2);
}

public sealed record CalculoPreinvoice(
    int Unidades,
    decimal SubtotalPeces,
    decimal Descuento,
    IReadOnlyList<LineaCargo> Cargos,
    decimal AwbEstimado,
    decimal Total,
    decimal CajasSugeridas,
    decimal AnticipoPeces,
    decimal AnticipoAwb)
{
    public decimal Anticipo => AnticipoPeces + AnticipoAwb;
}

public sealed record PesoCajas(string Descripcion, int Cajas, decimal KgPorCaja)
{
    public decimal Kg => Cajas * KgPorCaja;
}

public sealed record CalculoFlete(
    IReadOnlyList<PesoCajas> Pesos,
    decimal Kg,
    decimal CargoTarifa,
    decimal CargoFuel,
    decimal CargoJp,
    decimal Vsc,
    decimal Dbc,
    decimal Df,
    decimal DueAgent,
    decimal DueCarrier,
    decimal Total)
{
    public decimal CargoPeso => CargoTarifa + CargoFuel + CargoJp;

    public decimal OtrosVariables => Vsc + Dbc + Df;
}

public sealed record FilaOrdenCargue(
    int Orden, string ProductoCodigo, string Especie, int Cantidad, string? Empaque, string? Nota, decimal FraccionCaja,
    decimal Acumulado, int? CajaInicio, int? CajaFin)
{
    /// <summary>"13 - 14", "7" o "—" (sin fracción de caja asignada).</summary>
    public string Cajas => CajaInicio is null ? "—" : CajaInicio == CajaFin ? $"{CajaInicio}" : $"{CajaInicio} - {CajaFin}";
}

public sealed record CalculoFactura(
    int Unidades,
    int Cajas,
    decimal SubtotalPeces,
    decimal Descuento,
    IReadOnlyList<LineaCargo> Cargos,
    decimal Awb,
    bool AwbEstimado,
    decimal Total,
    decimal CostoBolsasCop);

public sealed record LineaNotaCredito(string ProductoCodigo, string NombreCientifico, string? NombreComun, int Muertos, decimal Precio)
{
    public decimal Total => Math.Round(Muertos * Precio, 2);
}

public sealed record CalculoNotaCredito(IReadOnlyList<LineaNotaCredito> Lineas, int Muertos, decimal Subtotal, decimal Descuento, decimal Total);

public sealed record CalculoBalance(decimal Factura, decimal NotaCredito, decimal Anticipo)
{
    /// <summary>Saldo a favor de JR (positivo) o del cliente (negativo).</summary>
    public decimal Saldo => Factura - NotaCredito - Anticipo;
}

public sealed record CalculosEmbarque(
    CalculoPreinvoice Preinvoice,
    CalculoFlete Flete,
    IReadOnlyList<FilaOrdenCargue> OrdenCargue,
    CalculoFactura Factura,
    CalculoNotaCredito NotaCredito,
    CalculoBalance Balance);

// --- Auditoría ---

public sealed record EmbarqueAuditable(
    string Estado,
    string? Lista,
    decimal DescuentoPeces,
    string? Aerolinea,
    string? Ruta,
    string? Awb,
    string? Destino,
    DateTime? SalidaVuelo,
    DateTime? LlegadaVuelo,
    DateOnly? FechaDespacho,
    string FormatoFactura,
    decimal? AwbValor,
    decimal AnticipoRecibido,
    string Tarifas,
    string Cargos,
    string CajasEstimadas,
    string CajasReales,
    string? Notas);
