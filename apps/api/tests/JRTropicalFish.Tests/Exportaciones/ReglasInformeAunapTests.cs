using JRTropicalFish.Application.Exportaciones;

namespace JRTropicalFish.Tests.Exportaciones;

/// <summary>
/// Informe mensual AUNAP: las unidades son las que salieron de bodega, una fila por especie × municipio de origen,
/// el criadero no se reporta y lo que falta (VUCE, DEX, factura DIAN, código, origen) se avisa.
/// </summary>
public class ReglasInformeAunapTests
{
    private static readonly Guid Embarque = Guid.NewGuid();
    private static readonly Guid Otocinclo = Guid.NewGuid();
    private static readonly Guid Altum = Guid.NewGuid();

    private static DespachoAunapFila Despacho(
        string producto, Guid? especie, string? codigo, string? centro, string? municipio, int unidades,
        int? autorizado = 5000, bool excluir = false, string? vuce = "TM-00017927-20260918", string? dex = "6027776268234",
        string? factura = "JRE437", int empacado = 0, string embarque = "EXP-001", Guid? embarqueId = null) =>
        new(embarqueId ?? Embarque, embarque, new DateOnly(2026, 10, 4), "Jakarta", "Indonesia", vuce, new DateOnly(2026, 9, 18), dex, factura,
            null, null, empacado, producto, especie, codigo, codigo is null ? null : "LORICARIIDAE", codigo is null ? null : $"Especie {codigo}",
            null, centro, municipio is null ? null : "Meta", municipio, municipio is null ? null : "Orinoco", excluir, autorizado, unidades);

    [Fact]
    public void Suma_por_especie_y_municipio_lo_que_salio()
    {
        var (filas, avisos, _) = ReglasInformeAunap.Armar(
        [
            Despacho("LO100", Otocinclo, "247", "Granada", "Granada", 1000),
            Despacho("LO101", Otocinclo, "247", "Granada", "Granada", 800),
            Despacho("LO100", Otocinclo, "247", "Villavicencio", "Villavicencio", 200),
            Despacho("CI500", Altum, "500", "Carreño", "Puerto Carreño", 240),
        ]);

        Assert.Empty(avisos);
        Assert.Collection(
            filas,
            f => Assert.Equal(("247", "Granada", 1800), (f.Codigo, f.Municipio, f.Unidades)),
            f => Assert.Equal(("247", "Villavicencio", 200), (f.Codigo, f.Municipio, f.Unidades)),
            f => Assert.Equal(("500", "Puerto Carreño", 240), (f.Codigo, f.Municipio, f.Unidades)));
        Assert.All(filas, f => Assert.Equal(("TM-00017927-20260918", "6027776268234", "JRE437"), (f.VuceNumero, f.DexNumero, f.FacturaDian)));
    }

    [Fact]
    public void El_criadero_no_se_reporta()
    {
        var (filas, avisos, excluidos) = ReglasInformeAunap.Armar(
        [
            Despacho("CI500", Altum, "500", "Carreño", "Puerto Carreño", 240),
            Despacho("CC145", null, null, "Medellin", null, 300, excluir: true),
        ]);

        Assert.Single(filas);
        Assert.Equal(300, excluidos);
        Assert.Empty(avisos);
    }

    [Fact]
    public void Sin_codigo_o_sin_origen_avisa_y_sin_codigo_no_se_declara()
    {
        var (filas, avisos, _) = ReglasInformeAunap.Armar(
        [
            Despacho("LO375", null, null, "Inirida", "Inírida", 200),
            Despacho("CI500", Altum, "500", "Bogota", null, 50),
        ]);

        var fila = Assert.Single(filas);
        Assert.Null(fila.Municipio);
        Assert.Contains(avisos, a => a.Texto.Contains("LO375 no tiene código AUNAP"));
        Assert.Contains(avisos, a => a.Texto.Contains("Bogota no tiene municipio AUNAP"));
    }

    [Fact]
    public void Avisa_tramites_faltantes_lo_no_aprobado_y_la_diferencia_con_el_empaque()
    {
        var (_, avisos, _) = ReglasInformeAunap.Armar(
        [
            Despacho("LO100", Otocinclo, "247", "Granada", "Granada", 1800, autorizado: 1500, vuce: null, dex: null, factura: null, empacado: 2000),
            Despacho("CI500", Altum, "500", "Carreño", "Puerto Carreño", 240, autorizado: null, vuce: null, dex: null, factura: null, empacado: 2000),
        ]);

        var textos = avisos.Select(a => a.Texto).ToList();
        Assert.Contains(textos, t => t.StartsWith("Falta el n.º de trámite VUCE"));
        Assert.Contains("Falta el n.º del DEX.", textos);
        Assert.Contains("Falta el n.º de la factura DIAN.", textos);
        Assert.Contains(textos, t => t.StartsWith("247") && t.Contains("salieron 1.800") && t.Contains("aprobó 1.500"));
        Assert.Contains(textos, t => t.StartsWith("500") && t.Contains("no figura en la VUCE"));
        Assert.Contains(textos, t => t.Contains("despachó 2.040") && t.Contains("registra 2.000"));
    }

    [Fact]
    public void Ordena_por_codigo_numerico_dentro_de_cada_exportacion()
    {
        var especie9 = Guid.NewGuid();
        var (filas, _, _) = ReglasInformeAunap.Armar(
        [
            Despacho("A", Altum, "500", "Carreño", "Puerto Carreño", 10),
            Despacho("B", especie9, "9", "Carreño", "Puerto Carreño", 10),
            Despacho("C", Otocinclo, "24*", "Carreño", "Puerto Carreño", 10),
        ]);

        Assert.Equal(["9", "24*", "500"], filas.Select(f => f.Codigo));
    }

    [Fact]
    public void El_CITES_solo_se_pide_cuando_van_rayas()
    {
        var raya = Guid.NewGuid();
        var sinRayas = ReglasInformeAunap.Armar([Despacho("CI500", Altum, "500", "Carreño", "Puerto Carreño", 240)]).Avisos;
        Assert.DoesNotContain(sinRayas, a => a.Texto.Contains("CITES"));

        var conRaya = Despacho("PT040", raya, "516", "Carreño", "Puerto Carreño", 10) with { NombreCientifico = "Potamotrygon motoro" };
        Assert.Contains(ReglasInformeAunap.Armar([conRaya]).Avisos, a => a.Texto == "Lleva rayas y falta el n.º de CITES.");
        Assert.DoesNotContain(ReglasInformeAunap.Armar([conRaya with { CitesNumero = "CO-123" }]).Avisos, a => a.Texto.Contains("CITES"));

        Assert.True(ReglasInformeAunap.RequiereCites("Paratrygon aiereba"));
        Assert.False(ReglasInformeAunap.RequiereCites("Catathyridium jenynsii"));
    }

    [Theory]
    [InlineData(2026, 10, "2026-10-01", "2026-10-31", "Octubre 2026")]
    [InlineData(2026, 2, "2026-02-01", "2026-02-28", "Febrero 2026")]
    public void Rango_y_nombre_del_mes(int anio, int mes, string desde, string hasta, string nombre)
    {
        Assert.Equal((DateOnly.Parse(desde), DateOnly.Parse(hasta)), ReglasInformeAunap.Rango(anio, mes));
        Assert.Equal(nombre, ReglasInformeAunap.NombreMes(anio, mes));
    }
}
