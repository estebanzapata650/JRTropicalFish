using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Domain.Seguridad;

namespace JRTropicalFish.Api.Startup;

public record SeedAdminOptions
{
    public const string SectionName = "SeedAdmin";

    public string? Nombre { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}

/// <summary>
/// Crea el primer usuario Administrador si todavía no existe uno con ese email. Solo se ejecuta en
/// desarrollo; en producción el primer usuario se crea con el procedimiento documentado en
/// docs/arquitectura/05-despliegue-vps.md.
/// </summary>
public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services, SeedAdminOptions options, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            return;
        }

        var usuarios = services.GetRequiredService<IUsuarioRepository>();
        if (await usuarios.BuscarPorEmailAsync(options.Email, ct) is not null)
        {
            return;
        }

        var perfilId = await services.GetRequiredService<IPerfilRepository>()
            .BuscarIdPorNombreAsync(PerfilesPlantilla.Administrador, ct)
            ?? throw new InvalidOperationException("No existe el perfil Administrador: ¿se aplicaron las migraciones?");

        var hasher = services.GetRequiredService<IPasswordHasher>();
        var reloj = services.GetRequiredService<IReloj>();

        await usuarios.CrearAsync(
            new Usuario
            {
                Nombre = options.Nombre ?? "Administrador",
                Email = options.Email,
                PasswordHash = hasher.Hash(options.Password),
                PerfilId = perfilId,
                Activo = true,
                CreadoEn = reloj.AhoraUtc,
            },
            ct);

        services.GetRequiredService<ILogger<SeedAdminOptions>>()
            .LogInformation("Usuario administrador de desarrollo creado: {Email}", options.Email);
    }
}
