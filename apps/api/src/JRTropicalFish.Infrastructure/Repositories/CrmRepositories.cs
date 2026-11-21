using Dapper;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Crm;
using JRTropicalFish.Domain.Crm;
using JRTropicalFish.Infrastructure.Persistence;
using JRTropicalFish.Infrastructure.Persistence.Migrations;
using Npgsql;

namespace JRTropicalFish.Infrastructure.Repositories;

/// <summary>
/// Base de los repositorios del CRM: abrir conexión, consultar y ejecutar. Los procedimientos de
/// "comparar y escribir" lanzan <c>SQLSTATE JR001</c> cuando otro cambió el registro antes; aquí se
/// traduce a <c>false</c> para que el servicio responda 409.
/// </summary>
public abstract class RepositorioCrm(PostgresConnectionFactory conexiones)
{
    protected async Task<IReadOnlyList<T>> ListarAsync<T>(string sql, object? parametros, CancellationToken ct)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        return (await connection.QueryAsync<T>(new CommandDefinition(sql, parametros, cancellationToken: ct))).AsList();
    }

    protected async Task<T?> UnoAsync<T>(string sql, object? parametros, CancellationToken ct)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<T>(new CommandDefinition(sql, parametros, cancellationToken: ct));
    }

    protected async Task<T> EscalarAsync<T>(string sql, object? parametros, CancellationToken ct)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        return (await connection.ExecuteScalarAsync<T>(new CommandDefinition(sql, parametros, cancellationToken: ct)))!;
    }

    protected async Task EjecutarAsync(string sql, object parametros, CancellationToken ct)
    {
        await using var connection = await conexiones.AbrirAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, parametros, cancellationToken: ct));
    }

    /// <returns><c>false</c> si el procedimiento detectó un cambio concurrente.</returns>
    protected async Task<bool> EjecutarSiNoCambioAsync(string sql, object parametros, CancellationToken ct)
    {
        try
        {
            await EjecutarAsync(sql, parametros, ct);
            return true;
        }
        catch (PostgresException ex) when (ex.SqlState == FuncionesCrm.SqlStateConcurrencia)
        {
            return false;
        }
    }
}

public class ConsecutivoRepository(PostgresConnectionFactory conexiones) : RepositorioCrm(conexiones), IConsecutivoRepository
{
    public Task<string> SiguienteAsync(string prefijo, CancellationToken ct = default) =>
        EscalarAsync<string>("SELECT fn_siguiente_consecutivo(@Prefijo::text)", new { Prefijo = prefijo }, ct);

    public Task AjustarAsync(string prefijo, int numero, CancellationToken ct = default) =>
        EjecutarAsync("CALL sp_consecutivo_ajustar(@Prefijo::text, @Numero::integer)", new { Prefijo = prefijo, Numero = numero }, ct);
}

public class ComercialRepository(PostgresConnectionFactory conexiones) : RepositorioCrm(conexiones), IComercialRepository
{
    public Task<IReadOnlyList<ComercialInfo>> ListarAsync(CancellationToken ct = default) =>
        ListarAsync<ComercialInfo>("SELECT * FROM fn_comerciales_listar()", null, ct);
}

public class ClienteRepository(PostgresConnectionFactory conexiones) : RepositorioCrm(conexiones), IClienteRepository
{
    public Task<IReadOnlyList<ClienteFila>> ListarAsync(string? busqueda, CancellationToken ct = default) =>
        ListarAsync<ClienteFila>("SELECT * FROM fn_clientes_listar(@Busqueda::text)", new { Busqueda = busqueda }, ct);

    public Task<ClienteFila?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        UnoAsync<ClienteFila>("SELECT * FROM fn_cliente_por_id(@Id::uuid)", new { Id = id }, ct);

    public Task<bool> ExisteCodigoAsync(string codigo, Guid? excluirId, CancellationToken ct = default) =>
        ExisteAsync("codigo", codigo, excluirId, ct);

