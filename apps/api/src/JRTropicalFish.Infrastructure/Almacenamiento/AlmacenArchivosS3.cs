using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using JRTropicalFish.Application.Abstractions;

namespace JRTropicalFish.Infrastructure.Almacenamiento;

/// <summary>
/// Almacén de archivos sobre la API de S3.
///
/// Un único bucket, organizado por prefijos que arma <c>ClaveArchivo</c>. El navegador nunca habla
/// con el almacén: solo la API, que construye la clave y valida el tipo real del archivo.
/// </summary>
public class AlmacenArchivosS3(IAmazonS3 s3, IOptions<AlmacenamientoOptions> opciones) : IAlmacenArchivos
{
    private readonly AlmacenamientoOptions _opciones = opciones.Value;

    public async Task<ArchivoSubido> SubirAsync(
        string clave, Stream contenido, string tipoMime, CancellationToken ct = default)
    {
        // El hash se calcula sobre el contenido ya materializado: sirve de control de integridad y
        // permite detectar una re-subida idéntica sin volver a bajar el archivo.
        using var memoria = new MemoryStream();
        await contenido.CopyToAsync(memoria, ct);
        memoria.Position = 0;

        var hash = Convert.ToHexStringLower(await SHA256.HashDataAsync(memoria, ct));
        memoria.Position = 0;
        // El SDK cierra el InputStream al terminar de subirlo, así que el tamaño se toma antes.
        var tamano = memoria.Length;

        await s3.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _opciones.Bucket,
                Key = clave,
                InputStream = memoria,
                ContentType = tipoMime,
                // Los servidores S3 autoalojados (Garage, SeaweedFS) no aceptan la firma por trozos
                // "aws-chunked" que el SDK usa por defecto: rechazan la petición con
                // "Invalid payload signature". Enviando el cuerpo entero se firma de una sola vez.
                UseChunkEncoding = false,
            },
            ct);

        return new ArchivoSubido(clave, tamano, hash);
    }

    public async Task<ArchivoDescargado?> DescargarAsync(string clave, CancellationToken ct = default)
    {
        try
        {
            var respuesta = await s3.GetObjectAsync(_opciones.Bucket, clave, ct);
            return new ArchivoDescargado(
                respuesta.ResponseStream,
                respuesta.Headers.ContentType ?? "application/octet-stream",
                respuesta.ContentLength);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // El archivo puede faltar si se purgó del almacén pero la fila sigue en la base.
            return null;
        }
    }

    public async Task EliminarAsync(string clave, CancellationToken ct = default) =>
        await s3.DeleteObjectAsync(_opciones.Bucket, clave, ct);
}
