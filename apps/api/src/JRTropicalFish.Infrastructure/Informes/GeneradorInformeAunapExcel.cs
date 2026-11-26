using ClosedXML.Excel;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Exportaciones;

namespace JRTropicalFish.Infrastructure.Informes;

/// <summary>
/// «Formato Informe Mensual» de la AUNAP (DTIV) con las mismas celdas que el libro oficial: encabezado del permisionario
/// (B3:G11), encabezados en las filas 15–16 y datos desde la fila 17, columnas B…S. ClosedXML (MIT) solo vive aquí.
/// </summary>
public sealed class GeneradorInformeAunapExcel : IGeneradorInformeAunapExcel
{
    private const int FilaDatos = 17;

    public byte[] Generar(InformeAunap informe)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add(NombreHoja(informe));
        var p = informe.Parametros;

        var titulo = hoja.Range("D1:L2").Merge();
        titulo.Value = "AUTORIDAD NACIONAL DE ACUICULTURA Y PESCA - AUNAP\nDirección Técnica de Inspección y Vigilancia - DTIV\nFormato Informe Mensual";
        titulo.Style.Alignment.WrapText = true;
        titulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        titulo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        titulo.Style.Font.Bold = true;
        hoja.Row(1).Height = 24;
        hoja.Row(2).Height = 24;

        void Dato(int fila, string etiqueta, string? valor, string hasta = "G")
        {
            hoja.Range($"B{fila}:C{fila}").Merge().Value = etiqueta;
            hoja.Cell($"B{fila}").Style.Font.Bold = true;
            hoja.Range($"D{fila}:{hasta}{fila}").Merge().Value = valor ?? "";
        }

        Dato(3, "RAZÓN SOCIAL EMPRESA", informe.RazonSocial, "L");
        Dato(5, "NOMBRE REPRESENTANTE LEGAL", p.RepresentanteLegal);
        Dato(6, "No. RESOLUCIÓN", p.Resolucion);
        hoja.Range("B7:C7").Merge().Value = "VIGENCIA";
        hoja.Cell("B7").Style.Font.Bold = true;
        Fecha(hoja.Cell("D7"), p.VigenciaDesde);
        hoja.Cell("F7").Value = "a";
        Fecha(hoja.Cell("G7"), p.VigenciaHasta);
        Dato(8, "DIRECCIÓN ACTUAL", p.Direccion);
        Dato(9, "CORREO ELECTRÓNICO", p.Correo);
        Dato(10, "TELÉFONO/CELULAR", p.Telefono);
        hoja.Cell("B11").Value = "MES";
        hoja.Cell("B11").Style.Font.Bold = true;
        hoja.Range("C11:D11").Merge().Value = ReglasInformeAunap.NombreMes(informe.Anio, informe.Mes);

        Encabezado(hoja, "B15:B16", "CÓDIGO\nAUNAP");
        Encabezado(hoja, "C15:C16", "FAMILIA");
        Encabezado(hoja, "D15:E16", "NOMBRE CIENTÍFICO");
        Encabezado(hoja, "F15:F16", "NOMBRE COMÚN");
        Encabezado(hoja, "G15:I15", "ORIGEN");
        Encabezado(hoja, "G16", "DEPARTAMENTO");
        Encabezado(hoja, "H16", "MUNICIPIO");
        Encabezado(hoja, "I16", "CUENCA");
        Encabezado(hoja, "J15:J16", "UNIDADES");
        Encabezado(hoja, "K15:L15", "DESTINO");
        Encabezado(hoja, "K16", "CIUDAD");
        Encabezado(hoja, "L16", "PAÍS");
        Encabezado(hoja, "M15:M16", "FECHA DE\nEXPORTACIÓN");
        Encabezado(hoja, "N15:N16", "N° DOCUMENTO DEX DIAN");
        Encabezado(hoja, "O15:P15", "VUCE");
        Encabezado(hoja, "O16", "No TRAMITE - (TM)");
        Encabezado(hoja, "P16", "FECHA APROBACIÓN");
        Encabezado(hoja, "Q15:Q16", "No FACTURA");
        Encabezado(hoja, "R15:S15", "CITES");
        Encabezado(hoja, "R16", "No CITES");
        Encabezado(hoja, "S16", "FECHA APROBACIÓN");