    public Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId, CancellationToken ct = default) =>
        ExisteAsync("nombre", nombre, excluirId, ct);

    public Task<bool> TieneMovimientosAsync(Guid id, CancellationToken ct = default) =>
        EscalarAsync<bool>("SELECT fn_cliente_tiene_movimientos(@Id::uuid)", new { Id = id }, ct);

    public Task CrearAsync(Cliente c, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_cliente_crear(@Id::uuid, @Codigo::text, @NombreEmpresa::text, @ContactoPrincipal::text, @Email::text,
                                  @TelefonoWhatsapp::text, @Pais::text, @Ciudad::text, @AeropuertoIata::text,
                                  @TipoCliente::text, @LicenciaImportacion::text, @Estado::text, @Notas::text,
                                  @CreadoEn::timestamptz)
            """,
            new
            {
                c.Id, c.Codigo, c.NombreEmpresa, c.ContactoPrincipal, c.Email, c.TelefonoWhatsapp, c.Pais, c.Ciudad,
                c.AeropuertoIata, c.TipoCliente, c.LicenciaImportacion, c.Estado, c.Notas, c.CreadoEn,
            },
            ct);

    public Task ActualizarAsync(Cliente c, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_cliente_actualizar(@Id::uuid, @NombreEmpresa::text, @ContactoPrincipal::text, @Email::text,
                                       @TelefonoWhatsapp::text, @Pais::text, @Ciudad::text, @AeropuertoIata::text,
                                       @TipoCliente::text, @LicenciaImportacion::text, @Estado::text, @Notas::text,
                                       @ActualizadoEn::timestamptz)
            """,
            new
            {
                c.Id, c.NombreEmpresa, c.ContactoPrincipal, c.Email, c.TelefonoWhatsapp, c.Pais, c.Ciudad,
                c.AeropuertoIata, c.TipoCliente, c.LicenciaImportacion, c.Estado, c.Notas, c.ActualizadoEn,
            },
            ct);

    public Task EliminarAsync(Guid id, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync("CALL sp_cliente_eliminar(@Id::uuid, @Fecha::timestamptz)", new { Id = id, Fecha = fecha }, ct);

    private Task<bool> ExisteAsync(string campo, string valor, Guid? excluirId, CancellationToken ct) =>
        EscalarAsync<bool>(
            "SELECT fn_cliente_valor_existe(@Campo::text, @Valor::text, @ExcluirId::uuid)",
            new { Campo = campo, Valor = valor, ExcluirId = excluirId }, ct);
}

public class OportunidadRepository(PostgresConnectionFactory conexiones) : RepositorioCrm(conexiones), IOportunidadRepository
{
    public Task<IReadOnlyList<OportunidadFila>> ListarAsync(Guid? comercialId, Guid? clienteId, CancellationToken ct = default) =>
        ListarAsync<OportunidadFila>(
            "SELECT * FROM fn_oportunidades_listar(@ComercialId::uuid, @ClienteId::uuid)",
            new { ComercialId = comercialId, ClienteId = clienteId }, ct);

    public Task<OportunidadFila?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        UnoAsync<OportunidadFila>("SELECT * FROM fn_oportunidad_por_id(@Id::uuid)", new { Id = id }, ct);

    public Task CrearAsync(Oportunidad o, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_oportunidad_crear(@Id::uuid, @Codigo::text, @ClienteId::uuid, @TipoVenta::text, @ComercialId::uuid,
                                      @FechaContactoInicial::date, @Etapa::text, @Moneda::text, @MontoEstimado::numeric,
                                      @CajasEstimadas::integer, @NotasSeguimiento::text, @CreadoEn::timestamptz)
            """,
            new
            {
                o.Id, o.Codigo, o.ClienteId, o.TipoVenta, o.ComercialId, o.FechaContactoInicial, o.Etapa, o.Moneda,
                o.MontoEstimado, o.CajasEstimadas, o.NotasSeguimiento, o.CreadoEn,
            },
            ct);

    public Task ActualizarAsync(Oportunidad o, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_oportunidad_actualizar(@Id::uuid, @ClienteId::uuid, @TipoVenta::text, @ComercialId::uuid,
                                           @FechaContactoInicial::date, @Moneda::text, @MontoEstimado::numeric,
                                           @CajasEstimadas::integer, @NotasSeguimiento::text, @ActualizadoEn::timestamptz)
            """,
            new
            {
                o.Id, o.ClienteId, o.TipoVenta, o.ComercialId, o.FechaContactoInicial, o.Moneda, o.MontoEstimado,
                o.CajasEstimadas, o.NotasSeguimiento, o.ActualizadoEn,
            },
            ct);

