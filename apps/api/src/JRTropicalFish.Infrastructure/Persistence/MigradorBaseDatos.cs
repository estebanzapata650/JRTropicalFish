using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JRTropicalFish.Infrastructure.Persistence;

/// <summary>
/// Aplica las migraciones pendientes.
///
/// <para>
/// En desarrollo se llama al arrancar la API. En producción <b>no</b>: es un paso explícito del
/// despliegue (<c>npm run prod:migrar</c>, que ejecuta la API con <c>--migrar</c> y sale). Así un
/// despliegue con una migración rota falla antes de reemplazar el contenedor que está sirviendo.
/// </para>
/// </summary>
public class MigradorBaseDatos(AppDbContext contexto, ILogger<MigradorBaseDatos> log)
{
    public async Task AplicarAsync(CancellationToken ct = default)
    {
        var pendientes = (await contexto.Database.GetPendingMigrationsAsync(ct)).ToList();
        if (pendientes.Count == 0)
        {
            log.LogInformation("Base de datos al día: no hay migraciones pendientes.");
            return;
        }

        log.LogInformation("Aplicando {Cantidad} migración(es): {Migraciones}", pendientes.Count, string.Join(", ", pendientes));
        await contexto.Database.MigrateAsync(ct);
        log.LogInformation("Migraciones aplicadas.");
    }
}
