using Microsoft.Extensions.Options;
using Npgsql;

namespace JRTropicalFish.Infrastructure.Persistence;

/// <summary>
/// Construye la cadena de conexión y abre conexiones Npgsql para Dapper. Una sola base de datos:
/// JR Tropical Fish es una sola empresa (ver docs/arquitectura/adr/0002-una-sola-base-de-datos.md).
/// </summary>
public class PostgresConnectionFactory(IOptions<PostgresOptions> options)
{
    public string ConnectionString { get; } = Construir(options.Value);

    public async Task<NpgsqlConnection> AbrirAsync(CancellationToken ct = default)
    {
        var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(ct);
        return connection;
    }

    public static string Construir(PostgresOptions o) =>
        new NpgsqlConnectionStringBuilder
        {
            Host = o.Host,
            Port = o.Port,
            Username = o.Username,
            Password = o.Password,
            Database = o.Database,
        }.ConnectionString;
}