    public Task<bool> CambiarEtapaAsync(
        Guid id, string etapaEsperada, string etapa, string? motivoPerdida, DateTime fecha, CancellationToken ct = default) =>
        EjecutarSiNoCambioAsync(
            "CALL sp_oportunidad_cambiar_etapa(@Id::uuid, @Esperada::text, @Etapa::text, @Motivo::text, @Fecha::timestamptz)",
            new { Id = id, Esperada = etapaEsperada, Etapa = etapa, Motivo = motivoPerdida, Fecha = fecha }, ct);

    public Task<bool> GanarAsync(
        Guid id,
        string etapaEsperada,
        DateOnly fechaPagoConfirmado,
        Creado venta,
        string origenCanal,
        decimal? trm,
        string estadoPago,
        Creado? tracking,
        DateTime fecha,
        CancellationToken ct = default) =>
        EjecutarSiNoCambioAsync(
            """
            CALL sp_oportunidad_ganar(@Id::uuid, @Esperada::text, @FechaPago::date, @VentaId::uuid, @VentaCodigo::text,
                                      @OrigenCanal::text, @Trm::numeric, @EstadoPago::text, @TrackingId::uuid,
                                      @TrackingCodigo::text, @Fecha::timestamptz)
            """,
            new
            {
                Id = id, Esperada = etapaEsperada, FechaPago = fechaPagoConfirmado, VentaId = venta.Id,
                VentaCodigo = venta.Codigo, OrigenCanal = origenCanal, Trm = trm, EstadoPago = estadoPago,
                TrackingId = tracking?.Id, TrackingCodigo = tracking?.Codigo, Fecha = fecha,
            },
            ct);

    public Task EliminarAsync(Guid id, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync("CALL sp_oportunidad_eliminar(@Id::uuid, @Fecha::timestamptz)", new { Id = id, Fecha = fecha }, ct);
}

public class VentaRepository(PostgresConnectionFactory conexiones) : RepositorioCrm(conexiones), IVentaRepository
{
    public Task<IReadOnlyList<VentaFila>> ListarAsync(
        Guid? comercialId, Guid? clienteId, DateOnly? desde, DateOnly? hasta, CancellationToken ct = default) =>
        ListarAsync<VentaFila>(
            "SELECT * FROM fn_ventas_listar(@ComercialId::uuid, @ClienteId::uuid, @Desde::date, @Hasta::date)",
            new { ComercialId = comercialId, ClienteId = clienteId, Desde = desde, Hasta = hasta }, ct);

    public Task<VentaFila?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        UnoAsync<VentaFila>("SELECT * FROM fn_venta_por_id(@Id::uuid)", new { Id = id }, ct);

    public Task CrearAsync(Venta v, Creado? tracking, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_venta_crear(@Id::uuid, @Codigo::text, @ClienteId::uuid, @ComercialId::uuid, @OrigenCanal::text,
                                @FechaPedido::date, @TipoVenta::text, @Moneda::text, @Total::numeric, @Trm::numeric,
                                @TotalCajas::integer, @TransportadoraAerolinea::text, @NoGuiaAwb::text, @EstadoPago::text,
                                @EstadoPreparacion::text, @CiudadEntrega::text, @DireccionEntrega::text, @Notas::text,
                                @TrackingId::uuid, @TrackingCodigo::text, @CreadoEn::timestamptz)
            """,
            new
            {
                v.Id, v.Codigo, v.ClienteId, v.ComercialId, v.OrigenCanal, v.FechaPedido, v.TipoVenta, v.Moneda, v.Total,
                v.Trm, v.TotalCajas, v.TransportadoraAerolinea, v.NoGuiaAwb, v.EstadoPago, v.EstadoPreparacion,
                v.CiudadEntrega, v.DireccionEntrega, v.Notas, TrackingId = tracking?.Id, TrackingCodigo = tracking?.Codigo,
                v.CreadoEn,
            },
            ct);

    public Task ActualizarAsync(Venta v, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_venta_actualizar(@Id::uuid, @OrigenCanal::text, @FechaPedido::date, @Moneda::text, @Total::numeric,
                                     @Trm::numeric, @TotalCajas::integer, @TransportadoraAerolinea::text, @NoGuiaAwb::text,
                                     @EstadoPago::text, @EstadoPreparacion::text, @CiudadEntrega::text,
                                     @DireccionEntrega::text, @Notas::text, @ActualizadoEn::timestamptz)
            """,
            new
            {
                v.Id, v.OrigenCanal, v.FechaPedido, v.Moneda, v.Total, v.Trm, v.TotalCajas, v.TransportadoraAerolinea,
                v.NoGuiaAwb, v.EstadoPago, v.EstadoPreparacion, v.CiudadEntrega, v.DireccionEntrega, v.Notas,
                v.ActualizadoEn,
            },
            ct);
}

public class TrackingRepository(PostgresConnectionFactory conexiones) : RepositorioCrm(conexiones), ITrackingRepository
{
    public Task<IReadOnlyList<TrackingFila>> ListarAsync(Guid? comercialId, Guid? clienteId, CancellationToken ct = default) =>
        ListarAsync<TrackingFila>(
            "SELECT * FROM fn_tracking_listar(@ComercialId::uuid, @ClienteId::uuid)",
            new { ComercialId = comercialId, ClienteId = clienteId }, ct);

