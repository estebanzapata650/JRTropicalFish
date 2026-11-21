using JRTropicalFish.Application.Exportaciones;
using JRTropicalFish.Domain.Exportaciones;

namespace JRTropicalFish.Tests.Exportaciones;

/// <summary>
/// Las fórmulas del libro «Formatos Comerciales» contra un despacho real: la factura JRCH 12186 a
/// Kamihata Tokyo (21 cajas dobles, 6.174 peces, USD 8.033,05; DOA USD 335,22).
/// </summary>
public class ReglasEmbarqueTests
{
    private static readonly TipoCajaFila Doble = new(Guid.NewGuid(), "Caja Holandesa", "Styrofoam Box Packing Double", 61117.8m, 20m, 20m, 1, 1, true);
    private static readonly TipoCajaFila Sencilla = new(Guid.NewGuid(), "Caja Europea", "Styrofoam Box Packing Single", 35706m, 13m, 10m, 1, 2, true);
    private static readonly TipoCajaFila DoblePiso = new(Guid.NewGuid(), "Caja Doble Piso", "Styrofoam Box 2 Level Floor", 0m, 38m, 32m, 2, 3, true);

    private static readonly Dictionary<Guid, TipoCajaFila> Tipos = new[] { Doble, Sencilla, DoblePiso }.ToDictionary(t => t.Id);

    private static readonly CargoEmbarque HeatPack = new(CodigosCargo.HeatPack, "Heat Pack", 0, 1.6m);

    private static readonly TarifasFleteInfo TarifasExcel = new(6.5m, 6.7m, 0m, 1m, 0.13m, 15m, 0m, 0m, 0m, 0m, 140m, 250m, 55m);

    /// <summary>(caja, código, bolsas, peces por bolsa, precio, DOA, ADD, FINAL) — la factura 12186 fila por fila.</summary>
    private static readonly (int Caja, string Cod, int Bolsas, int Peces, decimal Precio, int Doa, int Add, int Final)[] Factura12186 =
    [
        (1, "LO280", 2, 45, 2.6m, 6, 0, 0), (2, "LO300", 4, 20, 3.8m, 0, 0, 2), (3, "LO375", 2, 100, 0.7m, 4, 0, 0),
        (4, "LO190", 1, 50, 0.4m, 1, 0, 0), (4, "LO435", 1, 45, 0.4m, 0, 0, 1), (5, "CO035", 2, 140, 0.42m, 0, 1, 0),
        (6, "CO125", 2, 550, 0.7m, 30, 9, 7), (7, "CO130", 1, 45, 3.2m, 3, 0, 0), (7, "CO130", 1, 43, 3.2m, 0, 0, 0),
        (8, "CO055", 2, 140, 0.65m, 5, 2, 7), (9, "CO145", 2, 140, 0.4m, 0, 1, 6), (10, "CO150", 1, 145, 0.55m, 0, 1, 2),
        (10, "CO165", 1, 120, 0.46m, 0, 0, 3), (11, "CO160", 1, 70, 0.4m, 18, 5, 7), (11, "CO160", 1, 118, 0.4m, 0, 0, 0),
        (12, "CO120", 1, 120, 0.45m, 0, 0, 0), (12, "CO175", 1, 100, 0.5m, 8, 1, 91), (13, "CF065", 1, 90, 0.78m, 0, 4, 0),
        (13, "CF085", 1, 100, 1.85m, 1, 1, 0), (14, "CF020", 1, 9, 7.5m, 0, 0, 0), (14, "CF020", 1, 8, 7.5m, 0, 0, 0),
        (14, "CF025", 2, 1, 8.5m, 0, 0, 0), (14, "CF025", 2, 1, 8.5m, 0, 0, 0), (14, "CF100", 1, 1, 25m, 0, 0, 0),
        (14, "CF100", 2, 1, 25m, 0, 0, 0), (14, "CC105", 1, 1, 10m, 0, 0, 0), (15, "CH170", 3, 7, 2m, 0, 0, 0),
        (15, "CH170", 1, 8, 2m, 0, 0, 0), (16, "ST035", 2, 80, 0.3m, 80, 0, 0), (16, "TE150", 1, 320, 0.15m, 8, 4, 4),
        (17, "CH065", 1, 25, 2.4m, 0, 0, 0), (17, "CH040", 1, 15, 2.5m, 0, 0, 0), (17, "CC100", 1, 8, 7m, 0, 0, 0),
        (18, "CH275", 2, 10, 6m, 0, 0, 0), (18, "CC105", 1, 1, 10m, 0, 0, 0), (18, "CC147", 1, 45, 1.85m, 3, 0, 0),
        (19, "CC147", 1, 20, 1.8m, 0, 0, 0), (19, "TE060", 2, 75, 0.5m, 0, 0, 1), (19, "TE130", 1, 200, 0.25m, 0, 1, 0),
        (20, "CF190", 2, 30, 3.5m, 0, 0, 0), (20, "CH055", 1, 25, 2m, 0, 2, 0), (20, "CF010", 1, 115, 0.35m, 0, 0, 5),
        (21, "TE005", 2, 800, 0.18m, 85, 101, 614),
    ];

