namespace JRTropicalFish.Application.Catalogos;

// Los records *Fila salen de las funciones fn_* de la migración FuncionesCatalogos: Dapper los
// construye por posición, así que el orden de campos es el orden de columnas del RETURNS TABLE.

// --- Centros de acopio ---

public sealed record CentroAcopioFila(
    Guid Id, string Nombre, Guid CuencaId, string CuencaNombre, bool Activo, int Productos, int Proveedores, DateTime CreadoEn);

public sealed record GuardarCentroAcopioRequest(string? Nombre, Guid CuencaId, bool Activo);

public sealed record CentroAcopioAuditable(string Nombre, string Cuenca, bool Activo);

// --- Productos ---

public sealed record ProductoFila(
    Guid Id,
    string Codigo,
    Guid GrupoId,
    string GrupoCodigo,
    string GrupoNombre,
    string Tipo,
    string NombreCientifico,
    string? NombreComun,
    string? NombreEspanol,
    decimal? TallaMinCm,
    decimal? TallaMaxCm,
    string? TallaEtiqueta,
    int? BolsaMin,
    int? BolsaMax,
    string? Disponibilidad,
    bool Activo,
    string? Notas,
    DateTime CreadoEn);

public sealed record ProductoCentroFila(Guid ProductoId, Guid CentroAcopioId, string Nombre, int Orden);

public sealed record PrecioFila(Guid ProductoId, Guid ListaPreciosId, decimal Precio);

public sealed record CentroRef(Guid Id, string Nombre);

/// <summary>Producto con sus centros de origen (en orden) y sus precios por lista (sin las de costo si no hay permiso).</summary>
public sealed record ProductoDetalle(
    Guid Id,
    string Codigo,
    Guid GrupoId,
    string GrupoCodigo,
    string GrupoNombre,
    string Tipo,
    string NombreCientifico,
    string? NombreComun,
    string? NombreEspanol,
    decimal? TallaMinCm,
    decimal? TallaMaxCm,
    string? TallaEtiqueta,
    int? BolsaMin,
    int? BolsaMax,
    string? Disponibilidad,
    bool Activo,
    string? Notas,
    DateTime CreadoEn,
    IReadOnlyList<CentroRef> CentrosAcopio,
    IReadOnlyDictionary<Guid, decimal> Precios);

/// <param name="Codigo">Opcional al crear: sin él se asigna el siguiente del grupo. No cambia al editar.</param>
/// <param name="CentrosAcopio">Hasta 3, en orden de preferencia. Reemplaza los actuales.</param>
/// <param name="Precios">
/// Solo las listas indicadas cambian: valor &gt; 0 fija el precio, <c>null</c> o 0 lo quita. Las listas
/// que no vienen no se tocan. Una lista de costo exige <c>catalogos:ver-costos</c>.
/// </param>
public sealed record GuardarProductoRequest(
    string? Codigo,
    Guid GrupoId,
    string? Tipo,
    string? NombreCientifico,
    string? NombreComun,
    string? NombreEspanol,
    decimal? TallaMinCm,
    decimal? TallaMaxCm,
    string? TallaEtiqueta,
    int? BolsaMin,
    int? BolsaMax,
    string? Disponibilidad,
    bool Activo,
    string? Notas,
    IReadOnlyList<Guid>? CentrosAcopio,
    IReadOnlyDictionary<Guid, decimal?>? Precios);

public sealed record ProductoAuditable(
    string Codigo,
    string Grupo,
    string Tipo,
    string NombreCientifico,
    string? NombreComun,
    string? NombreEspanol,
    decimal? TallaMinCm,
    decimal? TallaMaxCm,
    string? TallaEtiqueta,
    int? BolsaMin,
    int? BolsaMax,
    string? Disponibilidad,
    bool Activo,
    string? Notas,
    string? CentrosAcopio);

// --- Listas de precios ---

public sealed record ListaPreciosFila(
    Guid Id,
    string Codigo,
    string Nombre,
    string Moneda,
    bool EsCosto,
    Guid? ClienteId,
    string? ClienteNombre,
    int Orden,
    bool Activo,
    Guid? ListaBaseId,
    string? ListaBaseNombre,
    decimal? FactorSalvaje,
    decimal? FactorCriadero,
    decimal? FactorEspecial,
    bool AplicaImpuestos,
    decimal Redondeo,
    int Productos);

/// <param name="Codigo">Solo al crear (no cambia). Moneda y <paramref name="EsCosto"/> tampoco cambian.</param>
public sealed record GuardarListaPreciosRequest(
    string? Codigo,
    string? Nombre,
    string? Moneda,
    bool EsCosto,
    Guid? ClienteId,
    int Orden,
    bool Activo,
    Guid? ListaBaseId,
    decimal? FactorSalvaje,
    decimal? FactorCriadero,
    decimal? FactorEspecial,
    bool AplicaImpuestos,
    decimal Redondeo);

public sealed record ListaPreciosAuditable(
    string Codigo,
    string Nombre,
    string Moneda,
    bool EsCosto,
    string? Cliente,
    int Orden,
    bool Activo,
    string? ListaBase,
    decimal? FactorSalvaje,
    decimal? FactorCriadero,
    decimal? FactorEspecial,
    bool AplicaImpuestos,
    decimal Redondeo);

public sealed record ParametrosPreciosInfo(decimal Iva, decimal Pasarela, decimal TrmReferencia, DateTime? ActualizadoEn);

public sealed record GuardarParametrosPreciosRequest(decimal Iva, decimal Pasarela, decimal TrmReferencia);

public sealed record ParametrosPreciosAuditable(decimal Iva, decimal Pasarela, decimal TrmReferencia);

/// <summary>Resultado de recalcular una lista: cuántos precios cambiaron y cuántos productos no tenían base.</summary>
public sealed record ResultadoRecalculo(int Calculados, int Cambiados, int SinBase);

// --- Proveedores ---

public sealed record ProveedorFila(
    Guid Id,
    string Nombre,
    Guid TipoIdentificacionId,
    string TipoIdentificacionCodigo,
    string NumeroIdentificacion,
    string? Telefono,
    string? Contacto,
    string? Email,
    string? Notas,
    bool Activo,
    DateTime CreadoEn);

public sealed record ProveedorCentroFila(Guid ProveedorId, Guid CentroAcopioId, string Nombre);

public sealed record ProveedorDetalle(
    Guid Id,
    string Nombre,
    Guid TipoIdentificacionId,
    string TipoIdentificacionCodigo,
    string NumeroIdentificacion,
    string? Telefono,
    string? Contacto,
    string? Email,
    string? Notas,
    bool Activo,
    DateTime CreadoEn,
    IReadOnlyList<CentroRef> CentrosAcopio);

public sealed record GuardarProveedorRequest(
    string? Nombre,
    Guid TipoIdentificacionId,
    string? NumeroIdentificacion,
    string? Telefono,
    string? Contacto,
    string? Email,
    string? Notas,
    bool Activo,
    IReadOnlyList<Guid>? CentrosAcopio);

public sealed record ProveedorAuditable(
    string Nombre,
    string TipoIdentificacion,
    string NumeroIdentificacion,
    string? Telefono,
    string? Contacto,
    string? Email,
    string? Notas,
    bool Activo,
    string? CentrosAcopio);
