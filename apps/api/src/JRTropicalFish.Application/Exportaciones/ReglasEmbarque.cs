using System.Globalization;
using JRTropicalFish.Domain.Exportaciones;

namespace JRTropicalFish.Application.Exportaciones;

/// <summary>
/// Cálculos de los documentos del embarque, con las mismas fórmulas del libro «Formatos Comerciales»
/// (hojas Preinvoice, Est. Freight Costs, Orden cargue, Lista Empaque, Factura, Credit Note y Balance).
/// Funciones puras: se prueban con los números reales del despacho 12186.
/// </summary>
public static class ReglasEmbarque
{
    /// <summary>Número de preinvoice como lo arma el Excel: año y día del año ("2026-240").</summary>
    public static string NumeroPreinvoice(DateOnly fecha) =>
        $"{fecha.Year}-{fecha.DayOfYear.ToString("000", CultureInfo.InvariantCulture)}";

    /// <summary>"2 (Expo) * 45": bolsas, primeras 4 letras del tipo de bolsa y peces por bolsa.</summary>
    public static string TextoEmpaque(int bolsas, string? tipoBolsa, int pecesPorBolsa)
    {
        var tipo = string.IsNullOrWhiteSpace(tipoBolsa) ? "" : $" ({(tipoBolsa.Length > 4 ? tipoBolsa[..4] : tipoBolsa).Trim()})";
        return $"{bolsas}{tipo} * {pecesPorBolsa}";
    }

    /// <summary>Heat packs: uno por piso de cada caja.</summary>
    public static int HeatPacks(IEnumerable<CajaCantidad> cajas, IReadOnlyDictionary<Guid, TipoCajaFila> tipos) =>
        cajas.Sum(c => tipos.TryGetValue(c.TipoCajaId, out var t) ? c.Cantidad * t.Pisos : c.Cantidad);

    /// <summary>
    /// Flete estimado (hoja Est. Freight Costs): kg por tipo de caja; cargo por peso a la tarifa del cliente
    /// más fuel y JP fee; VSC con su mínimo; DBC % sobre el cargo por peso; DF % sobre el due agent.
    /// </summary>
    public static CalculoFlete Flete(IEnumerable<CajaCantidad> cajas, IReadOnlyDictionary<Guid, TipoCajaFila> tipos, TarifasFleteInfo t)
    {
        var pesos = cajas
            .Where(c => c.Cantidad > 0 && tipos.ContainsKey(c.TipoCajaId))
            .Select(c => new PesoCajas(tipos[c.TipoCajaId].DescripcionFactura, c.Cantidad, tipos[c.TipoCajaId].KgPorCaja))
            .ToList();
        var kg = pesos.Sum(p => p.Kg);
        var tarifa = Dinero(kg * t.TarifaKgCliente);
        var fuel = Dinero(kg * t.FuelKg);
        var jp = Dinero(kg * t.JpFeeKg);
        var cargoPeso = tarifa + fuel + jp;
        var vsc = ConMinimo(kg * t.VscKg, t.VscMinimo);
        var dbc = ConMinimo(cargoPeso * t.DbcPorcentaje / 100m, t.DbcMinimo);
        var df = ConMinimo(t.DueAgentCliente * t.DfPorcentaje / 100m, t.DfMinimo);
        var total = cargoPeso + vsc + dbc + df + t.DueAgentCliente + t.DueCarrier;
        return new CalculoFlete(pesos, kg, tarifa, fuel, jp, vsc, dbc, df, t.DueAgentCliente, t.DueCarrier, Dinero(total));
    }

