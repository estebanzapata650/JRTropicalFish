using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Crm;
using JRTropicalFish.Domain.Crm;

namespace JRTropicalFish.Tests.Crm;

public class OportunidadServiceTests
{
    [Fact]
    public async Task Un_comercial_crea_oportunidades_a_su_nombre_con_codigo_consecutivo()
    {
        var mundo = new CrmEnMemoria();

        var creada = await mundo.OportunidadService().CrearAsync(mundo.Oportunidad(), mundo.ComoAna);

        Assert.True(creada.Exitoso);
        Assert.Equal("OPT-101", creada.Valor.Codigo);
        var o = mundo.Oportunidades[creada.Valor.Id];
        Assert.Equal(mundo.Ana.Id, o.ComercialId);
        Assert.Equal(EtapasPipeline.Prospeccion, o.Etapa);
    }

    [Fact]
    public async Task Un_comercial_no_asigna_oportunidades_a_otro()
    {
        var mundo = new CrmEnMemoria();

        var resultado = await mundo.OportunidadService().CrearAsync(
            mundo.Oportunidad() with { ComercialId = mundo.Beto.Id }, mundo.ComoAna);

        Assert.Equal(TipoError.Prohibido, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task Un_comercial_no_ve_lo_de_otro_y_gerencia_ve_todo()
    {
        var mundo = new CrmEnMemoria();
        var servicio = mundo.OportunidadService();
        var deBeto = await servicio.CrearAsync(mundo.Oportunidad(), mundo.ComoBeto);

        Assert.Empty(await servicio.ListarAsync(mundo.ComoAna));
        Assert.Equal(TipoError.NoEncontrado, (await servicio.ObtenerAsync(deBeto.Valor.Id, mundo.ComoAna)).Error!.Tipo);
        Assert.Single(await servicio.ListarAsync(mundo.ComoGerencia));
    }

    [Fact]
    public async Task Ganar_una_exportacion_crea_la_venta_y_el_tracking_en_cuarentena()
    {
        var mundo = new CrmEnMemoria();
        var servicio = mundo.OportunidadService();
        var creada = await servicio.CrearAsync(mundo.Oportunidad(), mundo.ComoAna);

        var ganada = await servicio.GanarAsync(
            creada.Valor.Id, new GanarOportunidadRequest(EtapasPipeline.Prospeccion, null, OrigenesCanal.B2BDirecto, 4100m, null),
            mundo.ComoAna);

        Assert.True(ganada.Exitoso);
        Assert.Equal(EtapasPipeline.Ganada, mundo.Oportunidades[creada.Valor.Id].Etapa);
        var venta = mundo.Ventas[ganada.Valor.VentaId];
        Assert.Equal("VTA-801", venta.Codigo);
        Assert.Equal(4850m, venta.Total);
        Assert.Equal(4100m, venta.Trm);
        Assert.Equal(EstadosPago.Anticipo, venta.EstadoPago);
        Assert.Equal(mundo.Hoy, venta.FechaPedido);
        var tracking = mundo.Tracking[ganada.Valor.TrackingId!.Value];
        Assert.Equal(FasesTracking.Cuarentena, tracking.Fase);
        Assert.Equal(12, tracking.CajasReales);
        Assert.Equal(4850m, tracking.MontoFobUsd);
    }

    [Fact]
    public async Task Ganar_una_venta_nacional_no_crea_tracking_ni_guarda_trm()
    {
        var mundo = new CrmEnMemoria();
        var servicio = mundo.OportunidadService();
        var creada = await servicio.CrearAsync(mundo.Oportunidad(TiposVenta.Nacional, Monedas.Cop), mundo.ComoAna);

        var ganada = await servicio.GanarAsync(
            creada.Valor.Id, new GanarOportunidadRequest(null, null, OrigenesCanal.MostradorBodega, 4100m, EstadosPago.Pagado),
            mundo.ComoAna);

        Assert.Null(ganada.Valor.TrackingId);
        Assert.Empty(mundo.Tracking);
        Assert.Null(mundo.Ventas[ganada.Valor.VentaId].Trm);
    }

    [Fact]
    public async Task Ganar_en_usd_exige_una_trm_razonable()
    {
        var mundo = new CrmEnMemoria();
        var servicio = mundo.OportunidadService();
        var creada = await servicio.CrearAsync(mundo.Oportunidad(), mundo.ComoAna);

        var sinTrm = await servicio.GanarAsync(
            creada.Valor.Id, new GanarOportunidadRequest(null, null, OrigenesCanal.B2BDirecto, null, null), mundo.ComoAna);
        var trmMal = await servicio.GanarAsync(
            creada.Valor.Id, new GanarOportunidadRequest(null, null, OrigenesCanal.B2BDirecto, 4.1m, null), mundo.ComoAna);

        Assert.Equal(TipoError.Validacion, sinTrm.Error!.Tipo);
        Assert.Equal(TipoError.Validacion, trmMal.Error!.Tipo);
        Assert.Empty(mundo.Ventas);
    }

    [Fact]
    public async Task Si_otro_movio_la_etapa_el_cambio_se_rechaza()
    {
        var mundo = new CrmEnMemoria();
        var servicio = mundo.OportunidadService();
        var creada = await servicio.CrearAsync(mundo.Oportunidad(), mundo.ComoAna);
        await servicio.CambiarEtapaAsync(creada.Valor.Id, new CambiarEtapaRequest(null, EtapasPipeline.PreInvoice, null), mundo.ComoAna);

        var tardio = await servicio.CambiarEtapaAsync(
            creada.Valor.Id, new CambiarEtapaRequest(EtapasPipeline.Prospeccion, EtapasPipeline.Stocklist, null), mundo.ComoAna);

        Assert.Equal(TipoError.Conflicto, tardio.Error!.Tipo);
        Assert.Equal(EtapasPipeline.PreInvoice, mundo.Oportunidades[creada.Valor.Id].Etapa);
    }

    [Fact]
    public async Task Una_oportunidad_ganada_solo_admite_editar_notas_y_no_se_elimina()
    {
        var mundo = new CrmEnMemoria();
        var servicio = mundo.OportunidadService();
        var creada = await servicio.CrearAsync(mundo.Oportunidad(), mundo.ComoAna);
        await servicio.GanarAsync(
            creada.Valor.Id, new GanarOportunidadRequest(null, null, OrigenesCanal.B2BDirecto, 4100m, null), mundo.ComoAna);

        await servicio.ActualizarAsync(
            creada.Valor.Id, mundo.Oportunidad() with { MontoEstimado = 1m, NotasSeguimiento = "Cliente feliz" }, mundo.ComoAna);

        var o = mundo.Oportunidades[creada.Valor.Id];
        Assert.Equal(4850m, o.MontoEstimado);
        Assert.Equal("Cliente feliz", o.NotasSeguimiento);
        Assert.Equal(TipoError.Conflicto, (await servicio.EliminarAsync(creada.Valor.Id, mundo.ComoAna)).Error!.Tipo);
    }
}

public class VentaYTrackingTests
{
    [Fact]
    public async Task Una_venta_nacional_descarta_la_guia_y_una_de_exportacion_la_direccion()
    {
        var mundo = new CrmEnMemoria();
        var servicio = mundo.VentaService();
        GuardarVentaRequest Solicitud(string tipo, string moneda, decimal? trm) => new(
            mundo.Cliente.Id, null, tipo, OrigenesCanal.WhatsappDirecto, null, moneda, 1000m, trm, 2, "Mensajería local",
            "157-123", null, null, "Bogotá", "Cl 195 # 50-65", null);

        var nacional = await servicio.CrearAsync(Solicitud(TiposVenta.Nacional, Monedas.Cop, 4100m), mundo.ComoAna);
        var exportacion = await servicio.CrearAsync(Solicitud(TiposVenta.Exportacion, Monedas.Usd, 4100m), mundo.ComoAna);

        var vn = mundo.Ventas[nacional.Valor.Id];
        Assert.Null(vn.NoGuiaAwb);
        Assert.Null(vn.Trm);
        Assert.Equal("Cl 195 # 50-65", vn.DireccionEntrega);
        var ve = mundo.Ventas[exportacion.Valor.Id];
        Assert.Equal("157-123", ve.NoGuiaAwb);
        Assert.Null(ve.DireccionEntrega);
        Assert.Single(mundo.Tracking.Values, t => t.VentaId == ve.Id);
    }

    [Theory]
    [InlineData(5608.84, 168.27, 3.00)]
    [InlineData(4850, 194, 4.00)]
    [InlineData(0, 0, null)]
    public void El_porcentaje_doa_es_doa_sobre_fob(double fob, double doa, double? esperado) =>
        Assert.Equal((decimal?)esperado, TrackingService.PorcentajeDoa((decimal)fob, (decimal)doa));

    [Fact]
    public async Task No_se_registra_doa_sin_fob()
    {
        var mundo = new CrmEnMemoria();
        var venta = await mundo.VentaService().CrearAsync(
            new GuardarVentaRequest(mundo.Cliente.Id, null, TiposVenta.Exportacion, OrigenesCanal.B2BDirecto, null, Monedas.Cop,
                0m, null, 1, null, null, null, null, null, null, null),
            mundo.ComoAna);
        var trackingId = mundo.Tracking.Values.Single().Id;

        var resultado = await mundo.TrackingService().ActualizarAsync(
            trackingId, new GuardarTrackingRequest(null, null, null, null, 1, FasesTracking.Arribo, 0m, 0m, 50m, null),
            mundo.ComoAna);

        Assert.True(venta.Exitoso);
        Assert.Equal(TipoError.Validacion, resultado.Error!.Tipo);
    }
}

public class TareaYMetricasTests
{
    [Fact]
    public async Task Una_tarea_vencida_sin_completar_va_en_rojo_y_al_completarla_sale()
    {
        var mundo = new CrmEnMemoria();
        var servicio = mundo.TareaService();
        var creada = await servicio.CrearAsync(
            new GuardarTareaRequest(mundo.Cliente.Id, null, "Enviar stocklist", TiposAccion.StocklistFotos, null, Prioridades.Alta,
                null, mundo.Hoy.AddDays(-5), mundo.Hoy.AddDays(-2), null),
            mundo.ComoAna);

        var antes = (await servicio.ObtenerAsync(creada.Valor.Id, mundo.ComoAna)).Valor;
        await servicio.CambiarEstadoAsync(
            creada.Valor.Id, new CambiarEstadoTareaRequest(EstadosTarea.Pendiente, EstadosTarea.Completada), mundo.ComoAna);
        var despues = (await servicio.ObtenerAsync(creada.Valor.Id, mundo.ComoAna)).Valor;

        Assert.Equal(-2, antes.DiasRestantes);
        Assert.True(antes.Vencida);
        Assert.False(despues.Vencida);
        Assert.Equal(mundo.Ahora, despues.CompletadaEn);
    }

    [Fact]
    public async Task Una_tarea_no_vence_antes_de_asignarse()
    {
        var mundo = new CrmEnMemoria();

        var resultado = await mundo.TareaService().CrearAsync(
            new GuardarTareaRequest(mundo.Cliente.Id, null, "Cobrar", TiposAccion.CobroCartera, null, Prioridades.Media, null,
                mundo.Hoy, mundo.Hoy.AddDays(-1), null),
            mundo.ComoAna);

        Assert.Equal(TipoError.Validacion, resultado.Error!.Tipo);
    }

    [Fact]
    public void El_pool_es_el_porcentaje_de_la_venta_neta_sin_doa_ni_flete_repartido_entre_quienes_tienen_meta()
    {
        var ana = Guid.CreateVersion7();
        var beto = Guid.CreateVersion7();
        var carla = Guid.CreateVersion7();
        IReadOnlyList<MetaMesFila> metas =
        [
            new(ana, "Ana", Guid.CreateVersion7(), 60_000_000m),
            new(beto, "Beto", Guid.CreateVersion7(), 60_000_000m),
            new(carla, "Carla", null, null),
        ];

        var (neta, pool, porComercial) = MetricasService.CalcularComisiones(
            100_000_000m, 4_000_000m, 11_000_000m, 10m, metas, []);

        Assert.Equal(85_000_000m, neta);
        Assert.Equal(8_500_000m, pool);
        Assert.Equal(2, porComercial.Count);
        Assert.Equal(4_250_000m, porComercial[ana]);
        Assert.False(porComercial.ContainsKey(carla));
    }

    [Fact]
    public void Sin_metas_el_pool_se_reparte_entre_todos_los_comerciales()
    {
        IReadOnlyList<MetaMesFila> metas =
        [
            new(Guid.CreateVersion7(), "Ana", null, null),
            new(Guid.CreateVersion7(), "Beto", null, null),
            new(Guid.CreateVersion7(), "Carla", null, null),
        ];

        var (_, pool, porComercial) = MetricasService.CalcularComisiones(180_000_000m, 0m, 0m, 10m, metas, []);

        Assert.Equal(18_000_000m, pool);
        Assert.All(porComercial.Values, parte => Assert.Equal(6_000_000m, parte));
    }
}

public class ClienteServiceTests
{
    [Fact]
    public async Task Importar_con_codigo_lo_conserva_y_sube_el_consecutivo()
    {
        var mundo = new CrmEnMemoria();
        var servicio = mundo.ClienteService();
        GuardarClienteRequest Solicitud(string nombre, string? codigo) => new(
            codigo, nombre, "Erick", "EPOOLE@Ornamental.com", "000-1703895799", "Estados Unidos", "Atlanta", "atl",
            TiposCliente.B2BInternacional, LicenciasImportacion.Activa, EstadosCliente.Activo, null);

        var importado = await servicio.CrearAsync(Solicitud("Ornamental Imports", "cli-047"));
        var nuevo = await servicio.CrearAsync(Solicitud("Otro cliente", null));

        Assert.Equal("CLI-047", importado.Valor.Codigo);
        Assert.Equal("CLI-048", nuevo.Valor.Codigo);
        var c = mundo.Clientes[importado.Valor.Id];
        Assert.Equal("ATL", c.AeropuertoIata);
        Assert.Equal("epoole@ornamental.com", c.Email);
    }

    [Fact]
    public async Task Un_cliente_con_oportunidades_no_se_elimina()
    {
        var mundo = new CrmEnMemoria();
        await mundo.OportunidadService().CrearAsync(mundo.Oportunidad(), mundo.ComoAna);

        var resultado = await mundo.ClienteService().EliminarAsync(mundo.Cliente.Id);

        Assert.Equal(TipoError.Conflicto, resultado.Error!.Tipo);
    }
}
