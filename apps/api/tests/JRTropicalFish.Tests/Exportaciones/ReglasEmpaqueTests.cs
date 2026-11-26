using JRTropicalFish.Application.Exportaciones;

namespace JRTropicalFish.Tests.Exportaciones;

/// <summary>
/// Empaque: el estándar va por producto (sin estándar, el catálogo), los aditivos se dosifican por litro de agua y un
/// costo que no se ha cargado no se inventa: queda en «faltantes».
/// </summary>
public class ReglasEmpaqueTests
{
    private static readonly Guid Otocinclo = Guid.NewGuid();
    private static readonly Guid Oscar = Guid.NewGuid();
    private static readonly Guid BolsaCuarto = Guid.NewGuid();
    private static readonly Guid BolsaMedia = Guid.NewGuid();
    private static readonly Guid CajaEuropea = Guid.NewGuid();
    private static readonly Guid Stress = Guid.NewGuid();

    private static EstandarEmpaqueFila Estandar(Guid producto, bool tiene, Guid? bolsa = null, int? min = null, int? max = null,
        decimal? litros = null, string? aditivos = null, int? catalogoMin = null, int? catalogoMax = null) =>
        new(producto, "LO010", "Otocinclus vittatus", null, null, "Loricáridos", catalogoMin, catalogoMax, tiene, bolsa,
            bolsa == BolsaCuarto ? "1/4 Baja" : bolsa == BolsaMedia ? "1/2" : null, min, max, litros, null, false, aditivos, null);

    private static EmbarqueEmpaqueFila Fila(Guid producto, Guid? bolsa, int bolsas, int peces, int caja = 1) =>
        new(Guid.NewGuid(), caja, 1, producto, producto == Otocinclo ? "LO010" : "CC147", "x", null, null, bolsa,
            bolsa == BolsaCuarto ? "1/4 Baja" : bolsa == BolsaMedia ? "1/2" : null, null, null, bolsas, peces, 0.2m, 0, 0, 0, null);

    private static CostosEmpaque Costos(decimal? heat = 1470, decimal? oxigeno = null, decimal? litrosCuarto = 0.5m, decimal? costoStress = 120) =>
        new(new ParametrosEmpaqueFila(heat, oxigeno, null),
        [
            new(BolsaCuarto, "1/4 Baja", "11,5\"", 0.25m, 1550, litrosCuarto, true),
            new(BolsaMedia, "1/2", "18\"", 0.5m, 2440, null, true),
        ],
        [new(Stress, "STRESS", "Stress Guard", "ml", 5, 40, costoStress, "AQUAVET")]);

    private static readonly Dictionary<Guid, TipoCajaFila> Cajas = new()
    {
        [CajaEuropea] = new(CajaEuropea, "Caja Europea", "Box", 35706, 13, 10, 1, 1, true),
    };

    [Fact]
    public void Calcula_cajas_bolsas_heat_pack_y_aditivo_por_litro_de_agua()
    {
        var estandares = new Dictionary<Guid, EstandarEmpaqueFila>
        {
            [Otocinclo] = Estandar(Otocinclo, true, BolsaCuarto, 100, 125, aditivos: Stress.ToString()),
        };
        var c = ReglasEmpaque.Costo([Fila(Otocinclo, BolsaCuarto, 4, 125), Fila(Otocinclo, BolsaCuarto, 4, 125, caja: 2)],
            [new CajaCantidad(CajaEuropea, 2)], Cajas, estandares, Costos(oxigeno: 300));

        Assert.Equal(71412, c.Conceptos.Single(x => x.Concepto.StartsWith("Caja")).Total);
        Assert.Equal(12400, c.Conceptos.Single(x => x.Concepto.StartsWith("Bolsa")).Total);
        Assert.Equal(2940, c.Conceptos.Single(x => x.Concepto == "Heat pack").Total);
        Assert.Equal(2400, c.Conceptos.Single(x => x.Concepto == "Oxígeno").Total);
        // Stress Guard 5 mL / 40 L × 0,5 L × 8 bolsas = 0,5 mL × 120 COP.
        var stress = c.Conceptos.Single(x => x.Concepto == "Stress Guard");
        Assert.Equal((0.5m, 60m), (stress.Cantidad, stress.Total!.Value));
        Assert.Empty(c.Faltantes);
        Assert.Equal(1000, c.Peces);
        Assert.Equal(71412 + 12400 + 2940 + 2400 + 60, c.Total);
    }

