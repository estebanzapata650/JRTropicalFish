using Dapper;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Domain.Auditoria;
using JRTropicalFish.Infrastructure.Persistence;

namespace JRTropicalFish.Infrastructure.Repositories;

public class AuditoriaRepository(PostgresConnectionFactory conexiones) : IAuditoriaRepository
{
    public async Task RegistrarAsync(RegistroAuditoria registro, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);

        // En un CALL se castea TODO parámetro: un nulo (UsuarioId de un proceso automático) llega
        // como "unknown" y Postgres responde que el procedimiento no existe, no que el tipo esté mal.
        // Y el jsonb llega como text si no se castea.
        await connection.ExecuteAsync(new CommandDefinition(
            """
            CALL sp_auditoria_registrar(
                @Id::uuid, @Entidad::text, @EntidadId::uuid, @Accion::text, @CambiosJson::jsonb,
                @UsuarioId::uuid, @UsuarioNombre::text, @FechaUtc::timestamptz)
            """,
            new
            {
                registro.Id,
                registro.Entidad,
                registro.EntidadId,
                Accion = registro.Accion.ToString(),
                registro.CambiosJson,
                registro.UsuarioId,
                registro.UsuarioNombre,
                registro.FechaUtc,
            },
            cancellationToken: ct));
    }

    public async Task<IReadOnlyList<RegistroAuditoriaLectura>> ListarPorEntidadAsync(
        string entidad, Guid entidadId, int limite, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        var resultado = await connection.QueryAsync<RegistroAuditoriaLectura>(new CommandDefinition(
            "SELECT * FROM fn_auditoria_por_entidad(@Entidad, @EntidadId, @Limite)",
            new { Entidad = entidad, EntidadId = entidadId, Limite = limite },
            cancellationToken: ct));
        return resultado.AsList();
    }
}