    /// <summary>
    /// Preinvoice: peces × precio, descuento, cajas estimadas, heat packs, certificados y cargos, y el AWB
    /// estimado. Anticipo: 50 % del valor de los peces y 100 % del AWB.
    /// </summary>
    public static CalculoPreinvoice Preinvoice(
        IReadOnlyList<EmbarqueLineaFila> lineas,
        decimal descuentoPorcentaje,
        IReadOnlyList<CajaCantidad> cajas,
        IReadOnlyDictionary<Guid, TipoCajaFila> tipos,
        IReadOnlyList<CargoEmbarque> cargos,
        decimal awbEstimado)
    {
        var subtotal = lineas.Sum(l => l.Total);
        var descuento = Dinero(subtotal * descuentoPorcentaje / 100m);
        var lineasCargo = CargosDeCajas(cajas, tipos, cargos);
        var total = subtotal - descuento + lineasCargo.Sum(c => c.Total) + awbEstimado;
        return new CalculoPreinvoice(
            lineas.Sum(l => l.Cantidad),
            subtotal,
            descuento,
            lineasCargo,
            awbEstimado,
            Dinero(total),
            lineas.Sum(l => l.FraccionCaja),
            Dinero(subtotal * 0.5m),
            awbEstimado);
    }

    /// <summary>
    /// Orden de cargue: cada especie ocupa su fracción de caja en el orden de la lista; el acumulado dice
    /// en qué cajas va ("13 - 14"). Sin fracción, la especie queda sin caja asignada.
    /// </summary>
    public static IReadOnlyList<FilaOrdenCargue> OrdenCargue(IReadOnlyList<EmbarqueLineaFila> lineas)
    {
        var filas = new List<FilaOrdenCargue>();
        var acumulado = 0m;
        foreach (var l in lineas.OrderBy(l => l.Orden))
        {
            var anterior = acumulado;
            acumulado += l.FraccionCaja;
            int? inicio = null, fin = null;
            if (l.FraccionCaja > 0)
            {
                inicio = (int)Math.Floor(anterior) + 1;
                fin = Math.Max(inicio.Value, (int)Math.Ceiling(acumulado));
            }

            var especie = PrimerNombre(l.NombreComun) ?? l.NombreCientifico;
            filas.Add(new FilaOrdenCargue(l.Orden, l.ProductoCodigo, especie, l.Cantidad, l.Empaque, l.Nota, l.FraccionCaja, acumulado, inicio, fin));
        }

        return filas;
    }

    /// <summary>
    /// Factura: por caja desde el empaque real (bolsas × peces × precio); descuento; cajas reales con su
    /// heat pack; certificados y cargos; y el AWB real (o el estimado mientras no se conozca).
    /// </summary>
    public static CalculoFactura Factura(
        IReadOnlyList<EmbarqueEmpaqueFila> empaque,
        decimal descuentoPorcentaje,
        IReadOnlyList<CajaCantidad> cajas,
        IReadOnlyDictionary<Guid, TipoCajaFila> tipos,
        IReadOnlyList<CargoEmbarque> cargos,
        decimal? awbReal,
        decimal awbEstimado)
    {
        var subtotal = empaque.Sum(e => e.Total);
        var descuento = Dinero(subtotal * descuentoPorcentaje / 100m);
        var lineasCargo = CargosDeCajas(cajas, tipos, cargos);
        var awb = awbReal ?? awbEstimado;
        var total = subtotal - descuento + lineasCargo.Sum(c => c.Total) + awb;
        var costoBolsas = empaque.Sum(e => (e.TipoBolsaCosto ?? 0) * e.Bolsas);
        var cajasEmpacadas = empaque.Select(e => e.NumeroCaja).Distinct().Count();
        return new CalculoFactura(
            empaque.Sum(e => e.Cantidad),
            cajas.Sum(c => c.Cantidad) is > 0 and var n ? n : cajasEmpacadas,
            subtotal,
            descuento,
            lineasCargo,
            awb,
            awbReal is null,
            Dinero(total),
            costoBolsas);
    }

