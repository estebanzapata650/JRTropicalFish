using System.Text;
using JRTropicalFish.Application.Archivos;

namespace JRTropicalFish.Tests.Archivos;

public class DetectorTipoArchivoTests
{
    [Fact]
    public void Detecta_un_pdf_por_su_firma()
    {
        Assert.Equal("application/pdf", DetectorTipoArchivo.Detectar("%PDF-1.7 resto"u8));
    }

    [Fact]
    public void Detecta_un_png_por_su_firma()
    {
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];
        Assert.Equal("image/png", DetectorTipoArchivo.Detectar(png));
    }

    [Fact]
    public void Un_ejecutable_renombrado_no_pasa()
    {
        Assert.Null(DetectorTipoArchivo.Detectar(Encoding.ASCII.GetBytes("MZ\x90\0\x03\0\0\0\x04\0\0\0")));
    }

    [Fact]
    public void El_nombre_se_sanea_para_el_encabezado_de_descarga()
    {
        Assert.Equal("factura.pdf", DetectorTipoArchivo.SanearNombre("../../etc/factura.pdf"));
        Assert.Equal("documento", DetectorTipoArchivo.SanearNombre("   "));
    }
}
