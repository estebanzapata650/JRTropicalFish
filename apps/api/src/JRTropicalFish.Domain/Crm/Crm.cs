using JRTropicalFish.Domain.Comun;

namespace JRTropicalFish.Domain.Crm;

/// <summary>
/// Directorio único de clientes (nacionales e internacionales). Es el <c>Cliente</c> que el resto de
/// módulos (Catálogos, Exportaciones) reutiliza y amplía.
/// </summary>
public class Cliente : EntidadBase
{
    /// <summary>Código legible (<c>CLI-001</c>). Lo asigna el consecutivo o viene de la importación.</summary>
    public required string Codigo { get; set; }

    public required string NombreEmpresa { get; set; }

    public string? ContactoPrincipal { get; set; }

    public string? Email { get; set; }

    /// <summary>Tal como lo escribe el usuario; el web arma <c>wa.me</c> con solo los dígitos.</summary>
    public string? TelefonoWhatsapp { get; set; }

    public required string Pais { get; set; }

    public string? Ciudad { get; set; }

    /// <summary>Código IATA del aeropuerto de destino (3 letras), ej. NRT, BKK, MIA.</summary>
    public string? AeropuertoIata { get; set; }

    public required string TipoCliente { get; set; }

    public required string LicenciaImportacion { get; set; }

    public required string Estado { get; set; }

    public string? Notas { get; set; }

    public DateTime? EliminadoEn { get; set; }
}

/// <summary>
/// Oportunidad de venta: mide la negociación hasta confirmar el pago. La etapa solo avanza (o pasa a
/// Perdida); al ganarse se crea la <see cref="Venta"/> y, si es exportación, el <see cref="TrackingVuelo"/>.
/// </summary>
public class Oportunidad : EntidadBase
{
    public required string Codigo { get; set; }

    public Guid ClienteId { get; set; }

    public required string TipoVenta { get; set; }

    /// <summary>Usuario con el permiso <c>crm:vender</c>.</summary>
    public Guid ComercialId { get; set; }

    public DateOnly FechaContactoInicial { get; set; }

    public required string Etapa { get; set; }

    public required string Moneda { get; set; }

    public decimal MontoEstimado { get; set; }

    public int CajasEstimadas { get; set; }

    public DateOnly? FechaPagoConfirmado { get; set; }

    /// <summary>Obligatorio solo si la etapa es Perdida (CHECK en la base).</summary>
    public string? MotivoPerdida { get; set; }

    public string? NotasSeguimiento { get; set; }

    /// <summary>Cuándo llegó a la etapa actual: el tablero muestra los días en la etapa.</summary>
    public DateTime EtapaCambiadaEn { get; set; }

    public DateTime? EliminadoEn { get; set; }
}

/// <summary>Orden firme, nacional o de exportación. Puede nacer de una oportunidad ganada o ser directa.</summary>
public class Venta : EntidadBase
{
    public required string Codigo { get; set; }

    public Guid? OportunidadId { get; set; }

    public Guid ClienteId { get; set; }

    public Guid ComercialId { get; set; }

    public required string OrigenCanal { get; set; }

    public DateOnly FechaPedido { get; set; }

    public required string TipoVenta { get; set; }

    public required string Moneda { get; set; }

    public decimal Total { get; set; }

    /// <summary>TRM del día de la venta; obligatoria si la moneda es USD. Fija el valor en COP.</summary>
    public decimal? Trm { get; set; }

    /// <summary>Columna generada: <c>total × coalesce(trm, 1)</c>. Base de las metas y las comisiones.</summary>
    public decimal TotalCop { get; set; }

    public int TotalCajas { get; set; }

    public string? TransportadoraAerolinea { get; set; }

    public string? NoGuiaAwb { get; set; }

    public required string EstadoPago { get; set; }

    public required string EstadoPreparacion { get; set; }

    public string? CiudadEntrega { get; set; }

    /// <summary>Solo ventas nacionales.</summary>
    public string? DireccionEntrega { get; set; }

    public string? Notas { get; set; }
}

/// <summary>Logística internacional posventa y control DOA de una venta de exportación (una por venta).</summary>
public class TrackingVuelo : EntidadBase
{
    public required string Codigo { get; set; }

    public Guid VentaId { get; set; }

    public Guid? OportunidadId { get; set; }

    public Guid ClienteId { get; set; }

    public string? NFacturaInvoice { get; set; }

    public string? NoAwb { get; set; }

    public string? Aerolinea { get; set; }

    public DateOnly? FechaDespachoVuelo { get; set; }

    public int CajasReales { get; set; }

    public required string Fase { get; set; }

    public decimal MontoFobUsd { get; set; }

    public decimal FleteAereoUsd { get; set; }

    public decimal DoaReportadoUsd { get; set; }

    /// <summary>Columna generada: <c>DOA / FOB × 100</c>, nula si el FOB es 0.</summary>
    public decimal? PorcentajeDoa { get; set; }

    public string? NotaCreditoNo { get; set; }
}

/// <summary>Micro-acción con fecha límite de un comercial, ligada a un cliente y opcionalmente a una oportunidad.</summary>
public class Tarea : EntidadBase
{
    public required string Codigo { get; set; }

    public Guid ClienteId { get; set; }

    public Guid? OportunidadId { get; set; }

    public required string Accion { get; set; }

    public required string TipoAccion { get; set; }

    public required string Estado { get; set; }

    public required string Prioridad { get; set; }

    public Guid ComercialId { get; set; }

    public DateOnly FechaAsignacion { get; set; }

    public DateOnly FechaVencimiento { get; set; }

    public string? Notas { get; set; }

    public DateTime? CompletadaEn { get; set; }

    public DateTime? EliminadoEn { get; set; }
}

/// <summary>Meta de ventas de un comercial en un mes (en COP).</summary>
public class MetaComercial : EntidadBase
{
    /// <summary>Primer día del mes.</summary>
    public DateOnly Mes { get; set; }

    public Guid ComercialId { get; set; }

    public decimal MetaCop { get; set; }
}

/// <summary>Parámetros comerciales de la empresa. Una sola fila (<c>Id = 1</c>).</summary>
public class ParametrosComerciales
{
    public int Id { get; set; } = 1;

    public decimal MetaEmpresaCop { get; set; }

    /// <summary>Meta que se propone a cada comercial al crear las metas de un mes.</summary>
    public decimal MetaComercialCop { get; set; }

    public int PuntoEquilibrioDespachos { get; set; }

    /// <summary>Porcentaje de la venta neta que forma el pool de comisiones.</summary>
    public decimal PorcentajeComision { get; set; }

    /// <summary>% DOA a partir del cual un despacho se marca en rojo.</summary>
    public decimal UmbralDoa { get; set; }

    public int MetaClientesNuevos { get; set; }

    public DateTime? ActualizadoEn { get; set; }
}

/// <summary>Último número usado por prefijo (<c>CLI</c>, <c>OPT</c>…). Se bloquea con FOR UPDATE al usarlo.</summary>
public class Consecutivo
{
    public required string Prefijo { get; set; }

    public int Ultimo { get; set; }
}
