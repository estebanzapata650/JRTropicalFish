namespace JRTropicalFish.Application.Archivos;

/// <summary>
/// Detecta el tipo real de un archivo por su firma binaria.
///
/// No se confía en el <c>Content-Type</c> que declara el navegador ni en la extensión: los dos los
/// controla quien sube el archivo. Un ejecutable renombrado a .pdf pasaría cualquier validación que
/// mire solo el nombre.
/// </summary>
public static class DetectorTipoArchivo
{
    /// <summary>Bytes que hay que leer para poder decidir.</summary>
    public const int BytesNecesarios = 12;

    private static readonly byte[] Pdf = "%PDF-"u8.ToArray();
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] Riff = "RIFF"u8.ToArray();
    private static readonly byte[] Webp = "WEBP"u8.ToArray();

    /// <summary>Tipo MIME real, o <c>null</c> si no es uno de los formatos permitidos.</summary>
    public static string? Detectar(ReadOnlySpan<byte> inicio)
    {
        if (Empieza(inicio, Pdf)) return "application/pdf";
        if (Empieza(inicio, Jpeg)) return "image/jpeg";
        if (Empieza(inicio, Png)) return "image/png";

        // WEBP es un contenedor RIFF: "RIFF" + 4 bytes de tamaño + "WEBP".
        if (Empieza(inicio, Riff) && inicio.Length >= 12 && inicio[8..12].SequenceEqual(Webp))
        {
            return "image/webp";
        }

        return null;
    }

    private static bool Empieza(ReadOnlySpan<byte> datos, ReadOnlySpan<byte> firma) =>
        datos.Length >= firma.Length && datos[..firma.Length].SequenceEqual(firma);

    /// <summary>
    /// Deja el nombre en algo seguro para el encabezado de descarga: sin rutas, sin saltos de línea
    /// y acotado en longitud.
    /// </summary>
    public static string SanearNombre(string? nombre)
    {
        var soloArchivo = Path.GetFileName(nombre ?? string.Empty).Trim();
        var limpio = new string(soloArchivo.Where(c => !char.IsControl(c) && c != '"').ToArray());
        if (string.IsNullOrWhiteSpace(limpio))
        {
            return "documento";
        }

        return limpio.Length > 200 ? limpio[^200..] : limpio;
    }
}
