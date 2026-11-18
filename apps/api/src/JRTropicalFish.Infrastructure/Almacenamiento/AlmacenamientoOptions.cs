namespace JRTropicalFish.Infrastructure.Almacenamiento;

/// <summary>Configuración del almacén de objetos (sección <c>Almacenamiento</c>).</summary>
public class AlmacenamientoOptions
{
    public const string SectionName = "Almacenamiento";

    /// <summary>URL del servicio S3. En producción se deja vacío para usar el endpoint de AWS.</summary>
    public string? Endpoint { get; set; }

    public string Region { get; set; } = "us-east-1";

    public string Bucket { get; set; } = "jr-documentos";

    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    /// <summary>Tamaño máximo por archivo. Se valida también en el navegador y en el controlador.</summary>
    public int TamanoMaximoMb { get; set; } = 10;
}
