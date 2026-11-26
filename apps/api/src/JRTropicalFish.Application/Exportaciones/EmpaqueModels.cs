namespace JRTropicalFish.Application.Exportaciones;

// Empaque. Dapper construye por posición y nombre: el orden y el nombre de cada record = las columnas del RETURNS TABLE
// en la migración FuncionesEmpaque.

public sealed record EstandarEmpaqueFila(
    Guid ProductoId,
    string ProductoCodigo,
    string NombreCientifico,
    string? NombreEspanol,
    string? Talla,
    string GrupoNombre,
    int? BolsaMin,
    int? BolsaMax,
    bool TieneEstandar,
    Guid? TipoBolsaId,
    string? TipoBolsaNombre,
    int? PecesMin,
    int? PecesMax,
    decimal? LitrosAgua,
    string? Notas,
    bool Revisado,
    string? AditivosTexto,
    DateTime? ActualizadoEn)
{
    public IReadOnlyList<Guid> Aditivos =>
        string.IsNullOrEmpty(AditivosTexto) ? [] : [.. AditivosTexto.Split(',').Select(Guid.Parse)];
}

public sealed record ParametrosEmpaqueFila(decimal? CostoHeatPackCop, decimal? CostoOxigenoBolsaCop, DateTime? ActualizadoEn);

public sealed record EmpaqueBolsaFila(Guid TipoBolsaId, string Nombre, string? Tamano, decimal Porcion, decimal CostoCop, decimal? LitrosAgua, bool Activo);

public sealed record AditivoEmpaqueFila(
    Guid MedicamentoId, string Codigo, string Nombre, string Unidad, decimal DosisCantidad, decimal DosisBase, decimal? CostoCopUnidad, string? Fuente);

public sealed record DoaEmpaqueFila(
    Guid ProductoId,
    string ProductoCodigo,
    string NombreCientifico,
    string? NombreEspanol,
    string? Talla,
    Guid? TipoBolsaId,
    string? TipoBolsaNombre,
    int PecesPorBolsa,
    int Embarques,
    int Bolsas,
    int Peces,
    int Doa)
{
    public decimal PorcentajeDoa => Peces == 0 ? 0 : Math.Round(Doa * 100m / Peces, 2);
}

public sealed record CostosEmpaque(ParametrosEmpaqueFila Parametros, IReadOnlyList<EmpaqueBolsaFila> Bolsas, IReadOnlyList<AditivoEmpaqueFila> Aditivos);

// --- Peticiones ---

public sealed record GuardarEstandarEmpaqueRequest(
    Guid? TipoBolsaId, int? PecesMin, int? PecesMax, decimal? LitrosAgua, string? Notas, bool Revisado, IReadOnlyList<Guid>? Aditivos);

public sealed record GuardarParametrosEmpaqueRequest(decimal? CostoHeatPackCop, decimal? CostoOxigenoBolsaCop);

public sealed record GuardarLitrosBolsaRequest(decimal? LitrosAgua);

public sealed record GuardarCostoAditivoRequest(decimal? CostoCopUnidad);

// --- Costo y estándar de un despacho ---

/// <summary>Una línea del costo de empaque: concepto, cantidad, costo unitario y total (nulos si falta el costo).</summary>
public sealed record ConceptoCostoEmpaque(string Concepto, decimal Cantidad, string Unidad, decimal? CostoUnitario, decimal? Total);

/// <summary>Una fila del empaque real que se sale de su estándar.</summary>
public sealed record AvisoEmpaque(int NumeroCaja, string ProductoCodigo, string Texto);

public sealed record CostoEmpaqueEmbarque(
    IReadOnlyList<ConceptoCostoEmpaque> Conceptos,
    decimal Total,
    IReadOnlyList<string> Faltantes,
    IReadOnlyList<AvisoEmpaque> FueraDeEstandar,
    int Peces,
    decimal? CostoPorPez);
