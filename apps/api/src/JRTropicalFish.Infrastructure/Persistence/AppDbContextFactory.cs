using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JRTropicalFish.Infrastructure.Persistence;

/// <summary>
/// Factory que usan las herramientas de EF Core (<c>dotnet ef migrations add</c>) en tiempo de diseño.
/// Apunta a la base local de docker-compose; se puede sobrescribir con la variable
/// <c>JRTF_DESIGN_CONNECTION</c>. Generar una migración no necesita que la base esté levantada.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("JRTF_DESIGN_CONNECTION")
            ?? "Host=localhost;Port=5433;Database=jrtropicalfish;Username=jrtropicalfish;Password=jrtropicalfish";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();

        return new AppDbContext(optionsBuilder.Options);
    }
}
