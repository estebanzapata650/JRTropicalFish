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

public class AunapRepository(PostgresConnectionFactory conexiones) : RepositorioInventario(conexiones), IAunapRepository
{
    public Task<IReadOnlyList<EspecieAunapFila>> EspeciesAsync(CancellationToken ct = default) =>
        ListarAsync<EspecieAunapFila>("SELECT * FROM fn_especies_aunap_listar()", null, ct);

    public async Task<ImportacionAunap> ImportarAsync(IReadOnlyList<EspecieAunapImportar> especies, DateTime fecha, CancellationToken ct = default) =>
        (await UnoAsync<ImportacionAunap>(
            "SELECT * FROM fn_especies_aunap_importar(@Filas::jsonb, @Fecha::timestamptz)",
            new
            {
                Filas = JsonSerializer.Serialize(especies.Select(e => new
                {
                    id = Guid.CreateVersion7(), codigo = e.Codigo!.Trim(), nombreCientifico = e.NombreCientifico!.Trim(),
                    nombreComun = string.IsNullOrWhiteSpace(e.NombreComun) ? null : e.NombreComun.Trim(),
                    amazonia = e.Amazonia, orinoquia = e.Orinoquia, magdalena = e.Magdalena, pacifico = e.Pacifico, caribe = e.Caribe,
                    orden = string.IsNullOrWhiteSpace(e.Orden) ? null : e.Orden.Trim(),
                    familia = string.IsNullOrWhiteSpace(e.Familia) ? null : e.Familia.Trim(),
                })),
                Fecha = fecha,
            },
            ct))!;

    public Task<IReadOnlyList<ProductoAunapFila>> ProductosAsync(CancellationToken ct = default) =>
        ListarAsync<ProductoAunapFila>("SELECT * FROM fn_productos_aunap()", null, ct);

    public Task EnlazarProductoAsync(Guid productoId, Guid? especieAunapId, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync("CALL sp_producto_aunap(@ProductoId::uuid, @EspecieId::uuid, @Fecha::timestamptz)",
            new { ProductoId = productoId, EspecieId = especieAunapId, Fecha = fecha }, ct);

    public Task<int> EnlazarAutomaticoAsync(DateTime fecha, CancellationToken ct = default) =>
        EscalarAsync<int>("SELECT fn_productos_aunap_enlazar(@Fecha::timestamptz)", new { Fecha = fecha }, ct);

    public Task<PermisoAunapFila?> PermisoAsync(Guid embarqueId, CancellationToken ct = default) =>
        UnoAsync<PermisoAunapFila>("SELECT * FROM fn_permiso_aunap(@Id::uuid)", new { Id = embarqueId }, ct);

    public Task<IReadOnlyList<PermisoAunapLineaFila>> PermisoLineasAsync(Guid embarqueId, CancellationToken ct = default) =>
        ListarAsync<PermisoAunapLineaFila>("SELECT * FROM fn_permiso_aunap_lineas(@Id::uuid)", new { Id = embarqueId }, ct);

    public Task GuardarPermisoAsync(Guid embarqueId, GuardarPermisoAunapRequest p, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_permiso_aunap_guardar(@Id::uuid, @Numero::text, @FechaPermiso::date, @VuceFechaSolicitud::date, @DexNumero::text,
                @FacturaDian::text, @CitesNumero::text, @CitesFecha::date, @Notas::text, @Lineas::jsonb, @Fecha::timestamptz)
            """,
            new
            {
                Id = embarqueId, p.Numero, FechaPermiso = p.Fecha, p.VuceFechaSolicitud, p.DexNumero, p.FacturaDian, p.CitesNumero,
                p.CitesFecha, p.Notas,
                Lineas = JsonSerializer.Serialize((p.Lineas ?? []).Select(l => new { especie = l.EspecieAunapId, cantidad = l.Cantidad })),
                Fecha = fecha,
            },
            ct);

    public Task<string> AsignarProformaAsync(Guid embarqueId, DateOnly fecha, DateTime actualizado, CancellationToken ct = default) =>
        EscalarAsync<string>(
            "SELECT fn_proforma_asignar(@Id::uuid, @FechaProforma::date, @Fecha::timestamptz)",
            new { Id = embarqueId, FechaProforma = fecha, Fecha = actualizado }, ct);

    public Task<IReadOnlyList<MunicipioAunapFila>> MunicipiosAsync(CancellationToken ct = default) =>
        ListarAsync<MunicipioAunapFila>("SELECT * FROM fn_municipios_aunap()", null, ct);

    public Task<IReadOnlyList<CentroAcopioAunapFila>> CentrosAsync(CancellationToken ct = default) =>
        ListarAsync<CentroAcopioAunapFila>("SELECT * FROM fn_centros_acopio_aunap()", null, ct);

    public Task GuardarCentroAsync(Guid centroId, int? municipioId, bool excluir, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            "CALL sp_centro_acopio_aunap_guardar(@Id::uuid, @Municipio::integer, @Excluir::boolean, @Fecha::timestamptz)",
            new { Id = centroId, Municipio = municipioId, Excluir = excluir, Fecha = fecha }, ct);

    public async Task<ParametrosAunapFila> ParametrosAsync(CancellationToken ct = default) =>
        await UnoAsync<ParametrosAunapFila>("SELECT * FROM fn_parametros_aunap()", null, ct)
        ?? new ParametrosAunapFila(null, null, null, null, null, null, null, null);

    public Task GuardarParametrosAsync(GuardarParametrosAunapRequest p, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_parametros_aunap_guardar(@RepresentanteLegal::text, @Resolucion::text, @VigenciaDesde::date, @VigenciaHasta::date,
                @Direccion::text, @Correo::text, @Telefono::text, @Fecha::timestamptz)
            """,
            new { p.RepresentanteLegal, p.Resolucion, p.VigenciaDesde, p.VigenciaHasta, p.Direccion, p.Correo, p.Telefono, Fecha = fecha }, ct);

    public Task<IReadOnlyList<DespachoAunapFila>> DespachosAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default) =>
        ListarAsync<DespachoAunapFila>("SELECT * FROM fn_informe_aunap(@Desde::date, @Hasta::date)", new { Desde = desde, Hasta = hasta }, ct);
}

