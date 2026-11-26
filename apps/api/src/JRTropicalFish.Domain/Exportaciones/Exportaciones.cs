using JRTropicalFish.Domain.Comun;

namespace JRTropicalFish.Domain.Exportaciones;

// Exportaciones: los documentos comerciales del despacho (libro «PM-COM-VTA. Formatos Comerciales»).
// Un embarque nace de una venta de exportación del CRM y la acompaña hasta el balance con el cliente.

/// <summary>Datos de la empresa que encabezan y firman los documentos. Una sola fila (<c>Id = 1</c>).</summary>
public class DatosEmpresa
{
    public int Id { get; set; } = 1;

    public required string RazonSocial { get; set; }

    public string? Nit { get; set; }

    public string? Direccion { get; set; }

    /// <summary>"Bogotá D.C., Colombia - South America".</summary>
    public string? CiudadPais { get; set; }

    public string? Telefono { get; set; }

    public string? Email { get; set; }

    public string? SitioWeb { get; set; }

    public string? Banco { get; set; }

    public string? Swift { get; set; }

    public string? CuentaBancaria { get; set; }

    public string? Beneficiario { get; set; }

    public string? DireccionBanco { get; set; }

    /// <summary>Quien firma los documentos comerciales ("Jorge Enrique Ramirez G").</summary>
    public string? FirmanteNombre { get; set; }

    public string? FirmanteCargo { get; set; }

    /// <summary>Celular y WhatsApp del firmante.</summary>
    public string? FirmanteTelefono { get; set; }

    /// <summary>Clave S3 del logo (PNG/JPG) que va en el encabezado de los PDF.</summary>
    public string? LogoClave { get; set; }

    public DateTime? ActualizadoEn { get; set; }
}

/// <summary>
/// Tarifas de flete aéreo por defecto (hoja Parametros, "Costeo flete"). Cada embarque copia estas tarifas
/// al crearse y puede ajustarlas con la cotización de la agencia. Una sola fila (<c>Id = 1</c>).
/// Valores en USD.
/// </summary>
public class TarifasFlete
{
    public int Id { get; set; } = 1;

    /// <summary>Lo que la agencia le cobra a JR por kg (referencia interna).</summary>
    public decimal TarifaKgJr { get; set; }

    /// <summary>Lo que se le cobra al cliente por kg.</summary>
    public decimal TarifaKgCliente { get; set; }

    public decimal FuelKg { get; set; }

    public decimal JpFeeKg { get; set; }

    public decimal VscKg { get; set; }

    public decimal VscMinimo { get; set; }

    /// <summary>DBC en % sobre el cargo por peso.</summary>
    public decimal DbcPorcentaje { get; set; }

    public decimal DbcMinimo { get; set; }

    /// <summary>DF en % sobre el due agent.</summary>
    public decimal DfPorcentaje { get; set; }

    public decimal DfMinimo { get; set; }

    public decimal DueAgentJr { get; set; }

    public decimal DueAgentCliente { get; set; }

    /// <summary>Vet fee, transfer fee, bee fee, HAWB, MCC, MYC… (due carrier).</summary>
    public decimal DueCarrier { get; set; }

    public DateTime? ActualizadoEn { get; set; }
}

/// <summary>Tipo de caja de empaque (holandesa = doble, europea = sencilla, mexicana, doble/triple piso).</summary>
public class TipoCaja : EntidadBase
{
    public required string Nombre { get; set; }

    /// <summary>Cómo sale en la factura: "CardBoard + Styrofoam Box Packing Double".</summary>
    public required string DescripcionFactura { get; set; }

    /// <summary>Costo de materiales (cartón, icopor, agua, jumbolón, bolsas) en COP, referencia interna.</summary>
    public decimal CostoCop { get; set; }

    /// <summary>Precio al cliente en USD.</summary>
    public decimal PrecioUsd { get; set; }

    /// <summary>Peso de la caja llena para estimar el flete.</summary>
    public decimal KgPorCaja { get; set; }

    /// <summary>Pisos de la caja: un heat pack por piso.</summary>
    public int Pisos { get; set; } = 1;

    public int Orden { get; set; }

    public bool Activo { get; set; } = true;
}

/// <summary>Tipo de bolsa (Export, 1/2, 1/4 Alta…): qué porción de caja ocupa y cuánto cuesta.</summary>
public class TipoBolsa : EntidadBase
{
    public required string Nombre { get; set; }