    [Fact]
    public void Un_costo_que_falta_no_se_inventa_y_se_avisa()
    {
        var estandares = new Dictionary<Guid, EstandarEmpaqueFila>
        {
            [Otocinclo] = Estandar(Otocinclo, true, BolsaMedia, aditivos: Stress.ToString()),
        };
        var c = ReglasEmpaque.Costo([Fila(Otocinclo, BolsaMedia, 2, 100)], [new CajaCantidad(CajaEuropea, 1)], Cajas, estandares,
            Costos(oxigeno: null, costoStress: null));

        Assert.Null(c.Conceptos.Single(x => x.Concepto == "Oxígeno").Total);
        Assert.Contains("Costo del oxígeno por bolsa", c.Faltantes);
        Assert.Contains(c.Faltantes, f => f.StartsWith("Litros de agua de la bolsa 1/2"));
        Assert.DoesNotContain(c.Conceptos, x => x.Concepto == "Stress Guard");
    }

    [Fact]
    public void Los_litros_del_estandar_de_la_especie_mandan_sobre_los_de_la_bolsa()
    {
        var estandares = new Dictionary<Guid, EstandarEmpaqueFila>
        {
            [Otocinclo] = Estandar(Otocinclo, true, BolsaCuarto, litros: 2, aditivos: Stress.ToString()),
        };
        var c = ReglasEmpaque.Costo([Fila(Otocinclo, BolsaCuarto, 4, 125)], [], Cajas, estandares, Costos());

        // 5 mL / 40 L × 2 L × 4 bolsas = 1 mL.
        Assert.Equal(1m, c.Conceptos.Single(x => x.Concepto == "Stress Guard").Cantidad);
        Assert.Contains("Cajas reales del despacho (paso Factura)", c.Faltantes);
    }

    [Fact]
    public void Avisa_otro_tipo_de_bolsa_o_mas_peces_que_el_estandar_y_sin_estandar_usa_el_catalogo()
    {
        var estandares = new Dictionary<Guid, EstandarEmpaqueFila>
        {
            [Otocinclo] = Estandar(Otocinclo, true, BolsaCuarto, 100, 125),
            [Oscar] = Estandar(Oscar, false, catalogoMin: 20, catalogoMax: 30),
        };
        var avisos = ReglasEmpaque.FueraDeEstandar(
            [Fila(Otocinclo, BolsaMedia, 2, 150), Fila(Otocinclo, BolsaCuarto, 4, 110), Fila(Oscar, BolsaMedia, 2, 35)], estandares);

        Assert.Equal(3, avisos.Count);
        Assert.Contains(avisos, a => a.Texto.Contains("el estándar dice 1/4 Baja"));
        Assert.Contains(avisos, a => a.Texto.Contains("150 peces por bolsa; el estándar dice máximo 125"));
        Assert.Contains(avisos, a => a.Texto.Contains("35 peces por bolsa; el catálogo dice máximo 30"));
    }

    [Theory]
    [InlineData(10, 5, null, "mínimo")]
    [InlineData(null, null, 60.0, "litros")]
    [InlineData(0, null, null, "1 a 10.000")]
    public void Valida_el_estandar(int? min, int? max, double? litros, string error)
    {
        var r = new GuardarEstandarEmpaqueRequest(null, min, max, (decimal?)litros, null, false, []);
        Assert.Contains(error, ReglasEmpaque.Validar(r, new HashSet<Guid>(), new HashSet<Guid>()));
    }

    [Fact]
    public void Un_aditivo_que_no_se_dosifica_por_agua_no_se_acepta()
    {
        var r = new GuardarEstandarEmpaqueRequest(BolsaCuarto, 100, 125, null, null, true, [Guid.NewGuid()]);
        Assert.Contains("aditivo", ReglasEmpaque.Validar(r, new HashSet<Guid> { BolsaCuarto }, new HashSet<Guid> { Stress }));
    }
}
