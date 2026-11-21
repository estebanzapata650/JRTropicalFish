using Dapper;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Administracion;
using JRTropicalFish.Application.Maestros;
using JRTropicalFish.Domain.Administracion;
using JRTropicalFish.Infrastructure.Persistence;

namespace JRTropicalFish.Infrastructure.Repositories;

/// <summary>
/// Maestros genéricos: la tabla viaja como parámetro a funciones PL/pgSQL que la validan contra su
/// lista blanca (<c>fn_maestro_tabla</c>) y la citan con <c>format('%I')</c>. Nunca se concatena en el SQL.
/// </summary>
public class MaestroRepository(PostgresConnectionFactory conexiones) : IMaestroRepository
{
    public async Task<IReadOnlyList<ValorMaestro>> ListarAsync(string tabla, bool soloActivos, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        var resultado = await connection.QueryAsync<ValorMaestro>(new CommandDefinition(
            "SELECT * FROM fn_maestro_listar(@Tabla::text, @SoloActivos::boolean)",
            new { Tabla = tabla, SoloActivos = soloActivos }, cancellationToken: ct));
        return resultado.AsList();
    }

    public async Task<ValorMaestro?> ObtenerAsync(string tabla, Guid id, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<ValorMaestro>(new CommandDefinition(
            "SELECT * FROM fn_maestro_por_id(@Tabla::text, @Id::uuid)", new { Tabla = tabla, Id = id }, cancellationToken: ct));
    }

    public Task<bool> ExisteCodigoAsync(string tabla, string codigo, Guid? excluirId, CancellationToken ct = default) =>
        ExisteAsync(tabla, "codigo", codigo, excluirId, ct);

    public Task<bool> ExisteNombreAsync(string tabla, string nombre, Guid? excluirId, CancellationToken ct = default) =>
        ExisteAsync(tabla, "nombre", nombre, excluirId, ct);

    public Task CrearAsync(string tabla, Guid id, string codigo, string nombre, bool activo, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            "CALL sp_maestro_crear(@Tabla::text, @Id::uuid, @Codigo::text, @Nombre::text, @Activo::boolean, @Fecha::timestamptz)",
            new { Tabla = tabla, Id = id, Codigo = codigo, Nombre = nombre, Activo = activo, Fecha = fecha }, ct);

    public Task ActualizarAsync(string tabla, Guid id, string codigo, string nombre, bool activo, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            "CALL sp_maestro_actualizar(@Tabla::text, @Id::uuid, @Codigo::text, @Nombre::text, @Activo::boolean, @Fecha::timestamptz)",
            new { Tabla = tabla, Id = id, Codigo = codigo, Nombre = nombre, Activo = activo, Fecha = fecha }, ct);

    public Task EliminarAsync(string tabla, Guid id, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            "CALL sp_maestro_eliminar(@Tabla::text, @Id::uuid, @Fecha::timestamptz)",
            new { Tabla = tabla, Id = id, Fecha = fecha }, ct);

    private async Task<bool> ExisteAsync(string tabla, string campo, string valor, Guid? excluirId, CancellationToken ct)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT fn_maestro_valor_existe(@Tabla::text, @Campo::text, @Valor::text, @ExcluirId::uuid)",
            new { Tabla = tabla, Campo = campo, Valor = valor, ExcluirId = excluirId }, cancellationToken: ct));
    }

    private async Task EjecutarAsync(string sql, object parametros, CancellationToken ct)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, parametros, cancellationToken: ct));
    }
}

public class EmpleadoRepository(PostgresConnectionFactory conexiones) : IEmpleadoRepository
{
    public async Task<IReadOnlyList<EmpleadoFila>> ListarAsync(string? busqueda, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        var resultado = await connection.QueryAsync<EmpleadoFila>(new CommandDefinition(
            "SELECT * FROM fn_empleados_listar(@Busqueda::text)", new { Busqueda = busqueda }, cancellationToken: ct));
        return resultado.AsList();
    }

    public async Task<EmpleadoFila?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<EmpleadoFila>(new CommandDefinition(
            "SELECT * FROM fn_empleado_por_id(@Id::uuid)", new { Id = id }, cancellationToken: ct));
    }

    public async Task<bool> ExisteIdentificacionAsync(
        Guid tipoIdentificacionId, string numero, Guid? excluirId, CancellationToken ct = default)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT fn_empleado_identificacion_existe(@TipoId::uuid, @Numero::text, @ExcluirId::uuid)",
            new { TipoId = tipoIdentificacionId, Numero = numero, ExcluirId = excluirId }, cancellationToken: ct));
    }

    public Task CrearAsync(Empleado e, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_empleado_crear(
                @Id::uuid, @TipoIdentificacionId::uuid, @NumeroIdentificacion::text, @Nombres::text, @Apellidos::text,
                @Genero::text, @FechaNacimiento::date, @Telefono::text, @Email::text, @Direccion::text, @Ciudad::text,
                @CargoId::uuid, @AreaId::uuid, @TipoContratoId::uuid, @FechaIngreso::date, @FechaRetiro::date,
                @Salario::numeric, @Activo::boolean, @CreadoEn::timestamptz)
            """,
            Parametros(e, new { e.Activo, e.CreadoEn }), ct);

    public Task ActualizarAsync(Empleado e, bool actualizarSalario, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_empleado_actualizar(
                @Id::uuid, @TipoIdentificacionId::uuid, @NumeroIdentificacion::text, @Nombres::text, @Apellidos::text,
                @Genero::text, @FechaNacimiento::date, @Telefono::text, @Email::text, @Direccion::text, @Ciudad::text,
                @CargoId::uuid, @AreaId::uuid, @TipoContratoId::uuid, @FechaIngreso::date, @FechaRetiro::date,
                @ActualizarSalario::boolean, @Salario::numeric, @ActualizadoEn::timestamptz)
            """,
            Parametros(e, new { ActualizarSalario = actualizarSalario, e.ActualizadoEn }), ct);

    public Task CambiarEstadoAsync(Guid id, bool activo, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            "CALL sp_empleado_cambiar_estado(@Id::uuid, @Activo::boolean, @Fecha::timestamptz)",
            new { Id = id, Activo = activo, Fecha = fecha }, ct);

    public Task CambiarFotoAsync(Guid id, string? fotoClave, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            "CALL sp_empleado_cambiar_foto(@Id::uuid, @FotoClave::text, @Fecha::timestamptz)",
            new { Id = id, FotoClave = fotoClave, Fecha = fecha }, ct);

    public Task EliminarAsync(Guid id, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync("CALL sp_empleado_eliminar(@Id::uuid, @Fecha::timestamptz)", new { Id = id, Fecha = fecha }, ct);

    /// <summary>Parámetros comunes de crear/actualizar más los propios de cada uno.</summary>
    private static DynamicParameters Parametros(Empleado e, object adicionales)
    {
        var parametros = new DynamicParameters(new
        {
            e.Id,
            e.TipoIdentificacionId,
            e.NumeroIdentificacion,
            e.Nombres,
            e.Apellidos,
            e.Genero,
            e.FechaNacimiento,
            e.Telefono,
            e.Email,
            e.Direccion,
            e.Ciudad,
            e.CargoId,
            e.AreaId,
            e.TipoContratoId,
            e.FechaIngreso,
            e.FechaRetiro,
            e.Salario,
        });
        parametros.AddDynamicParams(adicionales);
        return parametros;
    }

    private async Task EjecutarAsync(string sql, object parametros, CancellationToken ct)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, parametros, cancellationToken: ct));
    }
}