    private static List<EmbarqueEmpaqueFila> Empaque12186() =>
        [.. Factura12186.Select((f, i) => new EmbarqueEmpaqueFila(
            Guid.NewGuid(), f.Caja, i, Guid.NewGuid(), f.Cod, f.Cod, null, null, null, "Export", 1m, 3490m,
            f.Bolsas, f.Peces, f.Precio, f.Doa, f.Add, f.Final, null))];

    private static EmbarqueLineaFila Linea(int orden, string cod, int cantidad, decimal precio, decimal fraccion) =>
        new(Guid.NewGuid(), orden, Guid.NewGuid(), cod, cod, $"{cod} common | other", null, null, cantidad, precio, fraccion, null, null);

    [Fact]
    public void Factura_12186_cuadra_con_la_del_excel()
    {
        var empaque = Empaque12186();
        var cargos = new[] { HeatPack, new CargoEmbarque(CodigosCargo.AirportFee, "Charges Prepaid & Miscelaneus", 1, 70m) };

        var f = ReglasEmbarque.Factura(empaque, 0m, [new CajaCantidad(Doble.Id, 21)], Tipos, cargos, awbReal: 3244.40m, awbEstimado: 0m);

        Assert.Equal(6174, f.Unidades);
        Assert.Equal(4265.05m, f.SubtotalPeces);
        Assert.Equal(21, f.Cajas);
        Assert.Equal([420m, 33.6m, 70m], f.Cargos.Select(c => c.Total));
        Assert.False(f.AwbEstimado);
        Assert.Equal(8033.05m, f.Total);
    }

    [Fact]
    public void Nota_credito_12186_suma_doa_add_y_final_por_especie()
    {
        var nota = ReglasEmbarque.NotaCredito(Empaque12186(), 0m);

        Assert.Equal(335.22m, nota.Total);
        // Los dos renglones de CO160 (mismo precio) se agrupan: 18 + 5 + 7 = 30 muertos.
        Assert.Equal(30, nota.Lineas.Single(l => l.ProductoCodigo == "CO160").Muertos);
        Assert.Equal(144m, nota.Lineas.Single(l => l.ProductoCodigo == "TE005").Total);
    }

    [Fact]
    public void Balance_es_factura_menos_nota_credito_menos_anticipo()
    {
        var empaque = Empaque12186();
        var f = ReglasEmbarque.Factura(empaque, 0m, [new CajaCantidad(Doble.Id, 21)], Tipos, [HeatPack], 3244.40m, 0m);
        var b = ReglasEmbarque.Balance(f, ReglasEmbarque.NotaCredito(empaque, 0m), anticipo: 6015.53m);

        Assert.Equal(7963.05m - 335.22m - 6015.53m, b.Saldo);
    }

    [Fact]
    public void Flete_estimado_del_excel_37_cajas_sencillas()
    {
        // Hoja Est. Freight Costs: 370 kg × (6,7 + 0 + 1) = 2.849; VSC 48,1; due agent 250; carrier 55.
        var flete = ReglasEmbarque.Flete([new CajaCantidad(Sencilla.Id, 37)], Tipos, TarifasExcel);

        Assert.Equal(370m, flete.Kg);
        Assert.Equal(2849m, flete.CargoPeso);
        Assert.Equal(48.1m, flete.Vsc);
        Assert.Equal(3202.1m, flete.Total);
    }

    [Fact]
    public void Vsc_respeta_el_minimo()
    {
        var flete = ReglasEmbarque.Flete([new CajaCantidad(Sencilla.Id, 2)], Tipos, TarifasExcel);

        Assert.Equal(15m, flete.Vsc); // 20 kg × 0,13 = 2,6 < mínimo 15
    }

