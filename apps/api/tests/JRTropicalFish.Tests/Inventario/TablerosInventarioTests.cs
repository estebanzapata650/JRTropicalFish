using JRTropicalFish.Application.Inventario;

namespace JRTropicalFish.Tests.Inventario;

public class TablerosInventarioTests
{
    private static LoteFila Lote(DateOnly ingreso, int recibidos, int muertosLlegada, int muertos) =>
        new(Guid.NewGuid(), "LOT-0001", Guid.NewGuid(), "AP005", "x", null, null, null, null, null, null,
            ingreso, 0, null, recibidos, muertosLlegada, "recepcion", 0, 0, 0, 0, muertos, DateTime.UtcNow);

    [Theory]
    [InlineData("2026-11-16", "2026-11-16")] // lunes
    [InlineData("2026-11-21", "2026-11-16")] // sábado
    [InlineData("2026-11-22", "2026-11-16")] // domingo: la semana va de lunes a domingo
    [InlineData("2026-11-23", "2026-11-23")]
    public void La_semana_arranca_el_lunes(string fecha, string lunes) =>
        Assert.Equal(DateOnly.Parse(lunes), TablerosInventarioService.Lunes(DateOnly.Parse(fecha)));

    [Fact]
    public void Las_semanas_vacias_del_periodo_quedan_en_cero()
    {
        var semanas = TablerosInventarioService.Semanas(
            new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 21),
            [Lote(new DateOnly(2026, 11, 3), 100, 2, 5), Lote(new DateOnly(2026, 11, 4), 50, 0, 0), Lote(new DateOnly(2026, 11, 18), 20, 1, 1)]);

        // 1 de nov es domingo: su semana arranca el lunes 26 de oct.
        Assert.Equal(
            [new DateOnly(2026, 10, 26), new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 9), new DateOnly(2026, 11, 16)],
            semanas.Select(s => s.Semana));
        Assert.Equal([0, 150, 0, 20], semanas.Select(s => s.Recibidos));
        Assert.Equal(3.3m, semanas[1].PorcentajeMuertos);
        Assert.Equal(0m, semanas[2].PorcentajeMuertos);
    }

    [Fact]
    public void El_resumen_de_proveedor_separa_muertos_al_llegar_y_despues()
    {
        var p = new ProveedorResumen(null, "Sin proveedor", 0, 0, 0, Lotes: 2, Recibidos: 200, MuertosLlegada: 4, MuertosDespues: 6);

        Assert.Equal(5m, p.PorcentajeMuertos);
        Assert.Equal(0m, p.Cumplimiento); // sin pedidos en el periodo (p. ej. saldo inicial)
    }

    [Fact]
    public void Ocupacion_y_acuarios_vacios()
    {
        var t = new TableroBodega(0, 0, 0, AcuariosActivos: 545, AcuariosOcupados: 271, AcuariosConEnfermos: 0, 0, 0, [], [], []);

        Assert.Equal(274, t.AcuariosVacios);
        Assert.Equal(49.7m, t.Ocupacion);
    }
}
