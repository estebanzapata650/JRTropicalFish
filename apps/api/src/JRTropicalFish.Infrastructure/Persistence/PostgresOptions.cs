namespace JRTropicalFish.Infrastructure.Persistence;

/// <summary>Conexión a PostgreSQL (sección <c>Postgres</c>). En producción llega por variables de entorno
/// (<c>Postgres__Host</c>, <c>Postgres__Password</c>…).</summary>
public class PostgresOptions
{
    public const string SectionName = "Postgres";

    public required string Host { get; set; }
    public int Port { get; set; } = 5432;
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string Database { get; set; }
}