    public Task<TrackingFila?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        UnoAsync<TrackingFila>("SELECT * FROM fn_tracking_por_id(@Id::uuid)", new { Id = id }, ct);

    public Task ActualizarAsync(TrackingVuelo t, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_tracking_actualizar(@Id::uuid, @NFacturaInvoice::text, @NoAwb::text, @Aerolinea::text,
                                        @FechaDespachoVuelo::date, @CajasReales::integer, @Fase::text,
                                        @MontoFobUsd::numeric, @FleteAereoUsd::numeric, @DoaReportadoUsd::numeric,
                                        @NotaCreditoNo::text, @ActualizadoEn::timestamptz)
            """,
            new
            {
                t.Id, t.NFacturaInvoice, t.NoAwb, t.Aerolinea, t.FechaDespachoVuelo, t.CajasReales, t.Fase, t.MontoFobUsd,
                t.FleteAereoUsd, t.DoaReportadoUsd, t.NotaCreditoNo, t.ActualizadoEn,
            },
            ct);

    public Task<bool> CambiarFaseAsync(Guid id, string faseEsperada, string fase, DateTime fecha, CancellationToken ct = default) =>
        EjecutarSiNoCambioAsync(
            "CALL sp_tracking_cambiar_fase(@Id::uuid, @Esperada::text, @Fase::text, @Fecha::timestamptz)",
            new { Id = id, Esperada = faseEsperada, Fase = fase, Fecha = fecha }, ct);
}

public class TareaRepository(PostgresConnectionFactory conexiones) : RepositorioCrm(conexiones), ITareaRepository
{
    public Task<IReadOnlyList<TareaFila>> ListarAsync(DateOnly hoy, Guid? comercialId, Guid? clienteId, CancellationToken ct = default) =>
        ListarAsync<TareaFila>(
            "SELECT * FROM fn_tareas_listar(@Hoy::date, @ComercialId::uuid, @ClienteId::uuid)",
            new { Hoy = hoy, ComercialId = comercialId, ClienteId = clienteId }, ct);

    public Task<TareaFila?> ObtenerAsync(Guid id, DateOnly hoy, CancellationToken ct = default) =>
        UnoAsync<TareaFila>("SELECT * FROM fn_tarea_por_id(@Id::uuid, @Hoy::date)", new { Id = id, Hoy = hoy }, ct);

    public Task CrearAsync(Tarea t, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_tarea_crear(@Id::uuid, @Codigo::text, @ClienteId::uuid, @OportunidadId::uuid, @Accion::text,
                                @TipoAccion::text, @Estado::text, @Prioridad::text, @ComercialId::uuid,
                                @FechaAsignacion::date, @FechaVencimiento::date, @Notas::text,
                                @CompletadaEn::timestamptz, @CreadoEn::timestamptz)
            """,
            new
            {
                t.Id, t.Codigo, t.ClienteId, t.OportunidadId, t.Accion, t.TipoAccion, t.Estado, t.Prioridad, t.ComercialId,
                t.FechaAsignacion, t.FechaVencimiento, t.Notas, t.CompletadaEn, t.CreadoEn,
            },
            ct);