    /// <summary>Ancho de la bolsa, "11,5\"".</summary>
    public string? Tamano { get; set; }

    /// <summary>Fracción de caja que ocupa una bolsa (Export = 1, 1/2 = 0,5…).</summary>
    public decimal Porcion { get; set; }

    public decimal CostoCop { get; set; }

    public int Orden { get; set; }

    public bool Activo { get; set; } = true;
}

/// <summary>Cargo fijo del despacho (certificados, airport fee, heat pack) con su precio en USD.</summary>
public class CargoExportacion
{
    /// <summary>Código estable (<see cref="CodigosCargo"/>).</summary>
    public required string Codigo { get; set; }

    /// <summary>Cómo sale en la factura: "Zoosanitary certificate".</summary>
    public required string Nombre { get; set; }

    public decimal PrecioUsd { get; set; }

    public int Orden { get; set; }
}

/// <summary>Datos de exportación de un cliente del CRM (dirección para documentos, EORI, formato y lista).</summary>
public class ClienteExportacion
{
    public Guid ClienteId { get; set; }

    public string? Direccion { get; set; }

    /// <summary>Número EORI (clientes de la Unión Europea).</summary>
    public string? Eori { get; set; }

    public required string FormatoFactura { get; set; }

    /// <summary>Lista de precios con la que se arma su preinvoice (nula = STANDARD).</summary>
    public Guid? ListaPreciosId { get; set; }

    /// <summary>Descuento sobre los peces que se le da por defecto (%).</summary>
    public decimal DescuentoPeces { get; set; }

    public DateTime? ActualizadoEn { get; set; }
}

/// <summary>
/// Embarque de exportación: el despacho documental de una venta del CRM, de la preinvoice al balance.
/// Las tarifas, cargos y cajas se guardan como JSON (copias editables, no catálogos).
/// </summary>
public class Embarque : EntidadBase
{
    public required string Codigo { get; set; }

    public Guid VentaId { get; set; }

    public Guid? PedidoClienteId { get; set; }

    public Guid ClienteId { get; set; }

    public Guid ComercialId { get; set; }

    public required string Estado { get; set; }

    public Guid? ListaPreciosId { get; set; }

    /// <summary>Descuento sobre el valor de los peces (%).</summary>
    public decimal DescuentoPeces { get; set; }

    public string? Aerolinea { get; set; }

    /// <summary>"BOG - MIA - NRT".</summary>
    public string? Ruta { get; set; }

    public string? Awb { get; set; }

    /// <summary>Destino para el encabezado de los documentos ("Frankfurt, Germany. Way: direct").</summary>
    public string? Destino { get; set; }

    public DateTime? SalidaVuelo { get; set; }

    public DateTime? LlegadaVuelo { get; set; }

    public DateOnly? FechaDespacho { get; set; }

    /// <summary>Número de preinvoice ("2026-240": año y día del año, como en el Excel).</summary>
    public string? PreinvoiceNumero { get; set; }

    public DateOnly? PreinvoiceFecha { get; set; }

    public required string FormatoFactura { get; set; }

    /// <summary>Número de factura comercial; se asigna al emitirla (consecutivo propio).</summary>
    public int? FacturaNumero { get; set; }

    public DateOnly? FacturaFecha { get; set; }

    public DateOnly? NotaCreditoFecha { get; set; }

    /// <summary>Valor real de la guía aérea (AWB) que se factura; mientras no exista se usa el estimado.</summary>
    public decimal? AwbValor { get; set; }

    public decimal AnticipoRecibido { get; set; }

    /// <summary>JSON con <c>TarifasFlete</c> copiadas al crear.</summary>
    public required string Tarifas { get; set; }

    /// <summary>JSON: <c>[{ codigo, nombre, cantidad, precio }]</c>. El heat pack se calcula de las cajas.</summary>
    public required string Cargos { get; set; }

    /// <summary>JSON: <c>[{ tipoCajaId, cantidad }]</c> estimadas para la preinvoice.</summary>
    public required string CajasEstimadas { get; set; }

    /// <summary>JSON: <c>[{ tipoCajaId, cantidad }]</c> reales para la factura.</summary>
    public required string CajasReales { get; set; }

    public string? Notas { get; set; }
}

