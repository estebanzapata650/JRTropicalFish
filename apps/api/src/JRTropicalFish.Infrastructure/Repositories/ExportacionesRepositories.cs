using System.Text.Json;
using Dapper;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Exportaciones;
using JRTropicalFish.Domain.Exportaciones;
using JRTropicalFish.Infrastructure.Persistence;
using Npgsql;

namespace JRTropicalFish.Infrastructure.Repositories;

public class ConfiguracionExportacionRepository(PostgresConnectionFactory conexiones)
    : RepositorioInventario(conexiones), IConfiguracionExportacionRepository
{
    public async Task<DatosEmpresaInfo> DatosEmpresaAsync(CancellationToken ct = default) =>
        await UnoAsync<DatosEmpresaInfo>("SELECT * FROM fn_datos_empresa()", null, ct)
        ?? throw new InvalidOperationException("Falta la fila de datos_empresa (migración FuncionesExportaciones).");

    public Task ActualizarDatosEmpresaAsync(GuardarDatosEmpresaRequest d, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_datos_empresa_actualizar(@RazonSocial::text, @Nit::text, @Direccion::text, @CiudadPais::text, @Telefono::text,
                @Email::text, @SitioWeb::text, @Banco::text, @Swift::text, @CuentaBancaria::text, @Beneficiario::text,
                @DireccionBanco::text, @FirmanteNombre::text, @FirmanteCargo::text, @FirmanteTelefono::text, @Fecha::timestamptz)
            """,
            new
            {
                d.RazonSocial, d.Nit, d.Direccion, d.CiudadPais, d.Telefono, d.Email, d.SitioWeb, d.Banco, d.Swift, d.CuentaBancaria,
                d.Beneficiario, d.DireccionBanco, d.FirmanteNombre, d.FirmanteCargo, d.FirmanteTelefono, Fecha = fecha,
            },
            ct);

    public Task ActualizarLogoAsync(string? clave, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync("CALL sp_datos_empresa_logo(@Clave::text, @Fecha::timestamptz)", new { Clave = clave, Fecha = fecha }, ct);

    public async Task<TarifasFleteInfo> TarifasAsync(CancellationToken ct = default) =>
        await UnoAsync<TarifasFleteInfo>("SELECT * FROM fn_tarifas_flete()", null, ct)
        ?? throw new InvalidOperationException("Falta la fila de tarifas_flete (migración FuncionesExportaciones).");

    public Task ActualizarTarifasAsync(TarifasFleteInfo t, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_tarifas_flete_actualizar(@TarifaKgJr::numeric, @TarifaKgCliente::numeric, @FuelKg::numeric, @JpFeeKg::numeric,
                @VscKg::numeric, @VscMinimo::numeric, @DbcPorcentaje::numeric, @DbcMinimo::numeric, @DfPorcentaje::numeric,
                @DfMinimo::numeric, @DueAgentJr::numeric, @DueAgentCliente::numeric, @DueCarrier::numeric, @Fecha::timestamptz)
            """,
            new
            {
                t.TarifaKgJr, t.TarifaKgCliente, t.FuelKg, t.JpFeeKg, t.VscKg, t.VscMinimo, t.DbcPorcentaje, t.DbcMinimo, t.DfPorcentaje,
                t.DfMinimo, t.DueAgentJr, t.DueAgentCliente, t.DueCarrier, Fecha = fecha,
            },
            ct);

    public Task<IReadOnlyList<TipoCajaFila>> TiposCajaAsync(CancellationToken ct = default) =>
        ListarAsync<TipoCajaFila>("SELECT * FROM fn_tipos_caja_listar()", null, ct);

    public Task GuardarTipoCajaAsync(Guid id, GuardarTipoCaja c, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_tipo_caja_guardar(@Id::uuid, @Nombre::text, @DescripcionFactura::text, @CostoCop::numeric, @PrecioUsd::numeric,
                @KgPorCaja::numeric, @Pisos::integer, @Activo::boolean, @Fecha::timestamptz)
            """,
            new { Id = id, c.Nombre, c.DescripcionFactura, c.CostoCop, c.PrecioUsd, c.KgPorCaja, c.Pisos, c.Activo, Fecha = fecha }, ct);

    public Task<IReadOnlyList<TipoBolsaFila>> TiposBolsaAsync(CancellationToken ct = default) =>
        ListarAsync<TipoBolsaFila>("SELECT * FROM fn_tipos_bolsa_listar()", null, ct);

    public Task GuardarTipoBolsaAsync(Guid id, GuardarTipoBolsa b, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_tipo_bolsa_guardar(@Id::uuid, @Nombre::text, @Tamano::text, @Porcion::numeric, @CostoCop::numeric, @Activo::boolean,
                @Fecha::timestamptz)
            """,
            new { Id = id, b.Nombre, b.Tamano, b.Porcion, b.CostoCop, b.Activo, Fecha = fecha }, ct);

    public Task<IReadOnlyList<CargoFila>> CargosAsync(CancellationToken ct = default) =>
        ListarAsync<CargoFila>("SELECT * FROM fn_cargos_exportacion_listar()", null, ct);

    public Task ActualizarCargoAsync(string codigo, string nombre, decimal precio, CancellationToken ct = default) =>
        EjecutarAsync(
            "CALL sp_cargo_exportacion_actualizar(@Codigo::text, @Nombre::text, @Precio::numeric)",
            new { Codigo = codigo, Nombre = nombre, Precio = precio }, ct);

    public Task<ClienteExportacionInfo?> ClienteAsync(Guid clienteId, CancellationToken ct = default) =>
        UnoAsync<ClienteExportacionInfo>("SELECT * FROM fn_cliente_exportacion(@Id::uuid)", new { Id = clienteId }, ct);

    public Task GuardarClienteAsync(ClienteExportacionInfo c, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_cliente_exportacion_guardar(@ClienteId::uuid, @Direccion::text, @Eori::text, @FormatoFactura::text,
                @ListaPreciosId::uuid, @DescuentoPeces::numeric, @Fecha::timestamptz)
            """,
            new { c.ClienteId, c.Direccion, c.Eori, c.FormatoFactura, c.ListaPreciosId, c.DescuentoPeces, Fecha = fecha }, ct);
}

public class EmbarqueRepository(PostgresConnectionFactory conexiones) : RepositorioInventario(conexiones), IEmbarqueRepository
{
    public Task<IReadOnlyList<EmbarqueFila>> ListarAsync(Guid? comercialId, CancellationToken ct = default) =>
        ListarAsync<EmbarqueFila>("SELECT * FROM fn_embarques_listar(@ComercialId::uuid)", new { ComercialId = comercialId }, ct);

    public Task<EmbarqueFila?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        UnoAsync<EmbarqueFila>("SELECT * FROM fn_embarque_por_id(@Id::uuid)", new { Id = id }, ct);

    public Task<Guid?> IdPorVentaAsync(Guid ventaId, CancellationToken ct = default) =>
        EscalarAsync<Guid?>("SELECT fn_embarque_id_por_venta(@VentaId::uuid)", new { VentaId = ventaId }, ct);

    public Task<IReadOnlyList<EmbarqueLineaFila>> LineasAsync(Guid id, CancellationToken ct = default) =>
        ListarAsync<EmbarqueLineaFila>("SELECT * FROM fn_embarque_lineas(@Id::uuid)", new { Id = id }, ct);

    public Task<IReadOnlyList<EmbarqueEmpaqueFila>> EmpaqueAsync(Guid id, CancellationToken ct = default) =>
        ListarAsync<EmbarqueEmpaqueFila>("SELECT * FROM fn_embarque_empaque(@Id::uuid)", new { Id = id }, ct);

    public Task<IReadOnlyList<DocumentoEmbarqueFila>> DocumentosAsync(Guid id, CancellationToken ct = default) =>
        ListarAsync<DocumentoEmbarqueFila>("SELECT * FROM fn_embarque_documentos(@Id::uuid)", new { Id = id }, ct);

    public Task<IReadOnlyList<EnvioDocumentoFila>> EnviosAsync(Guid id, CancellationToken ct = default) =>
        ListarAsync<EnvioDocumentoFila>("SELECT * FROM fn_embarque_envios(@Id::uuid)", new { Id = id }, ct);

    public Task CrearAsync(Embarque e, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_embarque_crear(@Id::uuid, @Codigo::text, @VentaId::uuid, @PedidoClienteId::uuid, @ClienteId::uuid,
                @ComercialId::uuid, @ListaPreciosId::uuid, @DescuentoPeces::numeric, @Aerolinea::text, @Awb::text,
                @FormatoFactura::text, @Tarifas::jsonb, @Cargos::jsonb, @Fecha::timestamptz)
            """,
            new
            {
                e.Id, e.Codigo, e.VentaId, e.PedidoClienteId, e.ClienteId, e.ComercialId, e.ListaPreciosId, e.DescuentoPeces, e.Aerolinea,
                e.Awb, e.FormatoFactura, e.Tarifas, e.Cargos, Fecha = fecha,
            },
            ct);

    public Task ActualizarAsync(Embarque e, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_embarque_actualizar(@Id::uuid, @ListaPreciosId::uuid, @DescuentoPeces::numeric, @Aerolinea::text, @Ruta::text,
                @Awb::text, @Destino::text, @SalidaVuelo::timestamptz, @LlegadaVuelo::timestamptz, @FechaDespacho::date,
                @FormatoFactura::text, @AwbValor::numeric, @AnticipoRecibido::numeric, @Tarifas::jsonb, @Cargos::jsonb,
                @CajasEstimadas::jsonb, @CajasReales::jsonb, @Notas::text, @PedidoClienteId::uuid, @Fecha::timestamptz)
            """,
            new
            {
                e.Id, e.ListaPreciosId, e.DescuentoPeces, e.Aerolinea, e.Ruta, e.Awb, e.Destino, e.SalidaVuelo, e.LlegadaVuelo,
                e.FechaDespacho, e.FormatoFactura, e.AwbValor, e.AnticipoRecibido, e.Tarifas, e.Cargos, e.CajasEstimadas, e.CajasReales,
                e.Notas, e.PedidoClienteId, Fecha = fecha,
            },
            ct);

    public Task GuardarLineasAsync(Guid id, IReadOnlyList<EmbarqueLinea> lineas, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            "CALL sp_embarque_lineas_guardar(@Id::uuid, @Lineas::jsonb, @Fecha::timestamptz)",
            new { Id = id, Lineas = JsonSerializer.Serialize(lineas, Json), Fecha = fecha }, ct);

    public Task GuardarEmpaqueAsync(Guid id, IReadOnlyList<EmbarqueEmpaque> filas, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            "CALL sp_embarque_empaque_guardar(@Id::uuid, @Filas::jsonb, @Fecha::timestamptz)",
            new { Id = id, Filas = JsonSerializer.Serialize(filas, Json), Fecha = fecha }, ct);

    public Task GuardarDoaAsync(Guid id, IReadOnlyList<DoaFilaRequest> filas, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            "CALL sp_embarque_doa_guardar(@Id::uuid, @Filas::jsonb, @Fecha::timestamptz)",
            new { Id = id, Filas = JsonSerializer.Serialize(filas, Json), Fecha = fecha }, ct);

    public Task<FallaBaseDatos?> CambiarEstadoAsync(Guid id, string actual, string nuevo, DateTime fecha, CancellationToken ct = default) =>
        EjecutarConFallaAsync(
            "CALL sp_embarque_cambiar_estado(@Id::uuid, @Actual::text, @Nuevo::text, @Fecha::timestamptz)",
            new { Id = id, Actual = actual, Nuevo = nuevo, Fecha = fecha }, ct);

    public Task RegistrarPreinvoiceAsync(Guid id, string numero, DateOnly fechaDocumento, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            "CALL sp_embarque_preinvoice(@Id::uuid, @Numero::text, @FechaDocumento::date, @Fecha::timestamptz)",
            new { Id = id, Numero = numero, FechaDocumento = fechaDocumento, Fecha = fecha }, ct);

    public async Task<(int? Numero, FallaBaseDatos? Falla)> FacturarAsync(
        Guid id, string estadoActual, DateOnly fechaFactura, DateTime fecha, CancellationToken ct = default)
    {
        try
        {
            var numero = await EscalarAsync<int>(
                "SELECT fn_embarque_facturar(@Id::uuid, @Estado::text, @FechaFactura::date, @Fecha::timestamptz)",
                new { Id = id, Estado = estadoActual, FechaFactura = fechaFactura, Fecha = fecha }, ct);
            return (numero, null);
        }
        catch (PostgresException ex) when (ex.SqlState.StartsWith("JR", StringComparison.Ordinal))
        {
            return (null, new FallaBaseDatos(ex.SqlState, ex.MessageText));
        }
    }

    public Task RegistrarNotaCreditoAsync(Guid id, DateOnly fechaNota, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            "CALL sp_embarque_nota_credito(@Id::uuid, @FechaNota::date, @Fecha::timestamptz)",
            new { Id = id, FechaNota = fechaNota, Fecha = fecha }, ct);

    public Task SincronizarCrmAsync(
        Guid id, decimal total, int cajas, decimal fob, decimal flete, decimal doa, string? notaCredito, DateTime fecha,
        CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_embarque_sincronizar_crm(@Id::uuid, @Total::numeric, @Cajas::integer, @Fob::numeric, @Flete::numeric,
                @Doa::numeric, @NotaCredito::text, @Fecha::timestamptz)
            """,
            new { Id = id, Total = total, Cajas = cajas, Fob = fob, Flete = flete, Doa = doa, NotaCredito = notaCredito, Fecha = fecha }, ct);

    public Task RegistrarDocumentoAsync(DocumentoEmbarque d, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_documento_embarque_registrar(@Id::uuid, @EmbarqueId::uuid, @Tipo::text, @Numero::text, @ArchivoClave::text,
                @GeneradoPor::text, @Fecha::timestamptz)
            """,
            new { d.Id, d.EmbarqueId, d.Tipo, d.Numero, d.ArchivoClave, d.GeneradoPor, Fecha = fecha }, ct);

    public Task RegistrarEnvioAsync(EnvioDocumento e, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_envio_documento_registrar(@Id::uuid, @EmbarqueId::uuid, @TipoDocumento::text, @Para::text, @Asunto::text,
                @Proveedor::text, @Exitoso::boolean, @LlegoAlDestinatario::boolean, @MensajeId::text, @Error::text,
                @EnviadoPor::text, @Fecha::timestamptz)
            """,
            new
            {
                e.Id, e.EmbarqueId, e.TipoDocumento, e.Para, e.Asunto, e.Proveedor, e.Exitoso, e.LlegoAlDestinatario, e.MensajeId, e.Error,
                e.EnviadoPor, Fecha = fecha,
            },
            ct);
}
