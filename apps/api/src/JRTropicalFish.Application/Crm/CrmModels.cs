namespace JRTropicalFish.Application.Crm;

// Los records *Fila son lo que devuelven las funciones fn_* de la migración FuncionesCrm. Dapper los
// construye por posición: el orden de sus campos es el orden de las columnas del RETURNS TABLE.

/// <summary>
/// Quién pide y qué puede ver. Un comercial (<c>crm:vender</c>) sin <c>crm:ver-todo</c> solo ve y
/// gestiona lo suyo; cualquier otro usuario con acceso al CRM (logística, gerencia, consulta) ve todo.
/// </summary>
public sealed record AlcanceCrm(Guid UsuarioId, bool EsComercial, bool VerTodo)
{
    /// <summary>Comercial por el que hay que filtrar las listas, o <c>null</c> si ve todo.</summary>
    public Guid? FiltroComercial => EsComercial && !VerTodo ? UsuarioId : null;

    public bool PuedeVer(Guid comercialId) => FiltroComercial is null || FiltroComercial == comercialId;
}

public sealed record Creado(Guid Id, string Codigo);

public sealed record ComercialInfo(Guid Id, string Nombre, string Email);

// --- Clientes ---

public sealed record ClienteFila(
    Guid Id,
    string Codigo,
    string NombreEmpresa,
    string? ContactoPrincipal,
    string? Email,
    string? TelefonoWhatsapp,
    string Pais,
    string? Ciudad,
    string? AeropuertoIata,
    string TipoCliente,
    string LicenciaImportacion,
    string Estado,
    string? Notas,
    DateTime CreadoEn);

/// <param name="Codigo">Solo al crear y opcional: lo usa la importación para conservar <c>CLI-047</c>.</param>
public sealed record GuardarClienteRequest(
    string? Codigo,
    string? NombreEmpresa,
    string? ContactoPrincipal,
    string? Email,
    string? TelefonoWhatsapp,
    string? Pais,
    string? Ciudad,
    string? AeropuertoIata,
    string? TipoCliente,
    string? LicenciaImportacion,
    string? Estado,
    string? Notas);

public sealed record ClienteAuditable(
    string Codigo,
    string NombreEmpresa,
    string? ContactoPrincipal,
    string? Email,
    string? TelefonoWhatsapp,
    string Pais,
    string? Ciudad,
    string? AeropuertoIata,
    string TipoCliente,
    string LicenciaImportacion,
    string Estado,
    string? Notas);

/// <summary>Todo lo vinculado a un cliente, para su ficha (filtrado por el alcance de quien pide).</summary>
public sealed record ClienteRelacionados(
    IReadOnlyList<OportunidadFila> Oportunidades,
    IReadOnlyList<VentaFila> Ventas,
    IReadOnlyList<TrackingFila> Vuelos,
    IReadOnlyList<TareaFila> Tareas);

// --- Oportunidades ---

public sealed record OportunidadFila(
    Guid Id,
    string Codigo,
    Guid ClienteId,
    string ClienteCodigo,
    string ClienteNombre,
    string TipoVenta,
    Guid ComercialId,
    string ComercialNombre,
    DateOnly FechaContactoInicial,
    string Etapa,
    string Moneda,
    decimal MontoEstimado,
    int CajasEstimadas,
    DateOnly? FechaPagoConfirmado,
    string? MotivoPerdida,
    string? NotasSeguimiento,
    DateTime EtapaCambiadaEn,
    Guid? VentaId,
    DateTime CreadoEn);

/// <param name="ComercialId">Solo lo elige quien tiene <c>crm:ver-todo</c>; a un comercial se le asigna él mismo.</param>
/// <param name="Etapa">Solo al crear: una etapa activa (una oportunidad puede registrarse ya avanzada).</param>
public sealed record GuardarOportunidadRequest(
    Guid ClienteId,
    string? TipoVenta,
    Guid? ComercialId,
    DateOnly? FechaContactoInicial,
    string? Etapa,
    string? Moneda,
    decimal MontoEstimado,
    int CajasEstimadas,
    string? NotasSeguimiento);

