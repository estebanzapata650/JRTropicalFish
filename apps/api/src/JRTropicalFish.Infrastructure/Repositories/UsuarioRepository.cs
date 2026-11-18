using Dapper;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Seguridad;
using JRTropicalFish.Domain.Seguridad;
using JRTropicalFish.Infrastructure.Persistence;

namespace JRTropicalFish.Infrastructure.Repositories;

/// <summary>
/// Patrón de todos los repositorios: Dapper llamando funciones (<c>SELECT * FROM fn_x(...)</c>) y
/// procedimientos (<c>CALL sp_x(...)</c>) PL/pgSQL creados por migración. En los <c>CALL</c> se
/// castea cada parámetro: un nulo sin tipo hace que Postgres diga que el procedimiento no existe.
/// </summary>
public class UsuarioRepository(PostgresConnectionFactory conexiones) : IUsuarioRepository
{
    public async Task<UsuarioConPerfil?> BuscarPorEmailAsync(string email, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<UsuarioConPerfil>(new CommandDefinition(
            "SELECT * FROM fn_usuario_por_email(@Email)", new { Email = email }, cancellationToken: ct));
    }

    public async Task<UsuarioConPerfil?> BuscarParaAccesoPorIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<UsuarioConPerfil>(new CommandDefinition(
            "SELECT * FROM fn_usuario_acceso_por_id(@Id)", new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<string>> ObtenerPermisosAsync(Guid usuarioId, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        var resultado = await connection.QueryAsync<string>(new CommandDefinition(
            "SELECT codigo FROM fn_usuario_permisos(@Id)", new { Id = usuarioId }, cancellationToken: ct));
        return resultado.AsList();
    }

    public async Task<IReadOnlyList<UsuarioResumen>> ListarAsync(string? busqueda, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        var resultado = await connection.QueryAsync<UsuarioResumen>(new CommandDefinition(
            "SELECT * FROM fn_usuarios_listar(@Busqueda)", new { Busqueda = busqueda }, cancellationToken: ct));
        return resultado.AsList();
    }

    public async Task<UsuarioResumen?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<UsuarioResumen>(new CommandDefinition(
            "SELECT * FROM fn_usuario_por_id(@Id)", new { Id = id }, cancellationToken: ct));
    }

    public async Task<bool> ExisteEmailAsync(string email, Guid? excluirId, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT fn_usuario_email_existe(@Email::text, @ExcluirId::uuid)",
            new { Email = email, ExcluirId = excluirId }, cancellationToken: ct));
    }

    public async Task<long> ContarAdministradoresAsync(Guid? excluirUsuarioId, Guid? excluirPerfilId, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT fn_administradores_contar(@ExcluirUsuarioId::uuid, @ExcluirPerfilId::uuid)",
            new { ExcluirUsuarioId = excluirUsuarioId, ExcluirPerfilId = excluirPerfilId }, cancellationToken: ct));
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

    public async Task ActualizarAsync(
        Guid id, string nombre, string email, Guid perfilId, bool invalidarSesiones, DateTime fecha, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(
            """
            CALL sp_usuario_actualizar(@Id::uuid, @Nombre::text, @Email::text, @PerfilId::uuid,
                                       @InvalidarSesiones::boolean, @Fecha::timestamptz)
            """,
            new { Id = id, Nombre = nombre, Email = email, PerfilId = perfilId, InvalidarSesiones = invalidarSesiones, Fecha = fecha },
            cancellationToken: ct));
    }

    public Task CambiarEstadoAsync(Guid id, bool activo, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            "CALL sp_usuario_cambiar_estado(@Id::uuid, @Activo::boolean, @Fecha::timestamptz)",
            new { Id = id, Activo = activo, Fecha = fecha }, ct);

    public Task CambiarPasswordAsync(Guid id, string passwordHash, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            "CALL sp_usuario_cambiar_password(@Id::uuid, @PasswordHash::text, @Fecha::timestamptz)",
            new { Id = id, PasswordHash = passwordHash, Fecha = fecha }, ct);

    public Task RegistrarAccesoAsync(Guid id, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync("CALL sp_usuario_registrar_acceso(@Id::uuid, @Fecha::timestamptz)", new { Id = id, Fecha = fecha }, ct);

    public Task RegistrarFalloAsync(Guid id, DateTime? bloquearHasta, CancellationToken ct = default) =>
        EjecutarAsync(
            "CALL sp_usuario_registrar_fallo(@Id::uuid, @BloquearHasta::timestamptz)",
            new { Id = id, BloquearHasta = bloquearHasta }, ct);

    public Task DesbloquearAsync(Guid id, CancellationToken ct = default) =>
        EjecutarAsync("CALL sp_usuario_desbloquear(@Id::uuid)", new { Id = id }, ct);

    public async Task<bool> SesionValidaAsync(Guid id, DateTime emitidoEn, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT fn_usuario_sesion_valida(@Id::uuid, @EmitidoEn::timestamptz)",
            new { Id = id, EmitidoEn = emitidoEn }, cancellationToken: ct));
    }

    private async Task EjecutarAsync(string sql, object parametros, CancellationToken ct)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, parametros, cancellationToken: ct));
    }
}