    /// <summary>Nota crédito: muertos reportados (DOA + ADD + FINAL) por especie × precio, menos el descuento.</summary>
    public static CalculoNotaCredito NotaCredito(IReadOnlyList<EmbarqueEmpaqueFila> empaque, decimal descuentoPorcentaje)
    {
        var lineas = empaque
            .Where(e => e.Muertos > 0)
            .GroupBy(e => (e.ProductoCodigo, e.NombreCientifico, e.Precio))
            .Select(g => new LineaNotaCredito(
                g.Key.ProductoCodigo, g.Key.NombreCientifico, g.First().NombreFactura ?? PrimerNombre(g.First().NombreComun), g.Sum(e => e.Muertos), g.Key.Precio))
            .OrderBy(l => l.ProductoCodigo, StringComparer.Ordinal)
            .ToList();
        var subtotal = lineas.Sum(l => l.Total);
        var descuento = Dinero(subtotal * descuentoPorcentaje / 100m);
        return new CalculoNotaCredito(lineas, lineas.Sum(l => l.Muertos), subtotal, descuento, subtotal - descuento);
    }

    public static CalculoBalance Balance(CalculoFactura factura, CalculoNotaCredito nota, decimal anticipo) =>
        new(factura.Total, nota.Total, anticipo);

    /// <summary>Todos los cálculos del embarque de una vez (ficha y PDF usan los mismos números).</summary>
    public static CalculosEmbarque Calcular(
        EmbarqueFila e,
        TarifasFleteInfo tarifas,
        IReadOnlyList<CargoEmbarque> cargos,
        IReadOnlyList<CajaCantidad> estimadas,
        IReadOnlyList<CajaCantidad> reales,
        IReadOnlyList<EmbarqueLineaFila> lineas,
        IReadOnlyList<EmbarqueEmpaqueFila> empaque,
        IReadOnlyDictionary<Guid, TipoCajaFila> tipos)
    {
        var fleteEstimado = Flete(estimadas, tipos, tarifas);
        var preinvoice = Preinvoice(lineas, e.DescuentoPeces, estimadas, tipos, cargos, fleteEstimado.Total);
        var cajasFactura = reales.Any(c => c.Cantidad > 0) ? reales : estimadas;
        var factura = Factura(empaque, e.DescuentoPeces, cajasFactura, tipos, cargos, e.AwbValor, Flete(cajasFactura, tipos, tarifas).Total);
        var nota = NotaCredito(empaque, e.DescuentoPeces);
        return new CalculosEmbarque(preinvoice, fleteEstimado, OrdenCargue(lineas), factura, nota, Balance(factura, nota, e.AnticipoRecibido));
    }

    // --- Validaciones ---

    public static string? ValidarLineas(IReadOnlyList<LineaEmbarqueRequest> lineas) =>
        lineas.Count == 0 ? "Agrega al menos una especie."
        : lineas.Any(l => l.Cantidad <= 0) ? "Cada especie necesita una cantidad mayor a cero."
        : lineas.Any(l => l.Precio is < 0) ? "El precio no puede ser negativo."
        : lineas.Any(l => l.FraccionCaja is < 0 or > 50) ? "La fracción de caja va de 0 a 50."
        : lineas.GroupBy(l => l.ProductoId).Any(g => g.Count() > 1) ? "Una especie aparece dos veces: súmala en una sola línea."
        : null;

    public static string? ValidarEmpaque(IReadOnlyList<FilaEmpaqueRequest> filas) =>
        filas.Any(f => f.NumeroCaja <= 0 || f.NumeroCaja > 500) ? "El número de caja va de 1 a 500."
        : filas.Any(f => f.Bolsas <= 0) ? "Cada fila necesita al menos una bolsa."
        : filas.Any(f => f.PecesPorBolsa <= 0) ? "Indica cuántos peces van por bolsa."
        : filas.Any(f => f.Precio is < 0) ? "El precio no puede ser negativo."
        : null;

