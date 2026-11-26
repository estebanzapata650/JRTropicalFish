using System.Text.Json;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

// Vuelca cada palabra del PDF con su caja: [pagina, x, y (desde arriba), ancho, alto, texto].
// Uso: dotnet run --project scripts/extraer-palabras-pdf -- "<archivo.pdf>" "<salida.json>"
if (args.Length < 2)
{
    Console.Error.WriteLine("Uso: extraer-palabras-pdf <archivo.pdf> <salida.json>");
    return 1;
}

using var documento = PdfDocument.Open(args[0]);
var palabras = new List<object[]>();
foreach (var pagina in documento.GetPages())
{
    foreach (var palabra in pagina.GetWords(NearestNeighbourWordExtractor.Instance))
    {
        var caja = palabra.BoundingBox;
        palabras.Add(
        [
            pagina.Number, Math.Round(caja.Left, 1), Math.Round(pagina.Height - caja.Top, 1), Math.Round(caja.Width, 1),
            Math.Round(caja.Height, 1), palabra.Text,
        ]);
    }
}

File.WriteAllText(args[1], JsonSerializer.Serialize(palabras));
Console.WriteLine($"{documento.NumberOfPages} páginas, {palabras.Count} palabras.");
return 0;