    public Task ActualizarAsync(Tarea t, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_tarea_actualizar(@Id::uuid, @ClienteId::uuid, @OportunidadId::uuid, @Accion::text, @TipoAccion::text,
                                     @Estado::text, @Prioridad::text, @ComercialId::uuid, @FechaAsignacion::date,
                                     @FechaVencimiento::date, @Notas::text, @CompletadaEn::timestamptz,
                                     @ActualizadoEn::timestamptz)
            """,
            new
            {
                t.Id, t.ClienteId, t.OportunidadId, t.Accion, t.TipoAccion, t.Estado, t.Prioridad, t.ComercialId,
                t.FechaAsignacion, t.FechaVencimiento, t.Notas, t.CompletadaEn, t.ActualizadoEn,
            },
            ct);

    public Task<bool> CambiarEstadoAsync(
        Guid id, string estadoEsperado, string estado, DateTime? completadaEn, DateTime fecha, CancellationToken ct = default) =>
        EjecutarSiNoCambioAsync(
            """
            CALL sp_tarea_cambiar_estado(@Id::uuid, @Esperado::text, @Estado::text, @CompletadaEn::timestamptz,
                                         @Fecha::timestamptz)
            """,
            new { Id = id, Esperado = estadoEsperado, Estado = estado, CompletadaEn = completadaEn, Fecha = fecha }, ct);

    public Task EliminarAsync(Guid id, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync("CALL sp_tarea_eliminar(@Id::uuid, @Fecha::timestamptz)", new { Id = id, Fecha = fecha }, ct);
}

public class MetricasRepository(PostgresConnectionFactory conexiones) : RepositorioCrm(conexiones), IMetricasRepository
{
    public async Task<ParametrosComercialesInfo> ObtenerParametrosAsync(CancellationToken ct = default) =>
        await UnoAsync<ParametrosComercialesInfo>("SELECT * FROM fn_parametros_comerciales()", null, ct)
        ?? throw new InvalidOperationException("Falta la fila de parametros_comerciales (la siembra la migración FuncionesCrm).");

    public Task ActualizarParametrosAsync(GuardarParametrosRequest p, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_parametros_comerciales_actualizar(@MetaEmpresaCop::numeric, @MetaComercialCop::numeric,
                                                      @PuntoEquilibrioDespachos::integer, @PorcentajeComision::numeric,
                                                      @UmbralDoa::numeric, @MetaClientesNuevos::integer, @Fecha::timestamptz)
            """,
            new
            {
                p.MetaEmpresaCop, p.MetaComercialCop, p.PuntoEquilibrioDespachos, p.PorcentajeComision, p.UmbralDoa,
                p.MetaClientesNuevos, Fecha = fecha,
            },
            ct);

    public Task<IReadOnlyList<MetaMesFila>> MetasDelMesAsync(DateOnly mes, CancellationToken ct = default) =>
        ListarAsync<MetaMesFila>("SELECT * FROM fn_metas_mes(@Mes::date)", new { Mes = mes }, ct);

    public Task GuardarMetaAsync(Guid id, DateOnly mes, Guid comercialId, decimal metaCop, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            "CALL sp_meta_guardar(@Id::uuid, @Mes::date, @ComercialId::uuid, @MetaCop::numeric, @Fecha::timestamptz)",
            new { Id = id, Mes = mes, ComercialId = comercialId, MetaCop = metaCop, Fecha = fecha }, ct);

    public Task<IReadOnlyList<VentasComercialFila>> VentasPorComercialAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default) =>
        ListarAsync<VentasComercialFila>(
            "SELECT * FROM fn_crm_ventas_por_comercial(@Desde::date, @Hasta::date)", new { Desde = desde, Hasta = hasta }, ct);

    public Task<IReadOnlyList<EmbudoFila>> EmbudoAsync(DateTime desdeUtc, DateTime hastaUtc, Guid? comercialId, CancellationToken ct = default) =>
        ListarAsync<EmbudoFila>(
            "SELECT * FROM fn_crm_embudo(@Desde::timestamptz, @Hasta::timestamptz, @ComercialId::uuid)",
            new { Desde = desdeUtc, Hasta = hastaUtc, ComercialId = comercialId }, ct);

    public async Task<ResumenPeriodoFila> ResumenPeriodoAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default) =>
        await UnoAsync<ResumenPeriodoFila>(
            "SELECT * FROM fn_crm_resumen_periodo(@Desde::date, @Hasta::date)", new { Desde = desde, Hasta = hasta }, ct)
        ?? new ResumenPeriodoFila(0, 0);
}
