using System.Globalization;

namespace JRTropicalFish.Application.Exportaciones;

/// <summary>
/// Reglas de empaque (C# puro). El estándar es por producto (especie y talla); sin estándar se usan los peces por bolsa
/// del catálogo. Los aditivos se dosifican por litro de agua: dosis del medicamento ÷ su base en litros × litros de la
/// bolsa × bolsas. Un costo que no se ha cargado no se inventa: el concepto queda sin valor y se lista en «faltantes».
/// </summary>
public static class ReglasEmpaque
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-CO");

    /// <summary>Lo que se compara con el empaque real: el estándar o, si no hay, los peces por bolsa del catálogo.</summary>
    public sealed record Referencia(Guid? TipoBolsaId, string? TipoBolsaNombre, int? PecesMin, int? PecesMax, bool DelCatalogo);

    public static Referencia ReferenciaDe(EstandarEmpaqueFila p) =>
        p.TieneEstandar
            ? new Referencia(p.TipoBolsaId, p.TipoBolsaNombre, p.PecesMin, p.PecesMax, false)
            : new Referencia(null, null, p.BolsaMin, p.BolsaMax, true);

    /// <summary>Filas del empaque real que no siguen el estándar: otro tipo de bolsa o más/menos peces por bolsa.</summary>
    public static IReadOnlyList<AvisoEmpaque> FueraDeEstandar(
        IEnumerable<EmbarqueEmpaqueFila> empaque, IReadOnlyDictionary<Guid, EstandarEmpaqueFila> estandares)
    {
        var avisos = new List<AvisoEmpaque>();
        foreach (var f in empaque)
        {
            if (!estandares.TryGetValue(f.ProductoId, out var p))
            {
                continue;
            }

            var r = ReferenciaDe(p);
            var fuente = r.DelCatalogo ? "el catálogo" : "el estándar";
            if (r.TipoBolsaId is { } bolsa && f.TipoBolsaId != bolsa)
            {
                avisos.Add(new(f.NumeroCaja, f.ProductoCodigo, $"Bolsa {f.TipoBolsaNombre ?? "sin tipo"}; {fuente} dice {r.TipoBolsaNombre}."));
            }

            if (r.PecesMax is { } max && f.PecesPorBolsa > max)
            {
                avisos.Add(new(f.NumeroCaja, f.ProductoCodigo, $"{f.PecesPorBolsa} peces por bolsa; {fuente} dice máximo {max}."));
            }
            else if (r.PecesMin is { } min && f.PecesPorBolsa < min)
            {
                avisos.Add(new(f.NumeroCaja, f.ProductoCodigo, $"{f.PecesPorBolsa} peces por bolsa; {fuente} dice mínimo {min}."));
            }
        }

        return avisos;
    }

    /// <summary>
    /// Costo de empaque del despacho, con el empaque real: cajas (costo de materiales del tipo), bolsas (costo del tipo),
    /// heat packs (uno por piso), oxígeno por bolsa y aditivos del estándar de cada especie.
    /// </summary>
    public static CostoEmpaqueEmbarque Costo(
        IReadOnlyList<EmbarqueEmpaqueFila> empaque,
        IReadOnlyList<CajaCantidad> cajasReales,
        IReadOnlyDictionary<Guid, TipoCajaFila> tiposCaja,
        IReadOnlyDictionary<Guid, EstandarEmpaqueFila> estandares,
        CostosEmpaque costos)
    {
        var conceptos = new List<ConceptoCostoEmpaque>();
        var faltantes = new List<string>();
        void Agregar(string concepto, decimal cantidad, string unidad, decimal? costo, string? faltante)
        {
            if (cantidad <= 0)
            {
                return;
            }

            if (costo is null && faltante is not null && !faltantes.Contains(faltante))
            {
                faltantes.Add(faltante);
            }

            conceptos.Add(new(concepto, cantidad, unidad, costo, costo is { } c ? Math.Round(c * cantidad, 0) : null));
        }

        var pisos = 0;
        foreach (var caja in cajasReales.Where(c => c.Cantidad > 0))
        {
            var tipo = tiposCaja.GetValueOrDefault(caja.TipoCajaId);
            pisos += caja.Cantidad * (tipo?.Pisos ?? 1);
            var costo = tipo is { CostoCop: > 0 } ? tipo.CostoCop : (decimal?)null;
            Agregar(tipo?.Nombre ?? "Caja", caja.Cantidad, "cajas", costo, $"Costo de materiales de la {tipo?.Nombre ?? "caja"}");
        }

        if (cajasReales.Sum(c => c.Cantidad) == 0 && empaque.Count > 0)
        {
            faltantes.Add("Cajas reales del despacho (paso Factura)");
        }

        var bolsasPorTipo = costos.Bolsas.ToDictionary(b => b.TipoBolsaId);
        foreach (var g in empaque.GroupBy(f => f.TipoBolsaId))
        {
            var bolsa = g.Key is { } id ? bolsasPorTipo.GetValueOrDefault(id) : null;
            var nombre = bolsa?.Nombre ?? "sin tipo";
            Agregar($"Bolsa {nombre}", g.Sum(f => f.Bolsas), "bolsas", bolsa is { CostoCop: > 0 } ? bolsa.CostoCop : null, $"Costo de la bolsa {nombre}");
        }

        var totalBolsas = empaque.Sum(f => f.Bolsas);
        Agregar("Heat pack", pisos, "unidades", costos.Parametros.CostoHeatPackCop, "Costo del heat pack");
        Agregar("Oxígeno", totalBolsas, "bolsas", costos.Parametros.CostoOxigenoBolsaCop, "Costo del oxígeno por bolsa");

        // Aditivos: cantidad de producto = dosis ÷ base (L) × litros de agua × bolsas.
        var aditivos = costos.Aditivos.ToDictionary(a => a.MedicamentoId);
        var porAditivo = new Dictionary<Guid, decimal>();
        foreach (var f in empaque)
        {
            if (!estandares.TryGetValue(f.ProductoId, out var p) || p.Aditivos.Count == 0)
            {
                continue;
            }

            var litros = p.LitrosAgua ?? (f.TipoBolsaId is { } tb ? bolsasPorTipo.GetValueOrDefault(tb)?.LitrosAgua : null);
            if (litros is null)
            {
                var falta = $"Litros de agua de la bolsa {f.TipoBolsaNombre ?? "sin tipo"} (o del estándar de {f.ProductoCodigo})";
                if (!faltantes.Contains(falta))
                {
                    faltantes.Add(falta);
                }

                continue;
            }

            foreach (var id in p.Aditivos)
            {
                if (aditivos.GetValueOrDefault(id) is { DosisBase: > 0 } a)
                {
                    porAditivo[id] = porAditivo.GetValueOrDefault(id) + a.DosisCantidad / a.DosisBase * litros.Value * f.Bolsas;
                }
            }
        }

        foreach (var (id, cantidad) in porAditivo.OrderBy(x => aditivos[x.Key].Nombre, StringComparer.Ordinal))
        {
            var a = aditivos[id];
            Agregar(a.Nombre, Math.Round(cantidad, 2), a.Unidad, a.CostoCopUnidad, $"Costo de {a.Nombre} por {a.Unidad}");
        }

        var total = conceptos.Sum(c => c.Total ?? 0);
        var peces = empaque.Sum(f => f.Cantidad);
        return new CostoEmpaqueEmbarque(
            conceptos, total, faltantes, FueraDeEstandar(empaque, estandares), peces,
            peces > 0 ? Math.Round(total / peces, 2) : null);
    }

    /// <summary>Valida un estándar: peces y litros con sentido, y aditivos que se dosifican por agua.</summary>
    public static string? Validar(GuardarEstandarEmpaqueRequest r, IReadOnlySet<Guid> tiposBolsa, IReadOnlySet<Guid> aditivosValidos)
    {
        if (r.PecesMin is <= 0 or > 10_000 || r.PecesMax is <= 0 or > 10_000)
        {
            return "Los peces por bolsa van de 1 a 10.000.";
        }

        if (r.PecesMin is { } min && r.PecesMax is { } max && min > max)
        {
            return "El mínimo de peces por bolsa no puede ser mayor que el máximo.";
        }

        if (r.LitrosAgua is <= 0 or > 50)
        {
            return "Los litros de agua por bolsa van de más de 0 a 50.";
        }

        if (r.TipoBolsaId is { } bolsa && !tiposBolsa.Contains(bolsa))
        {
            return "El tipo de bolsa no existe.";
        }

        if ((r.Aditivos ?? []).Any(a => !aditivosValidos.Contains(a)))
        {
            return "Un aditivo no existe o no se dosifica por volumen de agua.";
        }

        return (r.Notas?.Trim().Length ?? 0) > 500 ? "Las notas van hasta 500 caracteres." : null;
    }

    public static string Moneda(decimal valor) => valor.ToString("N0", Es);
}