/// <summary>Especie pedida (preinvoice y orden de cargue).</summary>
public class EmbarqueLinea
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid EmbarqueId { get; set; }

    public int Orden { get; set; }

    public Guid ProductoId { get; set; }

    public int Cantidad { get; set; }

    /// <summary>Precio unitario USD (de la lista; editable cuando el cliente negocia).</summary>
    public decimal Precio { get; set; }

    /// <summary>Fracción de caja que ocupa (0,25 · 0,5 · 1 · 2…), para la orden de cargue.</summary>
    public decimal FraccionCaja { get; set; }

    /// <summary>Cómo se empaca para la orden de cargue: "2*140", "1/2*25".</summary>
    public string? Empaque { get; set; }

    public string? Nota { get; set; }
}

/// <summary>
/// Una fila del empaque real: qué va en cada caja (bolsas × peces por bolsa) y, después del vuelo,
/// los peces muertos reportados (DOA, ADD, FINAL, como las columnas de la factura JRCH).
/// </summary>
public class EmbarqueEmpaque
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid EmbarqueId { get; set; }

    public int NumeroCaja { get; set; }

    public int Orden { get; set; }

    public Guid ProductoId { get; set; }

    /// <summary>Nombre común si se factura distinto al catálogo ("C001 Incolicana Cory BIG").</summary>
    public string? NombreFactura { get; set; }

    public Guid? TipoBolsaId { get; set; }

    public int Bolsas { get; set; }

    public int PecesPorBolsa { get; set; }

    public decimal Precio { get; set; }

    public int Doa { get; set; }

    public int DoaAdicional { get; set; }

    public int DoaFinal { get; set; }

    public string? Nota { get; set; }
}

/// <summary>PDF emitido y guardado (la factura y la nota crédito emitidas no cambian).</summary>
public class DocumentoEmbarque : EntidadBase
{
    public Guid EmbarqueId { get; set; }

    public required string Tipo { get; set; }

    public required string Numero { get; set; }

    public required string ArchivoClave { get; set; }

    public string? GeneradoPor { get; set; }
}

/// <summary>Registro de cada documento enviado por correo al cliente.</summary>
public class EnvioDocumento : EntidadBase
{
    public Guid EmbarqueId { get; set; }

    public required string TipoDocumento { get; set; }

    public required string Para { get; set; }

    public required string Asunto { get; set; }

    public required string Proveedor { get; set; }

    public bool Exitoso { get; set; }

    public bool LlegoAlDestinatario { get; set; }

    public string? MensajeId { get; set; }

    public string? Error { get; set; }

    public string? EnviadoPor { get; set; }
}

/// <summary>
/// Especie autorizada para exportar como pez ornamental por la AUNAP (Resolución 1924 de 2015). El código es el
/// de la resolución (texto: hay códigos como "24*").
/// </summary>
public class EspecieAunap : EntidadBase
{
    public required string Codigo { get; set; }

    public required string NombreCientifico { get; set; }

    public string? NombreComun { get; set; }

    public bool Amazonia { get; set; }

    public bool Orinoquia { get; set; }

    public bool Magdalena { get; set; }

    public bool Pacifico { get; set; }

    public bool Caribe { get; set; }

    public string? Orden { get; set; }

    public string? Familia { get; set; }

    public bool Activo { get; set; } = true;
}

/// <summary>
/// Trámites de exportación del embarque. La proforma (cuando el cliente aprueba) es la base de la VUCE; la VUCE
/// (<see cref="Numero"/> = n.º de trámite TM, <see cref="Fecha"/> = aprobación) autoriza la exportación; el DEX,
/// la factura DIAN y el CITES se registran después. Alimentan el informe mensual AUNAP.
/// </summary>
public class PermisoAunap
{
    public Guid EmbarqueId { get; set; }

    /// <summary>N.º de trámite VUCE ("TM-00017927-20260918").</summary>
    public string? Numero { get; set; }

    /// <summary>Fecha de aprobación de la VUCE.</summary>
    public DateOnly? Fecha { get; set; }

    public DateOnly? VuceFechaSolicitud { get; set; }

    /// <summary>Consecutivo propio (<c>PRO-001</c>); se asigna al generar la proforma.</summary>
    public string? ProformaNumero { get; set; }

    public DateOnly? ProformaFecha { get; set; }

    /// <summary>N.º del documento de exportación DEX de la DIAN.</summary>
    public string? DexNumero { get; set; }

