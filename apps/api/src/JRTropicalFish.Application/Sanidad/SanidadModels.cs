using System.Text.Json.Serialization;

namespace JRTropicalFish.Application.Sanidad;

// Filas leídas de las funciones fn_* de sanidad. Dapper construye por posición: el orden de cada record es el
// orden de columnas del RETURNS TABLE en la migración FuncionesSanidad.

public sealed record MedicamentoFila(
    Guid Id,
    string Codigo,
    string Nombre,
    string? PrincipioActivo,
    string? Presentacion,
    string Unidad,
    string TipoDosis,
    decimal DosisCantidad,
    decimal? DosisMaxima,
    decimal DosisBase,
    int? ExposicionMinutos,
    bool RequiereAyuno,
    int DiasRetiro,
    string? Indicaciones,
    string? Contraindicaciones,
    string? Fuente,
    bool Activo,
    [property: JsonIgnore] string? IncompatiblesTexto)
{
    /// <summary>Ids de los medicamentos que no se combinan con este (la función los devuelve separados por coma).</summary>
    public Guid[] Incompatibles { get; init; } =
        string.IsNullOrEmpty(IncompatiblesTexto) ? [] : [.. IncompatiblesTexto.Split(',').Select(Guid.Parse)];
}

public sealed record AlimentoFila(Guid Id, string Codigo, string Nombre, string Tipo, string Unidad, bool Activo);

public sealed record DietaFila(
    Guid Id,
    Guid? GrupoId,
    string? GrupoNombre,
    Guid? ProductoId,
    string? ProductoCodigo,
    string? ProductoNombre,
    Guid AlimentoId,
    string AlimentoNombre,
    string Unidad,
    int VecesDia,
    decimal CantidadPor100,
    string? Nota);

public sealed record FichaResumenFila(
    Guid ProductoId,
    string ProductoCodigo,
    string NombreCientifico,
    string? NombreComun,
    Guid GrupoId,
    string? GrupoNombre,
    bool TieneFicha,
    decimal? PhMin,
    decimal? PhMax,
    decimal? TemperaturaMin,
    decimal? TemperaturaMax,
    int AyunoLlegadaHoras,
    string? SensibilidadQuimica);

public sealed record FichaTecnicaInfo(
    Guid ProductoId,
    string? Procedencia,
    string? Caracteristicas,
    string? Dimorfismo,
    decimal? PhMin,
    decimal? PhMax,
    decimal? GhMin,
    decimal? GhMax,
    decimal? TemperaturaMin,
    decimal? TemperaturaMax,
    string? Etologia,
    string? Empaque,
    string? SensibilidadQuimica,
    int AyunoLlegadaHoras);

public sealed record ProtocoloFila(
    Guid Id,
    string Codigo,
    string Nombre,
    string? Descripcion,
    string Estado,
    Guid? ProductoId,
    string? ProductoCodigo,
    Guid? GrupoId,
    string? GrupoNombre,
    Guid? CentroAcopioId,
    string? CentroNombre,
    Guid? CuencaId,
    string? CuencaNombre,
    int? DiasCuarentena,
    int AyunoInicialHoras,
    string? AprobadoPor,
    DateTime? AprobadoEn,
    int Pasos);

public sealed record ProtocoloPasoFila(
    Guid Id,
    int Dia,
    int Orden,
    string Accion,
    Guid? MedicamentoId,
    string? MedicamentoNombre,
    int? PorcentajeAgua,
    string? Nota);

public sealed record ProtocoloDetalle(ProtocoloFila Protocolo, IReadOnlyList<ProtocoloPasoFila> Pasos);

public sealed record TratamientoFila(
    Guid Id,
    string Codigo,
    Guid AcuarioId,
    string AcuarioCodigo,
    Guid? ProtocoloId,
    string? ProtocoloNombre,
    string Motivo,
    string? Diagnostico,
    decimal? Litros,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    DateOnly FechaLiberacion,
    string Estado,
    [property: JsonIgnore] string? ReporteClave,
    string? Nota,
    int Aplicaciones,
    int Registradas,
    string? CreadoPor,
    DateTime CreadoEn)
{
    public bool TieneReporte => ReporteClave is not null;
}

public sealed record AplicacionFila(
    Guid Id,
    Guid TratamientoId,
    string TratamientoCodigo,
    Guid AcuarioId,
    string AcuarioCodigo,
    DateOnly Fecha,
    int Dia,
    int DiasTotal,
    int Orden,
    string Accion,
    Guid? MedicamentoId,
    string? MedicamentoNombre,
    int? PorcentajeAgua,
    decimal? CantidadCalculada,
    decimal? CantidadAplicada,
    string? Unidad,
    string Estado,
    string? Nota,
    string? UsuarioNombre,
    DateTime? RegistradoEn);