public class PermisoRepository(PostgresConnectionFactory conexiones) : IPermisoRepository
{
    public async Task<IReadOnlyList<PermisoLectura>> ListarAsync(CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        var resultado = await connection.QueryAsync<PermisoLectura>(new CommandDefinition(
            "SELECT * FROM fn_permisos_listar()", cancellationToken: ct));
        return resultado.AsList();
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

    public async Task<IReadOnlyList<PerfilResumen>> ListarAsync(string? busqueda, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        var resultado = await connection.QueryAsync<PerfilResumen>(new CommandDefinition(
            "SELECT * FROM fn_perfiles_listar(@Busqueda::text)", new { Busqueda = busqueda }, cancellationToken: ct));
        return resultado.AsList();
    }

    public async Task<PerfilDetalle?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);

        var perfil = await connection.QuerySingleOrDefaultAsync<PerfilFila>(new CommandDefinition(
            "SELECT * FROM fn_perfil_por_id(@Id)", new { Id = id }, cancellationToken: ct));
        if (perfil is null)
        {
            return null;
        }

        var permisos = await connection.QueryAsync<string>(new CommandDefinition(
            "SELECT codigo FROM fn_perfil_permisos(@Id)", new { Id = id }, cancellationToken: ct));

        return new PerfilDetalle(perfil.Id, perfil.Nombre, perfil.Descripcion, perfil.EsPlantilla, perfil.Activo, permisos.AsList());
    }

    public async Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT fn_perfil_nombre_existe(@Nombre::text, @ExcluirId::uuid)",
            new { Nombre = nombre, ExcluirId = excluirId }, cancellationToken: ct));
    }

    public async Task<long> ContarUsuariosAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT fn_perfil_usuarios_contar(@Id)", new { Id = id }, cancellationToken: ct));
    }

    public async Task CrearAsync(Perfil perfil, IReadOnlyList<string> permisos, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        await using var transaccion = await connection.BeginTransactionAsync(ct);

        await connection.ExecuteAsync(new CommandDefinition(
            "CALL sp_perfil_crear(@Id::uuid, @Nombre::text, @Descripcion::text, @Activo::boolean, @CreadoEn::timestamptz)",
            new { perfil.Id, perfil.Nombre, perfil.Descripcion, perfil.Activo, perfil.CreadoEn },
            transaccion, cancellationToken: ct));

        await ReemplazarPermisosAsync(connection, transaccion, perfil.Id, permisos, ct);
        await transaccion.CommitAsync(ct);
    }

    public async Task ActualizarAsync(
        Perfil perfil, IReadOnlyList<string> permisos, bool invalidarSesiones, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        await using var transaccion = await connection.BeginTransactionAsync(ct);

        await connection.ExecuteAsync(new CommandDefinition(
            """
            CALL sp_perfil_actualizar(@Id::uuid, @Nombre::text, @Descripcion::text, @Activo::boolean,
                                      @InvalidarSesiones::boolean, @ActualizadoEn::timestamptz)
            """,
            new { perfil.Id, perfil.Nombre, perfil.Descripcion, perfil.Activo, InvalidarSesiones = invalidarSesiones, perfil.ActualizadoEn },
            transaccion, cancellationToken: ct));

        await ReemplazarPermisosAsync(connection, transaccion, perfil.Id, permisos, ct);
        await transaccion.CommitAsync(ct);
    }

    public async Task EliminarAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(
            "CALL sp_perfil_eliminar(@Id::uuid)", new { Id = id }, cancellationToken: ct));
    }

    private static Task ReemplazarPermisosAsync(
        Npgsql.NpgsqlConnection connection, System.Data.Common.DbTransaction transaccion,
        Guid perfilId, IReadOnlyList<string> permisos, CancellationToken ct) =>
        connection.ExecuteAsync(new CommandDefinition(
            "CALL sp_perfil_reemplazar_permisos(@Id::uuid, @Codigos::text[])",
            new { Id = perfilId, Codigos = permisos.ToArray() }, transaccion, cancellationToken: ct));

    private sealed record PerfilFila(Guid Id, string Nombre, string? Descripcion, bool EsPlantilla, bool Activo);
}
