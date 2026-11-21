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
                pagina.Size(informe.Horizontal ? PageSizes.A4.Landscape() : PageSizes.A4);
                pagina.Margin(1.5f, Unit.Centimetre);
                pagina.DefaultTextStyle(x => x.FontSize(9).FontColor(Tinta));

                pagina.Header().Element(e => Cabecera(e, informe));
                pagina.Content().PaddingVertical(12).Element(e => Cuerpo(e, informe));
                pagina.Footer().Element(e => Pie(e, informe));
            });
        }).GeneratePdf();

    private static void Cabecera(IContainer contenedor, InformeModelo informe) =>
        contenedor.PaddingBottom(8).BorderBottom(1).BorderColor(Borde).Row(fila =>
        {
            if (informe.Logo is { Length: > 0 } logo)
            {
                fila.ConstantItem(64).PaddingRight(10).Image(logo).FitArea();
            }

            fila.RelativeItem().Column(columna =>
            {
                if (informe.EmpresaDatos is { Count: > 0 } datos)
                {
                    // Documento comercial: la empresa en grande y sus datos debajo; el título a la derecha.
                    columna.Item().Text(informe.EmpresaNombre).FontSize(12).SemiBold();
                    foreach (var linea in datos)
                    {
                        columna.Item().Text(linea).FontSize(8).FontColor(TintaSuave);
                    }
                }
                else
                {
                    columna.Item().Text(informe.EmpresaNombre).FontSize(8).FontColor(TintaSuave);
                    columna.Item().PaddingTop(2).Text(informe.Titulo).FontSize(16).SemiBold();
                    if (!string.IsNullOrWhiteSpace(informe.Subtitulo))
                    {
                        columna.Item().Text(informe.Subtitulo).FontSize(10).FontColor(TintaSuave);
                    }
                }
            });

            if (informe.EmpresaDatos is { Count: > 0 })
            {
                fila.RelativeItem().AlignRight().AlignBottom().Column(columna =>
                {
                    columna.Item().AlignRight().Text(informe.Titulo).FontSize(16).SemiBold();
                    if (!string.IsNullOrWhiteSpace(informe.Subtitulo))
                    {
                        columna.Item().AlignRight().Text(informe.Subtitulo).FontSize(10).FontColor(TintaSuave);
                    }
                });
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
                    if (!string.IsNullOrWhiteSpace(seccion.Titulo))
                    {
                        bloque.Item().PaddingBottom(4).Text(seccion.Titulo).FontSize(11).SemiBold();
                    }

                    switch (seccion)
                    {
                        case SeccionCampos campos:
                            bloque.Item().Element(e => DibujarCampos(e, campos));
                            break;
                        case SeccionBloques bloques:
                            bloque.Item().Element(e => DibujarBloques(e, bloques));
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

    /// <summary>Bloques lado a lado, cada uno con sus pares etiqueta/valor en una columna.</summary>
    private static void DibujarBloques(IContainer contenedor, SeccionBloques seccion) =>
        contenedor.Row(fila =>
        {
            fila.Spacing(16);
            foreach (var b in seccion.Bloques)
            {
                fila.RelativeItem().Border(1).BorderColor(Borde).Padding(8).Column(columna =>
                {
                    columna.Spacing(2);
                    if (!string.IsNullOrWhiteSpace(b.Titulo))
                    {
                        columna.Item().PaddingBottom(2).Text(b.Titulo).FontSize(8).SemiBold().FontColor(TintaSuave);
                    }

                    foreach (var campo in b.Campos)
                    {
                        columna.Item().Row(celda =>
                        {
                            celda.ConstantItem(78).Text(campo.Etiqueta).FontColor(TintaSuave);
                            celda.RelativeItem().Text(string.IsNullOrWhiteSpace(campo.Valor) ? "—" : campo.Valor);
                        });
                    }
                });
            }
        });

    private static void DibujarTabla(IContainer contenedor, SeccionTabla seccion)
    {
        if (seccion.Filas.Count == 0 && seccion.Totales is not { Count: > 0 })
        {
            contenedor.Text("Sin registros.").FontColor(TintaSuave).Italic();
            return;
        }

        var tamano = seccion.Compacta ? 8f : 9f;
        var relleno = seccion.Compacta ? 3f : 5f;

        contenedor.DefaultTextStyle(x => x.FontSize(tamano)).Table(tabla =>
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
                for (var i = 0; i < seccion.Encabezados.Count; i++)
                {
                    Alinear(encabezado.Cell().Background(Fondo).Padding(relleno), seccion, i).Text(seccion.Encabezados[i]).SemiBold();
                }
            });

            foreach (var fila in seccion.Filas)
            {
                for (var i = 0; i < fila.Count; i++)
                {
                    Alinear(tabla.Cell().BorderBottom(1).BorderColor(Borde).Padding(relleno), seccion, i).Text(fila[i]);
                }
            }

            foreach (var fila in seccion.Totales ?? [])
            {
                for (var i = 0; i < fila.Count; i++)
                {
                    Alinear(tabla.Cell().Padding(relleno), seccion, i).Text(fila[i]).SemiBold();
                }
            }
        });
    }

    private static IContainer Alinear(IContainer celda, SeccionTabla seccion, int columna) =>
        (seccion.Alineaciones is { } a && columna < a.Count ? a[columna] : Alineacion.Izquierda) switch
        {
            Alineacion.Derecha => celda.AlignRight(),
            Alineacion.Centro => celda.AlignCenter(),
            _ => celda.AlignLeft(),
        };

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
                if (informe.Pie is { } pie)
                {
                    text.Span(pie);
                }
                else
                {
                    text.Span($"Generado por {informe.GeneradoPor} · ");
                    text.Span(informe.GeneradoEnUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                }
            });

            fila.ConstantItem(90).AlignRight().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(8).FontColor(TintaSuave));
                // Con pie propio (documentos al cliente, en inglés o español) la numeración va sin idioma.
                if (informe.Pie is null)
                {
                    text.Span("Página ");
                }

                text.CurrentPageNumber();
                text.Span(informe.Pie is null ? " de " : " / ");
                text.TotalPages();
            });
        });
}