public sealed record TratamientoDetalle(TratamientoFila Tratamiento, IReadOnlyList<AplicacionFila> Aplicaciones);

public sealed record AlimentacionFila(Guid AcuarioId, int Tomas, DateTime Ultima, string? UltimoUsuario);

public sealed record ReservaFila(
    Guid Id,
    Guid LineaId,
    Guid ProductoId,
    string ProductoCodigo,
    Guid LoteId,
    string LoteCodigo,
    Guid AcuarioId,
    string AcuarioCodigo,
    int Cantidad,
    DateTime CreadoEn);

public sealed record SanidadAcuarioFila(Guid Id, string Codigo, string Sector, decimal? Litros, Guid? ResponsableId, string? ResponsableNombre);

public sealed record SanidadExistenciaFila(
    Guid? AcuarioId,
    Guid LoteId,
    string LoteCodigo,
    Guid ProductoId,
    string ProductoCodigo,
    string ProductoNombre,
    Guid GrupoId,
    Guid? CentroAcopioId,
    Guid? CuencaId,
    string Condicion,
    int Cantidad,
    DateTime Llegada,
    int AyunoLlegadaHoras);

public sealed record TratamientoVigenteFila(
    Guid Id,
    string Codigo,
    Guid AcuarioId,
    string Estado,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    DateOnly FechaLiberacion,
    string Nombre,
    bool RequiereAyuno,
    bool AlimentoMedicado,
    int PendientesHoy);

public sealed record SanidadReservaFila(
    Guid AcuarioId,
    Guid LoteId,
    int Cantidad,
    Guid PedidoId,
    string PedidoCodigo,
    string ClienteNombre,
    Guid? EmbarqueId,
    string? EmbarqueCodigo,
    DateTime? SalidaVuelo,
    DateOnly? FechaDespacho,
    string? Destino);

// --- Peticiones ---

public sealed record GuardarMedicamentoRequest(
    string? Codigo,
    string? Nombre,
    string? PrincipioActivo,
    string? Presentacion,
    string? Unidad,
    string? TipoDosis,
    decimal DosisCantidad,
    decimal? DosisMaxima,
    decimal DosisBase,
    int? ExposicionMinutos,
    bool RequiereAyuno,
    int DiasRetiro,
    string? Indicaciones,
    string? Contraindicaciones,
    string? Fuente,
    bool Activo,
    IReadOnlyList<Guid>? Incompatibles);

public sealed record GuardarAlimentoRequest(string? Codigo, string? Nombre, string? Tipo, string? Unidad, bool Activo);

public sealed record GuardarDietaRequest(Guid? GrupoId, Guid? ProductoId, Guid AlimentoId, int VecesDia, decimal CantidadPor100, string? Nota);

public sealed record GuardarFichaRequest(
    string? Procedencia,
    string? Caracteristicas,
    string? Dimorfismo,
    decimal? PhMin,
    decimal? PhMax,
    decimal? GhMin,
    decimal? GhMax,
    decimal? TemperaturaMin,
    decimal? TemperaturaMax,
    string? Etologia,
    string? Empaque,
    string? SensibilidadQuimica,
    int AyunoLlegadaHoras);

public sealed record PasoRequest(int Dia, string? Accion, Guid? MedicamentoId, int? PorcentajeAgua, string? Nota);

public sealed record GuardarProtocoloRequest(
    string? Nombre,
    string? Descripcion,
    Guid? ProductoId,
    Guid? GrupoId,
    Guid? CentroAcopioId,
    Guid? CuencaId,
    int? DiasCuarentena,
    int AyunoInicialHoras,
    IReadOnlyList<PasoRequest>? Pasos);

public sealed record CambiarEstadoRequest(string? EstadoActual, string? Estado);

/// <summary>Desde un protocolo aprobado (<see cref="ProtocoloId"/>) o manual (prescripción, con sus pasos).</summary>
public sealed record CrearTratamientoRequest(
    Guid AcuarioId,
    Guid? ProtocoloId,
    string? Motivo,
    string? Diagnostico,
    DateOnly? FechaInicio,
    int? Dias,
    IReadOnlyList<PasoRequest>? Pasos,
    string? Nota);

public sealed record RegistrarAplicacionRequest(decimal? Cantidad, string? Nota);

