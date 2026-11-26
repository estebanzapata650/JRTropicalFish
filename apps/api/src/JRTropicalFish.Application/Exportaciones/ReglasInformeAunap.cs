using System.Globalization;

namespace JRTropicalFish.Application.Exportaciones;

/// <summary>
/// Informe mensual AUNAP (Formato Informe Mensual de la DTIV). Las unidades salen <b>solo</b> de los despachos reales
/// de bodega: nunca de la proforma, del pedido ni de lo aprobado en la VUCE. Una fila por exportación × especie AUNAP ×
/// municipio de origen. Los peces de criadero no se reportan; un producto sin código AUNAP no se declara (aviso).
/// </summary>
public static class ReglasInformeAunap
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-CO");

    /// <summary>Primer y último día del mes.</summary>
    public static (DateOnly Desde, DateOnly Hasta) Rango(int anio, int mes)
    {
        var desde = new DateOnly(anio, mes, 1);
        return (desde, desde.AddMonths(1).AddDays(-1));
    }

    /// <summary>"Octubre 2026", como la celda MES del formato.</summary>
    public static string NombreMes(int anio, int mes)
    {
        var nombre = Es.DateTimeFormat.GetMonthName(mes);
        return $"{char.ToUpper(nombre[0], Es)}{nombre[1..]} {anio}";
    }

    public static (IReadOnlyList<FilaInformeAunap> Filas, IReadOnlyList<AvisoInformeAunap> Avisos, int ExcluidosCriadero) Armar(
        IReadOnlyList<DespachoAunapFila> despachos)
    {
        var filas = new List<FilaInformeAunap>();
        var avisos = new List<AvisoInformeAunap>();
        var excluidos = despachos.Where(d => d.Excluir).Sum(d => d.Unidades);

        foreach (var embarque in despachos.GroupBy(d => d.EmbarqueId).OrderBy(g => g.First().FechaExportacion).ThenBy(g => g.First().EmbarqueCodigo))
        {
            var e = embarque.First();
            void Avisar(string texto) => avisos.Add(new AvisoInformeAunap(e.EmbarqueId, e.EmbarqueCodigo, texto));

            if (string.IsNullOrWhiteSpace(e.VuceNumero) || e.VuceFecha is null)
            {
                Avisar("Falta el n.º de trámite VUCE o su fecha de aprobación.");
            }

            if (string.IsNullOrWhiteSpace(e.DexNumero))
            {
                Avisar("Falta el n.º del DEX.");
            }

            if (string.IsNullOrWhiteSpace(e.FacturaDian))
            {
                Avisar("Falta el n.º de la factura DIAN.");
            }

            var reportables = embarque.Where(d => !d.Excluir).ToList();
            foreach (var sinCodigo in reportables.Where(d => d.EspecieAunapId is null).GroupBy(d => d.ProductoCodigo))
            {
                Avisar($"{sinCodigo.Key} no tiene código AUNAP ({Entero(sinCodigo.Sum(d => d.Unidades))} peces): no se puede declarar.");
            }

            foreach (var sinOrigen in reportables.Where(d => d.EspecieAunapId is not null && d.Municipio is null).GroupBy(d => d.CentroNombre))
            {
                Avisar(sinOrigen.Key is null
                    ? "Hay peces despachados de lotes sin centro de acopio: falta el origen."
                    : $"El centro de acopio {sinOrigen.Key} no tiene municipio AUNAP: complétalo en Exportaciones → AUNAP → Orígenes.");
            }

            var conCodigo = reportables.Where(d => d.EspecieAunapId is not null).ToList();
            foreach (var especie in conCodigo.GroupBy(d => d.EspecieAunapId))
            {
                var x = especie.First();
                var salio = especie.Sum(d => d.Unidades);
                if (x.Autorizado is null)
                {
                    Avisar($"{x.Codigo} {x.NombreCientifico}: salieron {Entero(salio)} y no figura en la VUCE.");
                }
                else if (salio > x.Autorizado)
                {
                    Avisar($"{x.Codigo} {x.NombreCientifico}: salieron {Entero(salio)} y la VUCE aprobó {Entero(x.Autorizado.Value)}.");
                }
            }

            var despachado = embarque.Sum(d => d.Unidades);
            if (e.Empacado > 0 && e.Empacado != despachado)
            {
                Avisar($"Bodega despachó {Entero(despachado)} peces y el empaque registra {Entero(e.Empacado)}: revisa antes de enviar el informe.");
            }

            filas.AddRange(conCodigo
                .GroupBy(d => (d.EspecieAunapId, d.Departamento, d.Municipio))
                .Select(g =>
                {
                    var x = g.First();
                    return new FilaInformeAunap(
                        e.EmbarqueId, e.EmbarqueCodigo, x.Codigo!, x.Familia, x.NombreCientifico!, x.NombreComun,
                        x.Departamento, x.Municipio, x.Cuenca, g.Sum(d => d.Unidades), e.Ciudad, e.Pais, e.FechaExportacion,
                        e.DexNumero, e.VuceNumero, e.VuceFecha, e.FacturaDian, e.CitesNumero, e.CitesFecha);
                })
                .OrderBy(f => OrdenCodigo(f.Codigo)).ThenBy(f => f.Codigo, StringComparer.Ordinal).ThenBy(f => f.Municipio, StringComparer.Ordinal));
        }

        return (filas, avisos, excluidos);
    }

    /// <summary>Los códigos son texto ("24*"): primero el número, para que 9 vaya antes de 10.</summary>
    private static int OrdenCodigo(string codigo) =>
        int.TryParse(new string([.. codigo.TakeWhile(char.IsDigit)]), out var n) ? n : int.MaxValue;

    private static string Entero(int n) => n.ToString("N0", Es);
}
