using JRTropicalFish.Application.Abstractions;

namespace JRTropicalFish.Infrastructure.Comun;

public class RelojSistema(TimeProvider tiempo) : IReloj
{
    public DateTime AhoraUtc => tiempo.GetUtcNow().UtcDateTime;

    /// <summary>
    /// «Hoy» en la hora local del servidor. En el VPS el contenedor corre con <c>TZ=America/Bogota</c>
    /// (ver docker-compose.prod.yml): un vencimiento no puede cambiar de día a las 7 p. m.
    /// </summary>
    public DateOnly Hoy => DateOnly.FromDateTime(tiempo.GetLocalNow().DateTime);
}