public sealed record TratamientoCreado(Guid Id, string Codigo, IReadOnlyList<string> Avisos);

public sealed record RegistrarAlimentacionRequest(Guid AcuarioId, Guid? AlimentoId, decimal? Cantidad);

public sealed record FijarReservaRequest(Guid LineaId, Guid LoteId, Guid AcuarioId, int Cantidad);

public sealed record ParametrosSanidadInfo(int HorasAyunoVuelo);

public sealed record AsignarResponsableRequest(Guid? ResponsableId);

/// <summary>Paso ya resuelto para crear las aplicaciones (fecha y dosis calculadas).</summary>
public sealed record AplicacionNueva(
    DateOnly Fecha,
    int Dia,
    int Orden,
    string Accion,
    Guid? MedicamentoId,
    int? PorcentajeAgua,
    decimal? Cantidad,
    string? Unidad,
    string? Nota);

// --- Tablero y Mi día (lo que ve el operario) ---

/// <summary>Especie dentro de un acuario (o un lote por ubicar).</summary>
public sealed record EspecieAcuario(Guid? LoteId, string? LoteCodigo, Guid ProductoId, string ProductoCodigo, string ProductoNombre, string Condicion, int Cantidad);

public sealed record VueloAcuario(Guid PedidoId, string PedidoCodigo, string ClienteNombre, Guid? EmbarqueId, string? EmbarqueCodigo,
    DateTime Salida, bool SinHora, double HorasRestantes, int Reservados);

public sealed record TratamientoAcuario(Guid Id, string Codigo, string Nombre, string Estado, int Dia, int DiasTotal, DateOnly FechaLiberacion,
    int PendientesHoy, bool RequiereAyuno, bool AlimentoMedicado);

/// <summary>Tarjeta del tablero: un acuario con peces, un lote por ubicar o una compra por llegar.</summary>
public sealed record TarjetaSanidad(
    string Clave,
    string Columna,
    Guid? AcuarioId,
    string Titulo,
    string? Sector,
    decimal? Litros,
    string? Responsable,
    int Peces,
    IReadOnlyList<EspecieAcuario> Especies,
    IReadOnlyList<string> Etiquetas,
    TratamientoAcuario? Tratamiento,
    VueloAcuario? Vuelo,
    DateTime? Llegada,
    DateOnly? FechaEstimada,
    string? Detalle);

public sealed record TableroSanidad(int HorasAyunoVuelo, IReadOnlyList<TarjetaSanidad> Tarjetas);

public sealed record RacionAlimento(Guid? AlimentoId, string Alimento, string Unidad, int VecesDia, decimal CantidadToma, string ProductoCodigo);

/// <summary>Una fila de la ronda de alimentación: verde con raciones o roja con los motivos.</summary>
public sealed record RondaAlimentacion(
    Guid AcuarioId,
    string AcuarioCodigo,
    int Peces,
    bool Alimentar,
    IReadOnlyList<string> Motivos,
    string? DetalleMotivo,
    bool AlimentoMedicado,
    IReadOnlyList<RacionAlimento> Raciones,
    bool SinDieta,
    int TomasHoy,
    DateTime? UltimaToma);

/// <summary>Lote por ubicar o llegado hoy, con el protocolo aprobado que se le sugiere.</summary>
public sealed record LlegadaHoy(
    Guid LoteId,
    string LoteCodigo,
    string ProductoCodigo,
    string ProductoNombre,
    int Cantidad,
    Guid? AcuarioId,
    string? AcuarioCodigo,
    DateTime Llegada,
    bool PorUbicar,
    Guid? ProtocoloSugeridoId,
    string? ProtocoloSugerido,
    bool TieneTratamiento);

public sealed record SalidaHoy(
    Guid PedidoId,
    string PedidoCodigo,
    string ClienteNombre,
    Guid? EmbarqueId,
    string? EmbarqueCodigo,
    DateTime? Salida,
    string? Destino,
    IReadOnlyList<SalidaAcuario> Acuarios);

public sealed record SalidaAcuario(Guid AcuarioId, string AcuarioCodigo, Guid LoteId, string ProductoCodigo, int Cantidad);

public sealed record MiDia(
    DateOnly Hoy,
    int HorasAyunoVuelo,
    IReadOnlyList<LlegadaHoy> Llegadas,
    IReadOnlyList<AplicacionFila> Medicacion,
    IReadOnlyList<RondaAlimentacion> Alimentacion,
    IReadOnlyList<SalidaHoy> Salidas,
    IReadOnlyList<string> Avisos);