public class EmpaqueRepository(PostgresConnectionFactory conexiones) : RepositorioInventario(conexiones), IEmpaqueRepository
{
    public Task<IReadOnlyList<EstandarEmpaqueFila>> EstandaresAsync(CancellationToken ct = default) =>
        ListarAsync<EstandarEmpaqueFila>("SELECT * FROM fn_estandares_empaque()", null, ct);

    public Task GuardarEstandarAsync(Guid productoId, GuardarEstandarEmpaqueRequest e, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_estandar_empaque_guardar(@Producto::uuid, @TipoBolsaId::uuid, @PecesMin::integer, @PecesMax::integer,
                @LitrosAgua::numeric, @Notas::text, @Revisado::boolean, @Aditivos::jsonb, @Fecha::timestamptz)
            """,
            new
            {
                Producto = productoId, e.TipoBolsaId, e.PecesMin, e.PecesMax, e.LitrosAgua, e.Notas, e.Revisado,
                Aditivos = JsonSerializer.Serialize(e.Aditivos ?? []), Fecha = fecha,
            },
            ct);

    public Task EliminarEstandarAsync(Guid productoId, CancellationToken ct = default) =>
        EjecutarAsync("CALL sp_estandar_empaque_eliminar(@Id::uuid)", new { Id = productoId }, ct);

    public async Task<ParametrosEmpaqueFila> ParametrosAsync(CancellationToken ct = default) =>
        await UnoAsync<ParametrosEmpaqueFila>("SELECT * FROM fn_parametros_empaque()", null, ct) ?? new ParametrosEmpaqueFila(null, null, null);

    public Task GuardarParametrosAsync(GuardarParametrosEmpaqueRequest p, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            "CALL sp_parametros_empaque_guardar(@CostoHeatPackCop::numeric, @CostoOxigenoBolsaCop::numeric, @Fecha::timestamptz)",
            new { p.CostoHeatPackCop, p.CostoOxigenoBolsaCop, Fecha = fecha }, ct);

    public Task<IReadOnlyList<EmpaqueBolsaFila>> BolsasAsync(CancellationToken ct = default) =>
        ListarAsync<EmpaqueBolsaFila>("SELECT * FROM fn_empaque_bolsas()", null, ct);

    public Task GuardarLitrosBolsaAsync(Guid tipoBolsaId, decimal? litros, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync("CALL sp_empaque_bolsa_guardar(@Id::uuid, @Litros::numeric, @Fecha::timestamptz)", new { Id = tipoBolsaId, Litros = litros, Fecha = fecha }, ct);

    public Task<IReadOnlyList<AditivoEmpaqueFila>> AditivosAsync(CancellationToken ct = default) =>
        ListarAsync<AditivoEmpaqueFila>("SELECT * FROM fn_empaque_aditivos()", null, ct);

    public Task GuardarCostoAditivoAsync(Guid medicamentoId, decimal? costo, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync("CALL sp_empaque_aditivo_costo(@Id::uuid, @Costo::numeric, @Fecha::timestamptz)", new { Id = medicamentoId, Costo = costo, Fecha = fecha }, ct);

    public Task<IReadOnlyList<DoaEmpaqueFila>> DoaAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default) =>
        ListarAsync<DoaEmpaqueFila>("SELECT * FROM fn_doa_empaque(@Desde::date, @Hasta::date)", new { Desde = desde, Hasta = hasta }, ct);
}