    [Fact]
    public void Preinvoice_suma_peces_descuento_cajas_heat_packs_cargos_y_awb_y_calcula_el_anticipo()
    {
        var lineas = new[] { Linea(1, "CO125", 1200, 0.7m, 1m), Linea(2, "TE005", 1000, 0.18m, 0.5m) };
        var cargos = new[] { HeatPack, new CargoEmbarque(CodigosCargo.Zoosanitario, "Zoosanitary certificate", 1, 50m) };

        var p = ReglasEmbarque.Preinvoice(lineas, 15m, [new CajaCantidad(DoblePiso.Id, 2)], Tipos, cargos, awbEstimado: 3437m);

        Assert.Equal(1020m, p.SubtotalPeces);
        Assert.Equal(153m, p.Descuento);
        // 2 cajas de doble piso = 4 heat packs.
        Assert.Equal([76m, 6.4m, 50m], p.Cargos.Select(c => c.Total));
        Assert.Equal(1020m - 153m + 76m + 6.4m + 50m + 3437m, p.Total);
        Assert.Equal(510m + 3437m, p.Anticipo);
        Assert.Equal(1.5m, p.CajasSugeridas);
    }

    [Fact]
    public void Orden_de_cargue_reparte_las_especies_en_cajas_por_fraccion()
    {
        var filas = ReglasEmbarque.OrdenCargue(
        [
            Linea(1, "CF065", 90, 0.78m, 0.5m), Linea(2, "CF085", 100, 1.85m, 0.5m), Linea(3, "CF010", 125, 0.35m, 0.25m),
            Linea(4, "CO125", 1200, 0.7m, 1m), Linea(5, "LO045", 12, 7m, 0m),
        ]);

        Assert.Equal(["1", "1", "2", "2 - 3", "—"], filas.Select(f => f.Cajas));
        Assert.Equal("CF065 common", filas[0].Especie);
    }

    [Theory]
    [InlineData("2026-08-28", "2026-240")]
    [InlineData("2026-01-05", "2026-005")]
    public void Numero_de_preinvoice_es_anio_y_dia_del_anio(string fecha, string numero) =>
        Assert.Equal(numero, ReglasEmbarque.NumeroPreinvoice(DateOnly.Parse(fecha)));

    [Theory]
    [InlineData(2, "Export", 45, "2 (Expo) * 45")]
    [InlineData(4, "1/4 Baja", 125, "4 (1/4) * 125")]
    [InlineData(3, null, 7, "3 * 7")]
    public void Texto_de_empaque(int bolsas, string? tipo, int peces, string esperado) =>
        Assert.Equal(esperado, ReglasEmbarque.TextoEmpaque(bolsas, tipo, peces));

    [Theory]
    [InlineData(EstadosEmbarque.Borrador, EstadosEmbarque.PreinvoiceEnviada, true)]
    [InlineData(EstadosEmbarque.Borrador, EstadosEmbarque.Facturado, false)]
    [InlineData(EstadosEmbarque.Facturado, EstadosEmbarque.Cancelado, false)]
    [InlineData(EstadosEmbarque.Empacado, EstadosEmbarque.Cancelado, true)]
    [InlineData(EstadosEmbarque.Facturado, EstadosEmbarque.Cerrado, true)]
    public void Transiciones_manuales(string actual, string nuevo, bool valida) =>
        Assert.Equal(valida, ReglasEmbarque.TransicionManualValida(actual, nuevo));

    [Fact]
    public void Validaciones_de_lineas_y_empaque()
    {
        var p = Guid.NewGuid();
        Assert.NotNull(ReglasEmbarque.ValidarLineas([]));
        Assert.NotNull(ReglasEmbarque.ValidarLineas([new(p, 10, 1m, 1m, null, null), new(p, 5, 1m, 0m, null, null)]));
        Assert.Null(ReglasEmbarque.ValidarLineas([new(p, 10, null, 0.5m, null, null)]));
        Assert.NotNull(ReglasEmbarque.ValidarEmpaque([new(1, p, null, null, 0, 10, 1m, null)]));
        Assert.Null(ReglasEmbarque.ValidarEmpaque([new(1, p, null, null, 2, 10, null, null)]));
    }
}
