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

public sealed record PermisoAunapFila(string? Numero, DateOnly? Fecha, string? Notas, DateTime ActualizadoEn);

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

public sealed record GuardarPermisoAunapRequest(string? Numero, DateOnly? Fecha, string? Notas, IReadOnlyList<CantidadAutorizada>? Lineas);

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