    /// <summary>N.º de la factura electrónica DIAN (Siigo), "JRE437".</summary>
    public string? FacturaDian { get; set; }

    public string? CitesNumero { get; set; }

    public DateOnly? CitesFecha { get; set; }

    public string? Notas { get; set; }

    public DateTime ActualizadoEn { get; set; }
}

/// <summary>Cantidad autorizada por especie AUNAP en el permiso del embarque.</summary>
public class PermisoAunapLinea
{
    public Guid EmbarqueId { get; set; }

    public Guid EspecieAunapId { get; set; }

    public int CantidadAutorizada { get; set; }
}

/// <summary>Municipio del formato de informe mensual AUNAP con la cuenca que le asigna (1.122 filas, hoja ESPECIES).</summary>
public class MunicipioAunap
{
    public int Id { get; set; }

    public required string Departamento { get; set; }

    public required string Municipio { get; set; }

    /// <summary>Amazonas, Orinoco, Magdalena Cauca, Pacifico o Caribe (como en el formato).</summary>
    public required string Cuenca { get; set; }
}

/// <summary>
/// Origen que se declara a la AUNAP para lo que llega de un centro de acopio. <see cref="Excluir"/> = no se reporta
/// (peces de criadero).
/// </summary>
public class CentroAcopioAunap
{
    public Guid CentroAcopioId { get; set; }

    public int? MunicipioAunapId { get; set; }

    public bool Excluir { get; set; }

    public DateTime ActualizadoEn { get; set; }
}

/// <summary>Encabezado del informe mensual AUNAP (datos del permisionario). Una sola fila (<c>Id = 1</c>).</summary>
public class ParametrosAunap
{
    public int Id { get; set; } = 1;

    public string? RepresentanteLegal { get; set; }

    /// <summary>"2124 DE 02 DE OCTUBRE DE 2025".</summary>
    public string? Resolucion { get; set; }

    public DateOnly? VigenciaDesde { get; set; }

    public DateOnly? VigenciaHasta { get; set; }

    public string? Direccion { get; set; }

    public string? Correo { get; set; }

    public string? Telefono { get; set; }

    public DateTime? ActualizadoEn { get; set; }
}

/// <summary>
/// Estándar de empaque de un producto (especie y talla): tipo de bolsa, peces por bolsa y litros de agua. Sin estándar,
/// se usan los peces por bolsa del catálogo. <see cref="Revisado"/> = lo validó el equipo.
/// </summary>
public class EstandarEmpaque
{
    public Guid ProductoId { get; set; }

    public Guid? TipoBolsaId { get; set; }

    public int? PecesMin { get; set; }

    public int? PecesMax { get; set; }

    /// <summary>Litros de agua por bolsa de esta especie; nulo = los del tipo de bolsa.</summary>
    public decimal? LitrosAgua { get; set; }

    public string? Notas { get; set; }

    public bool Revisado { get; set; }

    public DateTime ActualizadoEn { get; set; }
}

/// <summary>Aditivo del agua de empaque (Stress Guard, sal…): un medicamento, dosificado por litro de agua.</summary>
public class EstandarEmpaqueAditivo
{
    public Guid ProductoId { get; set; }

    public Guid MedicamentoId { get; set; }
}

/// <summary>Litros de agua de un tipo de bolsa (para dosificar aditivos).</summary>
public class EmpaqueBolsa
{
    public Guid TipoBolsaId { get; set; }

    public decimal? LitrosAgua { get; set; }

    public DateTime ActualizadoEn { get; set; }
}

/// <summary>Costo de un aditivo de empaque por unidad del medicamento (mL, g, mg…), en COP.</summary>
public class EmpaqueCostoAditivo
{
    public Guid MedicamentoId { get; set; }

    public decimal? CostoCopUnidad { get; set; }

    public DateTime ActualizadoEn { get; set; }
}

/// <summary>Costos de empaque que no son caja ni bolsa. Una sola fila (<c>Id = 1</c>).</summary>
public class ParametrosEmpaque
{
    public int Id { get; set; } = 1;

    /// <summary>Costo de un heat pack (uno por piso de caja).</summary>
    public decimal? CostoHeatPackCop { get; set; }

    /// <summary>Costo del oxígeno de una bolsa.</summary>
    public decimal? CostoOxigenoBolsaCop { get; set; }

    public DateTime? ActualizadoEn { get; set; }
}
