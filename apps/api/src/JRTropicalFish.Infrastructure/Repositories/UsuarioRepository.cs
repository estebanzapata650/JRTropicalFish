using Dapper;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Domain.Seguridad;
using JRTropicalFish.Infrastructure.Persistence;

namespace JRTropicalFish.Infrastructure.Repositories;

/// <summary>
/// Patrón de todos los repositorios: Dapper llamando funciones (<c>SELECT * FROM fn_x(...)</c>) y
/// procedimientos (<c>CALL sp_x(...)</c>) PL/pgSQL creados por migración. Nada de SQL de negocio
/// suelto en C#: la lógica de consulta vive versionada en las migraciones.
/// </summary>
public class UsuarioRepository(PostgresConnectionFactory conexiones) : IUsuarioRepository
{
    public async Task<UsuarioConPerfil?> BuscarPorEmailAsync(string email, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<UsuarioConPerfil>(new CommandDefinition(
            "SELECT * FROM fn_usuario_por_email(@Email)", new { Email = email }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<string>> ObtenerPermisosAsync(Guid usuarioId, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        var resultado = await connection.QueryAsync<string>(new CommandDefinition(
            "SELECT codigo FROM fn_usuario_permisos(@Id)", new { Id = usuarioId }, cancellationToken: ct));
        return resultado.AsList();
    }

    public async Task CrearAsync(Usuario usuario, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(
            """
            CALL sp_usuario_crear(@Id::uuid, @Nombre::text, @Email::text, @PasswordHash::text,
                                  @PerfilId::uuid, @Activo::boolean, @CreadoEn::timestamptz)
            """,
            new
            {
                usuario.Id,
                usuario.Nombre,
                usuario.Email,
                usuario.PasswordHash,
                usuario.PerfilId,
                usuario.Activo,
                usuario.CreadoEn,
            },
            cancellationToken: ct));
    }
}

public class PerfilRepository(PostgresConnectionFactory conexiones) : IPerfilRepository
{
    public async Task<Guid?> BuscarIdPorNombreAsync(string nombre, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        return await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            "SELECT fn_perfil_id_por_nombre(@Nombre)", new { Nombre = nombre }, cancellationToken: ct));
    }
}