        var fila = FilaDatos;
        foreach (var f in informe.Filas)
        {
            Texto(hoja.Cell(fila, "B"), f.Codigo);
            hoja.Cell(fila, "C").Value = f.Familia ?? "";
            var cientifico = hoja.Range($"D{fila}:E{fila}").Merge();
            cientifico.Value = f.NombreCientifico;
            cientifico.Style.Font.Italic = true;
            hoja.Cell(fila, "F").Value = f.NombreComun ?? "";
            hoja.Cell(fila, "G").Value = f.Departamento ?? "";
            hoja.Cell(fila, "H").Value = f.Municipio ?? "";
            hoja.Cell(fila, "I").Value = f.Cuenca ?? "";
            hoja.Cell(fila, "J").Value = f.Unidades;
            hoja.Cell(fila, "K").Value = f.Ciudad?.ToUpperInvariant() ?? "";
            hoja.Cell(fila, "L").Value = f.Pais?.ToUpperInvariant() ?? "";
            Fecha(hoja.Cell(fila, "M"), f.FechaExportacion);
            Texto(hoja.Cell(fila, "N"), f.DexNumero);
            Texto(hoja.Cell(fila, "O"), f.VuceNumero);
            Fecha(hoja.Cell(fila, "P"), f.VuceFecha);
            Texto(hoja.Cell(fila, "Q"), f.FacturaDian);
            Texto(hoja.Cell(fila, "R"), f.CitesNumero);
            Fecha(hoja.Cell(fila, "S"), f.CitesFecha);
            fila++;
        }

        if (informe.Filas.Count > 0)
        {
            var datos = hoja.Range($"B{FilaDatos}:S{fila - 1}");
            datos.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            datos.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            datos.Style.Font.FontSize = 10;
            hoja.Cell(fila, "I").Value = "TOTAL";
            hoja.Cell(fila, "I").Style.Font.Bold = true;
            hoja.Cell(fila, "J").FormulaA1 = $"SUM(J{FilaDatos}:J{fila - 1})";
            hoja.Cell(fila, "J").Style.Font.Bold = true;
        }

        hoja.Column("J").Style.NumberFormat.Format = "#,##0";
        foreach (var (columna, ancho) in new[]
                 {
                     ("A", 2.0), ("B", 9.0), ("C", 24.0), ("D", 26.0), ("E", 4.0), ("F", 26.0), ("G", 16.0), ("H", 18.0), ("I", 13.0),
                     ("J", 10.0), ("K", 14.0), ("L", 14.0), ("M", 13.0), ("N", 18.0), ("O", 24.0), ("P", 13.0), ("Q", 12.0), ("R", 14.0),
                     ("S", 13.0),
                 })
        {
            hoja.Column(columna).Width = ancho;
        }

        hoja.SheetView.FreezeRows(16);
        using var salida = new MemoryStream();
        libro.SaveAs(salida);
        return salida.ToArray();
    }

    /// <summary>"OCT-2026", como las hojas del libro.</summary>
    private static string NombreHoja(InformeAunap informe)
    {
        string[] meses = ["ENE", "FEB", "MAR", "ABR", "MAY", "JUN", "JUL", "AGO", "SEP", "OCT", "NOV", "DIC"];
        return $"{meses[informe.Mes - 1]}-{informe.Anio}";
    }

    private static void Encabezado(IXLWorksheet hoja, string rango, string texto)
    {
        var r = hoja.Range(rango);
        if (r.Cells().Count() > 1)
        {
            r.Merge();
        }

        r.FirstCell().Value = texto;
        r.Style.Font.Bold = true;
        r.Style.Font.FontSize = 10;
        r.Style.Alignment.WrapText = true;
        r.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        r.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        r.Style.Fill.BackgroundColor = XLColor.FromHtml("#DDEBF7");
        r.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    }

    /// <summary>Números largos (DEX, TM) como texto: Excel los volvería notación científica.</summary>
    private static void Texto(IXLCell celda, string? valor)
    {
        celda.Value = valor ?? "";
        celda.Style.NumberFormat.Format = "@";
    }

    private static void Fecha(IXLCell celda, DateOnly? fecha)
    {
        if (fecha is not { } f)
        {
            return;
        }

        celda.Value = f.ToDateTime(TimeOnly.MinValue);
        celda.Style.DateFormat.Format = "dd/mm/yyyy";
        celda.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }
}
