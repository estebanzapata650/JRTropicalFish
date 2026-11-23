using JRTropicalFish.Application.Exportaciones;

namespace JRTropicalFish.Tests.Exportaciones;

/// <summary>
/// Reglas AUNAP con casos del despacho 12186: lo declarado sale de lo real (misma especie, misma cantidad, mismo
/// valor) y una especie sin código AUNAP no se puede declarar.
/// </summary>
public class ReglasAunapTests
{
    private static readonly Guid Habrosus = Guid.NewGuid();
    private static readonly Guid Riesei = Guid.NewGuid();
    private static readonly Guid L103 = Guid.NewGuid();
    private static readonly Guid HabrosusGrande = Guid.NewGuid();
    private static readonly Guid AunapHabrosus = Guid.NewGuid();
    private static readonly Guid AunapRiesei = Guid.NewGuid();

    private static readonly Dictionary<Guid, ProductoAunapFila> Mapa = new()
    {
        [Habrosus] = new(Habrosus, "CO125", "Corydoras Habrosus", null, "Corydoras", true, AunapHabrosus, "217", "Corydoras habrosus", "Corredora habrosus", "Callichthyidae"),
        [HabrosusGrande] = new(HabrosusGrande, "CO126", "Corydoras Habrosus", null, "Corydoras", true, AunapHabrosus, "217", "Corydoras habrosus", "Corredora habrosus", "Callichthyidae"),
        [Riesei] = new(Riesei, "TE005", "Axelrodia Riesei", null, "Tetras", true, AunapRiesei, "50", "Axelrodia riesei", "Tetra rubí", "Characidae"),
        [L103] = new(L103, "LO375", "Peckoltia Sp. L103", null, "Loricáridos", true, null, null, null, null, null),
    };

    [Fact]
    public void Agrupa_por_especie_AUNAP_con_las_cantidades_y_valores_reales()
    {
        var (lineas, error) = ReglasAunap.Agrupar(
        [
            (Habrosus, "CO125", "Corydoras Habrosus", 1100, 770m),
            (HabrosusGrande, "CO126", "Corydoras Habrosus", 100, 90m),
            (Riesei, "TE005", "Axelrodia Riesei", 1600, 288m),
        ], Mapa);

        Assert.Null(error);
        Assert.Equal(2, lineas.Count);
        var habrosus = lineas.Single(l => l.Codigo == "217");
        Assert.Equal(1200, habrosus.Cantidad);
        Assert.Equal(860m, habrosus.Total);
        Assert.Equal(0.7167m, habrosus.PrecioPromedio);
        // Nada se pierde al agrupar: mismas unidades y mismo valor que la factura.
        Assert.Equal(2800, lineas.Sum(l => l.Cantidad));
        Assert.Equal(1148m, lineas.Sum(l => l.Total));
        Assert.Equal(1600, lineas.Single(l => l.Codigo == "50").Cantidad);
    }

    [Fact]
    public void Una_especie_sin_codigo_AUNAP_no_se_puede_declarar()
    {
        var (lineas, error) = ReglasAunap.Agrupar([(Riesei, "TE005", "Axelrodia Riesei", 1600, 288m), (L103, "LO375", "Peckoltia Sp. L103", 200, 140m)], Mapa);

        Assert.Empty(lineas);
        Assert.NotNull(error);
        Assert.Contains("LO375 Peckoltia Sp. L103", error);
    }

    [Fact]
    public void Resumen_compara_solicitado_autorizado_y_empacado()
    {
        var permiso = new PermisoAunapFila("AUNAP-123", new DateOnly(2026, 9, 10), null, DateTime.UtcNow);
        var resumen = ReglasAunap.Resumen(
            [(Habrosus, "CO125", "Corydoras Habrosus", 1100), (Riesei, "TE005", "Axelrodia Riesei", 1600), (L103, "LO375", "Peckoltia Sp. L103", 200)],
            [(Habrosus, 1100), (Riesei, 1600)],
            Mapa, permiso, new Dictionary<Guid, int> { [AunapHabrosus] = 1200, [AunapRiesei] = 800 });

        var habrosus = resumen.Filas.Single(f => f.Codigo == "217");
        Assert.Equal((1100, 1200, 1100, EstadosAunap.Ok), (habrosus.Solicitado, habrosus.Autorizado, habrosus.Empacado, habrosus.Estado));
        var riesei = resumen.Filas.Single(f => f.Codigo == "50");
        Assert.Equal(EstadosAunap.Excede, riesei.Estado);
        Assert.Single(resumen.SinCodigo);
        Assert.Equal("LO375", resumen.SinCodigo[0].ProductoCodigo);
        Assert.Equal(2, resumen.Avisos.Count);
        Assert.True(resumen.HayEmpaque);
    }

    [Fact]
    public void Sin_permiso_registrado_las_especies_quedan_pendientes()
    {
        var resumen = ReglasAunap.Resumen([(Riesei, "TE005", "Axelrodia Riesei", 1600)], [], Mapa, null, new Dictionary<Guid, int>());

        Assert.Equal(EstadosAunap.SinPermiso, resumen.Filas.Single().Estado);
        Assert.Null(resumen.Filas.Single().Autorizado);
        Assert.Empty(resumen.Avisos);
        Assert.False(resumen.HayEmpaque);
    }

    [Theory]
    [InlineData(true, null, 10, EstadosAunap.NoAutorizada)]
    [InlineData(true, 0, 10, EstadosAunap.NoAutorizada)]
    [InlineData(true, 10, 10, EstadosAunap.Ok)]
    [InlineData(true, 10, 11, EstadosAunap.Excede)]
    [InlineData(false, 10, 11, EstadosAunap.SinPermiso)]
    public void Estado_por_especie(bool hayPermiso, int? autorizado, int cantidad, string esperado) =>
        Assert.Equal(esperado, ReglasAunap.Estado(hayPermiso, autorizado, cantidad));

    [Fact]
    public void Valida_el_listado_a_importar()
    {
        EspecieAunapImportar Especie(string codigo, string nombre) => new(codigo, nombre, null, false, false, false, false, false, null, null);

        Assert.Null(ReglasAunap.ValidarImportacion([Especie("1", "Paratrygon aiereba"), Especie("1*", "Catoprion mento")]));
        Assert.NotNull(ReglasAunap.ValidarImportacion([]));
        Assert.NotNull(ReglasAunap.ValidarImportacion([Especie("1", "A"), Especie("1", "B")]));
        Assert.NotNull(ReglasAunap.ValidarImportacion([Especie("", "A")]));
    }
}