/// <param name="EtapaActual">La que vio el usuario: si cambió mientras tanto, la API responde 409.</param>
public sealed record CambiarEtapaRequest(string? EtapaActual, string? Etapa, string? MotivoPerdida);

public sealed record GanarOportunidadRequest(
    string? EtapaActual,
    DateOnly? FechaPagoConfirmado,
    string? OrigenCanal,
    decimal? Trm,
    string? EstadoPago);

public sealed record OportunidadGanada(Guid VentaId, string VentaCodigo, Guid? TrackingId, string? TrackingCodigo);

public sealed record OportunidadAuditable(
    string Cliente,
    string TipoVenta,
    string Comercial,
    DateOnly FechaContactoInicial,
    string Etapa,
    string Moneda,
    decimal MontoEstimado,
    int CajasEstimadas,
    DateOnly? FechaPagoConfirmado,
    string? MotivoPerdida,
    string? NotasSeguimiento);

// --- Ventas ---

public sealed record VentaFila(
    Guid Id,
    string Codigo,
    Guid? OportunidadId,
    string? OportunidadCodigo,
    Guid ClienteId,
    string ClienteCodigo,
    string ClienteNombre,
    Guid ComercialId,
    string ComercialNombre,
    string OrigenCanal,
    DateOnly FechaPedido,
    string TipoVenta,
    string Moneda,
    decimal Total,
    decimal? Trm,
    decimal TotalCop,
    int TotalCajas,
    string? TransportadoraAerolinea,
    string? NoGuiaAwb,
    string EstadoPago,
    string EstadoPreparacion,
    string? CiudadEntrega,
    string? DireccionEntrega,
    string? Notas,
    Guid? TrackingId,
    DateTime CreadoEn);

/// <param name="ClienteId">Solo al crear (cliente, comercial y tipo no cambian después).</param>
public sealed record GuardarVentaRequest(
    Guid? ClienteId,
    Guid? ComercialId,
    string? TipoVenta,
    string? OrigenCanal,
    DateOnly? FechaPedido,
    string? Moneda,
    decimal Total,
    decimal? Trm,
    int TotalCajas,
    string? TransportadoraAerolinea,
    string? NoGuiaAwb,
    string? EstadoPago,
    string? EstadoPreparacion,
    string? CiudadEntrega,
    string? DireccionEntrega,
    string? Notas);

public sealed record VentaAuditable(
    string Cliente,
    string Comercial,
    string OrigenCanal,
    DateOnly FechaPedido,
    string TipoVenta,
    string Moneda,
    decimal Total,
    decimal? Trm,
    int TotalCajas,
    string? TransportadoraAerolinea,
    string? NoGuiaAwb,
    string EstadoPago,
    string EstadoPreparacion,
    string? CiudadEntrega,
    string? DireccionEntrega,
    string? Notas);

// --- Tracking de vuelos ---

public sealed record TrackingFila(
    Guid Id,
    string Codigo,
    Guid VentaId,
    string VentaCodigo,
    Guid? OportunidadId,
    string? OportunidadCodigo,
    Guid ClienteId,
    string ClienteCodigo,
    string ClienteNombre,
    Guid ComercialId,
    string ComercialNombre,
    string? NFacturaInvoice,
    string? NoAwb,
    string? Aerolinea,
    DateOnly? FechaDespachoVuelo,
    int CajasReales,
    string Fase,
    decimal MontoFobUsd,
    decimal FleteAereoUsd,
    decimal DoaReportadoUsd,
    decimal? PorcentajeDoa,
    string? NotaCreditoNo,
    DateTime CreadoEn);

public sealed record GuardarTrackingRequest(
    string? NFacturaInvoice,
    string? NoAwb,
    string? Aerolinea,
    DateOnly? FechaDespachoVuelo,
    int CajasReales,
    string? Fase,
    decimal MontoFobUsd,
    decimal FleteAereoUsd,
    decimal DoaReportadoUsd,
    string? NotaCreditoNo);

public sealed record CambiarFaseRequest(string? FaseActual, string? Fase);

public sealed record TrackingAuditable(
    string? NFacturaInvoice,
    string? NoAwb,
    string? Aerolinea,
    DateOnly? FechaDespachoVuelo,
    int CajasReales,
    string Fase,
    decimal MontoFobUsd,
    decimal FleteAereoUsd,
    decimal DoaReportadoUsd,
    string? NotaCreditoNo);

