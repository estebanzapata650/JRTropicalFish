namespace JRTropicalFish.Application.Exportaciones;

// AUNAP. Dapper construye por posición y nombre: el orden y el nombre de cada record = las columnas del RETURNS TABLE
// en la migración FuncionesAunap.

public sealed record EspecieAunapFila(
    Guid Id,
    string Codigo,
    string NombreCientifico,
    string? NombreComun,
    bool Amazonia,
    bool Orinoquia,
    bool Magdalena,
    bool Pacifico,
    bool Caribe,
    string? Orden,
    string? Familia,
    bool Activo,
    int Productos);

public sealed record ProductoAunapFila(
    Guid ProductoId,
    string ProductoCodigo,
    string NombreCientifico,
    string? NombreComun,
    string? GrupoNombre,
    bool Activo,
    Guid? EspecieAunapId,
    string? AunapCodigo,
    string? AunapNombreCientifico,
    string? AunapNombreComun,
    string? AunapFamilia);

/// <summary>Trámites del embarque: VUCE (<see cref="Numero"/> = TM, <see cref="Fecha"/> = aprobación), proforma, DEX, factura DIAN, CITES.</summary>
public sealed record PermisoAunapFila(
    string? Numero,
    DateOnly? Fecha,
    string? Notas,
    DateTime ActualizadoEn,
    DateOnly? VuceFechaSolicitud = null,
    string? ProformaNumero = null,
    DateOnly? ProformaFecha = null,
    string? DexNumero = null,
    string? FacturaDian = null,
    string? CitesNumero = null,
    DateOnly? CitesFecha = null);

public sealed record PermisoAunapLineaFila(Guid EspecieAunapId, int CantidadAutorizada);

public sealed record ImportacionAunap(int Nuevas, int Actualizadas);

// --- Peticiones ---

public sealed record EspecieAunapImportar(
    string? Codigo,
    string? NombreCientifico,
    string? NombreComun,
    bool Amazonia,
    bool Orinoquia,
    bool Magdalena,
    bool Pacifico,
    bool Caribe,
    string? Orden,
    string? Familia);

public sealed record ImportarAunapRequest(IReadOnlyList<EspecieAunapImportar>? Especies);

public sealed record EnlazarProductoAunapRequest(Guid? EspecieAunapId);

public sealed record CantidadAutorizada(Guid EspecieAunapId, int Cantidad);

/// <param name="Numero">N.º de trámite VUCE (TM-…).</param>
/// <param name="Fecha">Fecha de aprobación de la VUCE.</param>
public sealed record GuardarPermisoAunapRequest(
    string? Numero,
    DateOnly? Fecha,
    string? Notas,
    IReadOnlyList<CantidadAutorizada>? Lineas,
    DateOnly? VuceFechaSolicitud = null,
    string? DexNumero = null,
    string? FacturaDian = null,
    string? CitesNumero = null,
    DateOnly? CitesFecha = null);

// --- Resumen del embarque ---

public static class EstadosAunap
{
    /// <summary>Aún no se registra el permiso del embarque.</summary>
    public const string SinPermiso = "sin_permiso";

    /// <summary>Hay permiso, pero esta especie no está autorizada en él.</summary>
    public const string NoAutorizada = "no_autorizada";

    /// <summary>Lo empacado (o, sin empaque, lo pedido) supera lo autorizado.</summary>
    public const string Excede = "excede";

    public const string Ok = "ok";
}

/// <summary>Una especie AUNAP del embarque: lo pedido, lo autorizado y lo realmente empacado.</summary>
public sealed record FilaAunap(
    Guid EspecieAunapId,
    string Codigo,
    string NombreCientifico,
    string? NombreComun,
    string? Familia,
    IReadOnlyList<string> Productos,
    int Solicitado,
    int? Autorizado,
    int Empacado,
    string Estado);

/// <summary>Producto del embarque sin código AUNAP: no se puede declarar ni exportar.</summary>
public sealed record ProductoSinAunap(Guid ProductoId, string ProductoCodigo, string NombreCientifico, int Cantidad);

