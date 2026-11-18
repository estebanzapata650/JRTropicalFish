namespace JRTropicalFish.Domain.Comun;

/// <summary>
/// Base de las entidades con identidad propia.
///
/// <para>
/// El Id es un UUID v7 (<see cref="Guid.CreateVersion7()"/>): ordenable por tiempo, así que los
/// índices de clave primaria no se fragmentan como con un UUID v4 aleatorio, y se puede generar en
/// la aplicación antes de tocar la base (útil para armar la clave del archivo antes del INSERT).
/// </para>
/// </summary>
public abstract class EntidadBase
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }
}