    public static string? ValidarCajas(IReadOnlyList<CajaCantidad> cajas, IReadOnlyDictionary<Guid, TipoCajaFila> tipos) =>
        cajas.Any(c => c.Cantidad < 0 || c.Cantidad > 500) ? "Las cajas van de 0 a 500."
        : cajas.Any(c => !tipos.ContainsKey(c.TipoCajaId)) ? "Hay un tipo de caja que no existe."
        : cajas.GroupBy(c => c.TipoCajaId).Any(g => g.Count() > 1) ? "Un tipo de caja aparece dos veces."
        : null;

    public static string? ValidarCargos(IReadOnlyList<CargoEmbarque> cargos) =>
        cargos.Any(c => !CodigosCargo.Todos.Contains(c.Codigo)) ? "Hay un cargo que no existe."
        : cargos.Any(c => c.Cantidad is < 0 or > 100 || c.Precio < 0) ? "Revisa las cantidades y precios de los cargos."
        : null;

    /// <summary>Transiciones manuales. Facturado y cerrado los pone el sistema (emitir factura / nota crédito).</summary>
    public static bool TransicionManualValida(string actual, string nuevo) => (actual, nuevo) switch
    {
        (EstadosEmbarque.Borrador, EstadosEmbarque.PreinvoiceEnviada) => true,
        (EstadosEmbarque.PreinvoiceEnviada, EstadosEmbarque.Confirmado) => true,
        (EstadosEmbarque.PreinvoiceEnviada, EstadosEmbarque.Borrador) => true,
        (EstadosEmbarque.Confirmado, EstadosEmbarque.Empacado) => true,
        (EstadosEmbarque.Confirmado, EstadosEmbarque.PreinvoiceEnviada) => true,
        (EstadosEmbarque.Empacado, EstadosEmbarque.Confirmado) => true,
        (EstadosEmbarque.Facturado, EstadosEmbarque.Cerrado) => true,
        (_, EstadosEmbarque.Cancelado) => EstadosEmbarque.Editables.Contains(actual),
        _ => false,
    };

    // --- Auxiliares ---

    /// <summary>Cajas (por tipo), heat packs y los cargos con cantidad (certificados, airport fee…).</summary>
    private static List<LineaCargo> CargosDeCajas(
        IReadOnlyList<CajaCantidad> cajas, IReadOnlyDictionary<Guid, TipoCajaFila> tipos, IReadOnlyList<CargoEmbarque> cargos)
    {
        var lineas = new List<LineaCargo>();
        foreach (var c in cajas.Where(c => c.Cantidad > 0 && tipos.ContainsKey(c.TipoCajaId)))
        {
            lineas.Add(new LineaCargo(tipos[c.TipoCajaId].DescripcionFactura, c.Cantidad, tipos[c.TipoCajaId].PrecioUsd));
        }

        var heat = cargos.FirstOrDefault(c => c.Codigo == CodigosCargo.HeatPack);
        var heatPacks = HeatPacks(cajas, tipos);
        if (heat is not null && heatPacks > 0)
        {
            lineas.Add(new LineaCargo(heat.Nombre, heatPacks, heat.Precio));
        }

        lineas.AddRange(cargos.Where(c => c.Codigo != CodigosCargo.HeatPack && c.Cantidad > 0).Select(c => new LineaCargo(c.Nombre, c.Cantidad, c.Precio)));
        return lineas;
    }

    /// <summary>Si hay mínimo (> 0), se cobra el mayor entre el cálculo y el mínimo — salvo que el cálculo sea 0.</summary>
    private static decimal ConMinimo(decimal calculado, decimal minimo) =>
        Dinero(calculado <= 0 ? 0 : minimo > 0 ? Math.Max(calculado, minimo) : calculado);

    private static decimal Dinero(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    /// <summary>Primer nombre común ("Dainty Cory | Salt and Pepper Cory" → "Dainty Cory").</summary>
    public static string? PrimerNombre(string? nombres) =>
        string.IsNullOrWhiteSpace(nombres) ? null : nombres.Split('|')[0].Trim();
}
