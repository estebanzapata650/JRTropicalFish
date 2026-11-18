namespace JRTropicalFish.Application.Abstractions;

public record ArchivoSubido(string Clave, long TamanoBytes, string HashSha256);

public record ArchivoDescargado(Stream Contenido, string TipoMime, long TamanoBytes);

/// <summary>
/// Almacén de archivos. La implementación habla S3, así que sirve igual contra Garage en desarrollo
/// y contra S3 o R2 en producción sin cambiar una línea de la lógica de negocio.
/// </summary>
public interface IAlmacenArchivos
{
    Task<ArchivoSubido> SubirAsync(
        string clave, Stream contenido, string tipoMime, CancellationToken ct = default);

    Task<ArchivoDescargado?> DescargarAsync(string clave, CancellationToken ct = default);

    Task EliminarAsync(string clave, CancellationToken ct = default);
}