public sealed record ResumenAunap(
    PermisoAunapFila? Permiso,
    IReadOnlyList<FilaAunap> Filas,
    IReadOnlyList<ProductoSinAunap> SinCodigo,
    bool HayEmpaque,
    IReadOnlyList<string> Avisos);

/// <summary>Una fila de un documento para autoridades: la especie AUNAP con su cantidad y valor reales.</summary>
public sealed record LineaDeclarada(
    string Codigo,
    string Familia,
    string NombreCientifico,
    string NombreComun,
    int Cantidad,
    decimal Total)
{
    public decimal PrecioPromedio => Cantidad == 0 ? 0 : Math.Round(Total / Cantidad, 4, MidpointRounding.AwayFromZero);
}

// --- Informe mensual AUNAP (migración FuncionesInformeAunap) ---

public sealed record MunicipioAunapFila(int Id, string Departamento, string Municipio, string Cuenca);

public sealed record CentroAcopioAunapFila(
    Guid CentroAcopioId,
    string CentroNombre,
    string CuencaInterna,
    int? MunicipioAunapId,
    string? Departamento,
    string? Municipio,
    string? Cuenca,
    bool Excluir);

/// <param name="Excluir">No se reporta (peces de criadero).</param>
public sealed record GuardarCentroAunapRequest(int? MunicipioAunapId, bool Excluir);

public sealed record ParametrosAunapFila(
    string? RepresentanteLegal,
    string? Resolucion,
    DateOnly? VigenciaDesde,
    DateOnly? VigenciaHasta,
    string? Direccion,
    string? Correo,
    string? Telefono,
    DateTime? ActualizadoEn);

public sealed record GuardarParametrosAunapRequest(
    string? RepresentanteLegal,
    string? Resolucion,
    DateOnly? VigenciaDesde,
    DateOnly? VigenciaHasta,
    string? Direccion,
    string? Correo,
    string? Telefono);

/// <summary>Lo despachado de un producto de un centro de acopio en un embarque (<c>fn_informe_aunap</c>).</summary>
public sealed record DespachoAunapFila(
    Guid EmbarqueId,
    string EmbarqueCodigo,
    DateOnly FechaExportacion,
    string? Ciudad,
    string? Pais,
    string? VuceNumero,
    DateOnly? VuceFecha,
    string? DexNumero,
    string? FacturaDian,
    string? CitesNumero,
    DateOnly? CitesFecha,
    int Empacado,
    string ProductoCodigo,
    Guid? EspecieAunapId,
    string? Codigo,
    string? Familia,
    string? NombreCientifico,
    string? NombreComun,
    string? CentroNombre,
    string? Departamento,
    string? Municipio,
    string? Cuenca,
    bool Excluir,
    int? Autorizado,
    int Unidades);

/// <summary>Una fila del formato AUNAP: especie × origen × exportación, con las unidades que salieron.</summary>
public sealed record FilaInformeAunap(
    Guid EmbarqueId,
    string EmbarqueCodigo,
    string Codigo,
    string? Familia,
    string NombreCientifico,
    string? NombreComun,
    string? Departamento,
    string? Municipio,
    string? Cuenca,
    int Unidades,
    string? Ciudad,
    string? Pais,
    DateOnly FechaExportacion,
    string? DexNumero,
    string? VuceNumero,
    DateOnly? VuceFecha,
    string? FacturaDian,
    string? CitesNumero,
    DateOnly? CitesFecha);

/// <summary>Algo que falta o no cuadra en un embarque del mes.</summary>
public sealed record AvisoInformeAunap(Guid EmbarqueId, string EmbarqueCodigo, string Texto);

public sealed record InformeAunap(
    int Anio,
    int Mes,
    string RazonSocial,
    ParametrosAunapFila Parametros,
    IReadOnlyList<FilaInformeAunap> Filas,
    IReadOnlyList<AvisoInformeAunap> Avisos,
    int ExcluidosCriadero,
    int Embarques);

public sealed record ArchivoGenerado(byte[] Contenido, string Nombre, string TipoContenido);
