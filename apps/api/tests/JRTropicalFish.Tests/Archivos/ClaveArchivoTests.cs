using JRTropicalFish.Application.Archivos;

namespace JRTropicalFish.Tests.Archivos;

public class ClaveArchivoTests
{
    private static readonly Guid EntidadId = Guid.Parse("0193a7c0-0000-7000-8000-000000000001");
    private static readonly Guid DocumentoId = Guid.Parse("0193a7c0-0000-7000-8000-000000000002");

    [Fact]
    public void La_clave_se_arma_con_entidad_ids_y_extension_del_tipo_real()
    {
        var clave = ClaveArchivo.ParaDocumento("importacion", EntidadId, DocumentoId, "application/pdf");

        Assert.Equal(
            "importacion/0193a7c0000070008000000000000001/0193a7c0000070008000000000000002.pdf",
            clave);
    }

    [Theory]
    [InlineData("../otra")]
    [InlineData("Importacion")]
    [InlineData("")]
    [InlineData("a/b")]
    public void Una_entidad_con_formato_raro_se_rechaza(string entidad)
    {
        Assert.Throws<ArgumentException>(() =>
            ClaveArchivo.ParaDocumento(entidad, EntidadId, DocumentoId, "application/pdf"));
    }

    [Fact]
    public void Un_tipo_no_permitido_se_rechaza()
    {
        Assert.Throws<ArgumentException>(() =>
            ClaveArchivo.ParaDocumento("especie", EntidadId, DocumentoId, "application/x-msdownload"));
    }
}
