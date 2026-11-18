using System.Text.RegularExpressions;

namespace JRTropicalFish.Application.Archivos;

/// <summary>
/// Único lugar donde se arma la clave (ruta) de un archivo en el almacén S3.
///
/// <para>
/// Ningún fragmento de la clave viene del usuario: ni el nombre original del archivo ni el tipo
/// MIME que declara el navegador. El nombre original se guarda en la base y solo se usa para el
/// encabezado de descarga. Así nadie puede escribir fuera de su carpeta con un <c>../</c>.
/// </para>
///
/// Formato: <c>{entidad}/{entidadId}/{documentoId}{extension}</c>, por ejemplo
/// <c>importacion/0193.../0193....pdf</c>.
/// </summary>
public static partial class ClaveArchivo
{
    /// <summary>Extensiones permitidas, derivadas del tipo real detectado por los magic bytes.</summary>
    public static readonly IReadOnlyDictionary<string, string> ExtensionPorMime =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["application/pdf"] = ".pdf",
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
        };

    public static string ParaDocumento(string entidad, Guid entidadId, Guid documentoId, string tipoMime)
    {
        // La entidad es una constante del código ("importacion", "especie"...), pero se valida igual:
        // es la carpeta raíz y un valor raro rompería la organización del bucket.
        if (!EntidadValida().IsMatch(entidad))
        {
            throw new ArgumentException($"Entidad no válida para una clave de archivo: {entidad}", nameof(entidad));
        }

        if (!ExtensionPorMime.TryGetValue(tipoMime, out var extension))
        {
            throw new ArgumentException($"Tipo de archivo no permitido: {tipoMime}", nameof(tipoMime));
        }

        return $"{entidad}/{entidadId:N}/{documentoId:N}{extension}";
    }

    [GeneratedRegex("^[a-z][a-z0-9-]{1,49}$")]
    private static partial Regex EntidadValida();
}