// --- Tareas ---

public sealed record TareaFila(
    Guid Id,
    string Codigo,
    Guid ClienteId,
    string ClienteCodigo,
    string ClienteNombre,
    Guid? OportunidadId,
    string? OportunidadCodigo,
    string Accion,
    string TipoAccion,
    string Estado,
    string Prioridad,
    Guid ComercialId,
    string ComercialNombre,
    DateOnly FechaAsignacion,
    DateOnly FechaVencimiento,
    int DiasRestantes,
    string? Notas,
    DateTime? CompletadaEn,
    DateTime CreadoEn)
{
    /// <summary>Semáforo rojo: venció y no está completada.</summary>
    public bool Vencida => DiasRestantes < 0 && Estado != Domain.Crm.EstadosTarea.Completada;
}

public sealed record GuardarTareaRequest(
    Guid ClienteId,
    Guid? OportunidadId,
    string? Accion,
    string? TipoAccion,
    string? Estado,
    string? Prioridad,
    Guid? ComercialId,
    DateOnly? FechaAsignacion,
    DateOnly? FechaVencimiento,
    string? Notas);

public sealed record CambiarEstadoTareaRequest(string? EstadoActual, string? Estado);

public sealed record TareaAuditable(
    string Cliente,
    string? Oportunidad,
    string Accion,
    string TipoAccion,
    string Estado,
    string Prioridad,
    string Comercial,
    DateOnly FechaAsignacion,
    DateOnly FechaVencimiento,
    string? Notas);

// --- Metas y dashboard ---

public sealed record ParametrosComercialesInfo(
    decimal MetaEmpresaCop,
    decimal MetaComercialCop,
    int PuntoEquilibrioDespachos,
    decimal PorcentajeComision,
    decimal UmbralDoa,
    int MetaClientesNuevos,
    DateTime? ActualizadoEn);

public sealed record GuardarParametrosRequest(
    decimal MetaEmpresaCop,
    decimal MetaComercialCop,
    int PuntoEquilibrioDespachos,
    decimal PorcentajeComision,
    decimal UmbralDoa,
    int MetaClientesNuevos);

public sealed record ParametrosAuditable(
    decimal MetaEmpresaCop,
    decimal MetaComercialCop,
    int PuntoEquilibrioDespachos,
    decimal PorcentajeComision,
    decimal UmbralDoa,
    int MetaClientesNuevos);

public sealed record MetaMesFila(Guid ComercialId, string ComercialNombre, Guid? MetaId, decimal? MetaCop);

public sealed record MetaItem(Guid ComercialId, decimal MetaCop);

/// <param name="Mes">Cualquier día del mes; se normaliza al día 1.</param>
public sealed record GuardarMetasRequest(DateOnly Mes, IReadOnlyList<MetaItem>? Metas);

public sealed record VentasComercialFila(
    Guid ComercialId, string ComercialNombre, int Ventas, decimal VentasCop, decimal DoaCop, decimal FleteCop);

public sealed record EmbudoFila(string Etapa, int Cantidad, decimal MontoUsd, decimal MontoCop);

public sealed record ResumenPeriodoFila(int Despachos, int ClientesNuevos);

public sealed record AvanceComercial(
    Guid ComercialId,
    string Nombre,
    int Ventas,
    decimal VentasCop,
    decimal MetaCop,
    decimal PorcentajeMeta,
    decimal Comision);

/// <summary>Todo el dashboard ejecutivo de un mes, calculado por <c>MetricasService</c>.</summary>
public sealed record DashboardCrm(
    DateOnly Mes,
    ParametrosComercialesInfo Parametros,
    decimal VentasCop,
    decimal DoaCop,
    decimal FleteCop,
    decimal VentaNetaCop,
    decimal PorcentajeMetaEmpresa,
    decimal PoolComision,
    int Despachos,
    int ClientesNuevos,
    IReadOnlyList<AvanceComercial> Comerciales,
    IReadOnlyList<EmbudoFila> Embudo,
    IReadOnlyList<TrackingFila> AlertasDoa,
    IReadOnlyList<TareaFila> TareasVencidas);
