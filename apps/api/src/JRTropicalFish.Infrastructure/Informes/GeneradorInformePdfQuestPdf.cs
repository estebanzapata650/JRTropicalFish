using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using JRTropicalFish.Application.Informes;

namespace JRTropicalFish.Infrastructure.Informes;

/// <summary>
/// Dibuja un <see cref="InformeModelo"/> con QuestPDF. **Es el único archivo del repositorio que
/// conoce esa librería**: para exportar una pantalla nueva se escribe su constructor de modelo, no se
/// vuelve a tocar esto.
///
/// El PDF usa su propia paleta neutra y no los tokens del tema: un documento impreso no tiene modo
/// oscuro, y un gris claro del tema quedaría ilegible en papel.
/// </summary>
public class GeneradorInformePdfQuestPdf : IGeneradorInformePdf
{
    private const string Tinta = "#111827";
    private const string TintaSuave = "#6b7280";
    private const string Borde = "#d1d5db";
    private const string Fondo = "#f3f4f6";

    public byte[] Generar(InformeModelo informe) =>
        Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(1.5f, Unit.Centimetre);
                pagina.DefaultTextStyle(x => x.FontSize(9).FontColor(Tinta));

                pagina.Header().Element(e => Cabecera(e, informe));
                pagina.Content().PaddingVertical(12).Element(e => Cuerpo(e, informe));
                pagina.Footer().Element(e => Pie(e, informe));
            });
        }).GeneratePdf();

    private static void Cabecera(IContainer contenedor, InformeModelo informe) =>
        contenedor.PaddingBottom(8).BorderBottom(1).BorderColor(Borde).Column(columna =>
        {
            columna.Item().Text(informe.EmpresaNombre).FontSize(8).FontColor(TintaSuave);
            columna.Item().PaddingTop(2).Text(informe.Titulo).FontSize(16).SemiBold();

            if (!string.IsNullOrWhiteSpace(informe.Subtitulo))
            {
                columna.Item().Text(informe.Subtitulo).FontSize(10).FontColor(TintaSuave);
            }
        });

    private static void Cuerpo(IContainer contenedor, InformeModelo informe) =>
        contenedor.Column(columna =>
        {
            columna.Spacing(14);

            foreach (var seccion in informe.Secciones)
            {
                columna.Item().Column(bloque =>
                {
                    bloque.Item().PaddingBottom(4).Text(seccion.Titulo).FontSize(11).SemiBold();

                    switch (seccion)
                    {
                        case SeccionCampos campos:
                            bloque.Item().Element(e => DibujarCampos(e, campos));
                            break;
                        case SeccionTabla tabla:
                            bloque.Item().Element(e => DibujarTabla(e, tabla));
                            break;
                        case SeccionTexto texto:
                            bloque.Item().Text(texto.Contenido).FontColor(Tinta);
                            break;
                        case SeccionFirmas firmas:
                            bloque.Item().Element(e => DibujarFirmas(e, firmas));
                            break;
                    }
                });
            }
        });

    /// <summary>Pares etiqueta/valor en dos columnas, para aprovechar el ancho de la hoja.</summary>
    private static void DibujarCampos(IContainer contenedor, SeccionCampos seccion) =>
        contenedor.Column(columna =>
        {
            columna.Spacing(3);

            foreach (var par in seccion.Campos.Chunk(2))
            {
                columna.Item().Row(fila =>
                {
                    foreach (var campo in par)
                    {
                        fila.RelativeItem().Row(celda =>
                        {
                            celda.ConstantItem(140).Text(campo.Etiqueta).FontColor(TintaSuave);
                            celda.RelativeItem().Text(string.IsNullOrWhiteSpace(campo.Valor) ? "—" : campo.Valor);
                        });
                    }

                    // Rellena la columna que falta cuando el número de campos es impar.
                    if (par.Length == 1)
                    {
                        fila.RelativeItem();
                    }
                });
            }
        });

    private static void DibujarTabla(IContainer contenedor, SeccionTabla seccion)
    {
        if (seccion.Filas.Count == 0)
        {
            contenedor.Text("Sin registros.").FontColor(TintaSuave).Italic();
            return;
        }

        contenedor.Table(tabla =>
        {
            tabla.ColumnsDefinition(columnas =>
            {
                for (var i = 0; i < seccion.Encabezados.Count; i++)
                {
                    columnas.RelativeColumn(seccion.PesosColumna is { } pesos && i < pesos.Count ? pesos[i] : 1);
                }
            });

            tabla.Header(encabezado =>
            {
                foreach (var titulo in seccion.Encabezados)
                {
                    encabezado.Cell().Background(Fondo).Padding(5).Text(titulo).SemiBold();
                }
            });

            foreach (var fila in seccion.Filas)
            {
                foreach (var celda in fila)
                {
                    tabla.Cell().BorderBottom(1).BorderColor(Borde).Padding(5).Text(celda);
                }
            }
        });
    }

    /// <summary>
    /// Casillas de firma, repartidas a lo ancho. La línea se dibuja con un borde superior sobre un
    /// espacio vacío: es lo que deja sitio para firmar a mano cuando el documento se imprime.
    /// </summary>
    private static void DibujarFirmas(IContainer contenedor, SeccionFirmas seccion) =>
        contenedor.PaddingTop(28).Row(fila =>
        {
            fila.Spacing(24);

            foreach (var casilla in seccion.Casillas)
            {
                fila.RelativeItem().Column(columna =>
                {
                    // Si ya se firmó electrónicamente, la constancia va donde iría la rúbrica.
                    if (!string.IsNullOrWhiteSpace(casilla.Firmado))
                    {
                        columna.Item().PaddingBottom(2).Text(casilla.Firmado)
                            .FontSize(8).FontColor(TintaSuave).Italic();
                    }

                    columna.Item().BorderTop(1).BorderColor(Tinta).PaddingTop(4)
                        .Text(casilla.Rotulo).FontSize(8).FontColor(TintaSuave);

                    if (!string.IsNullOrWhiteSpace(casilla.Nombre))
                    {
                        columna.Item().Text(casilla.Nombre).FontSize(9);
                    }
                });
            }
        });

    private static void Pie(IContainer contenedor, InformeModelo informe) =>
        contenedor.PaddingTop(6).BorderTop(1).BorderColor(Borde).Row(fila =>
        {
            fila.RelativeItem().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(8).FontColor(TintaSuave));
                text.Span($"Generado por {informe.GeneradoPor} · ");
                text.Span(informe.GeneradoEnUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
            });

            fila.ConstantItem(90).AlignRight().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(8).FontColor(TintaSuave));
                text.Span("Página ");
                text.CurrentPageNumber();
                text.Span(" de ");
                text.TotalPages();
            });
        });
}
