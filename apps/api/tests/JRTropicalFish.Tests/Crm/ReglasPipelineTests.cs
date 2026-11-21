using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Crm;
using JRTropicalFish.Domain.Crm;

namespace JRTropicalFish.Tests.Crm;

public class ReglasPipelineTests
{
    [Theory]
    [InlineData(EtapasPipeline.Prospeccion, EtapasPipeline.Stocklist)]
    [InlineData(EtapasPipeline.Prospeccion, EtapasPipeline.AprobacionVuelo)]
    [InlineData(EtapasPipeline.PreInvoice, EtapasPipeline.AprobacionVuelo)]
    public void Se_puede_avanzar_y_saltar_etapas(string actual, string nueva) =>
        Assert.Null(ReglasPipeline.ValidarCambio(actual, nueva, null));

    [Theory]
    [InlineData(EtapasPipeline.Stocklist, EtapasPipeline.Prospeccion)]
    [InlineData(EtapasPipeline.AprobacionVuelo, EtapasPipeline.PreInvoice)]
    public void No_se_puede_retroceder(string actual, string nueva) =>
        Assert.Equal(TipoError.Validacion, ReglasPipeline.ValidarCambio(actual, nueva, null)!.Tipo);

    [Theory]
    [InlineData(EtapasPipeline.Ganada)]
    [InlineData(EtapasPipeline.Perdida)]
    public void Una_oportunidad_cerrada_no_cambia_de_etapa(string actual)
    {
        Assert.Equal(TipoError.Conflicto, ReglasPipeline.ValidarCambio(actual, EtapasPipeline.Perdida, "Sin stock")!.Tipo);
        Assert.Empty(ReglasPipeline.Siguientes(actual));
        Assert.NotNull(ReglasPipeline.ValidarGanar(actual));
    }

    [Fact]
    public void Perdida_exige_motivo()
    {
        Assert.NotNull(ReglasPipeline.ValidarCambio(EtapasPipeline.Stocklist, EtapasPipeline.Perdida, null));
        Assert.NotNull(ReglasPipeline.ValidarCambio(EtapasPipeline.Stocklist, EtapasPipeline.Perdida, "  "));
        Assert.Null(ReglasPipeline.ValidarCambio(EtapasPipeline.Stocklist, EtapasPipeline.Perdida, "Flete costoso"));
    }

    [Fact]
    public void Ganada_solo_se_alcanza_con_ganar() =>
        Assert.NotNull(ReglasPipeline.ValidarCambio(EtapasPipeline.AprobacionVuelo, EtapasPipeline.Ganada, null));

    [Fact]
    public void Las_siguientes_son_las_posteriores_mas_perdida() =>
        Assert.Equal(
            [EtapasPipeline.AprobacionVuelo, EtapasPipeline.Ganada, EtapasPipeline.Perdida],
            ReglasPipeline.Siguientes(EtapasPipeline.PreInvoice));
}
