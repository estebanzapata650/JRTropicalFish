using System.Text.Json;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Catalogos;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Domain.Catalogos;

namespace JRTropicalFish.Tests.Catalogos;

public class ReglasPrecioTests
{
    private static readonly ParametrosPreciosInfo Parametros = new(19m, 3.15m, 3150m, null);

    private static ListaPreciosFila Lista(decimal salvaje, decimal criadero, bool impuestos, decimal redondeo) =>
        new(Guid.NewGuid(), "X", "X", "COP", false, null, null, 0, true, Guid.NewGuid(), "Base", salvaje, criadero, salvaje,
            impuestos, redondeo, 0);

    /// <summary>Casos reales del Excel «JR MAIN STOCK LIST» (costo → mayorista nacional).</summary>
    [Theory]
    [InlineData(3000, TiposProducto.Criadero, 4800)]
    [InlineData(2000, TiposProducto.Criadero, 3200)]
    [InlineData(10000, TiposProducto.Criadero, 15950)]
    [InlineData(40000, TiposProducto.Criadero, 63850)]
    public void El_mayorista_nacional_es_costo_por_factor_iva_y_pasarela(decimal costo, string tipo, decimal esperado) =>
        Assert.Equal(esperado, ReglasPrecio.Calcular(costo, tipo, Lista(2m, 1.3m, impuestos: true, 50m), Parametros));

    [Theory]
    [InlineData(4800, TiposProducto.Criadero, 7200)]
    [InlineData(15950, TiposProducto.Criadero, 23950)]
    [InlineData(4800, TiposProducto.Salvaje, 9600)]
    public void El_detal_nacional_es_mayorista_por_factor_del_tipo(decimal mayorista, string tipo, decimal esperado) =>
        Assert.Equal(esperado, ReglasPrecio.Calcular(mayorista, tipo, Lista(2m, 1.5m, impuestos: false, 50m), Parametros));

    [Fact]
    public void Minoristas_del_exterior_es_standard_por_1_2_a_centavos() =>
        Assert.Equal(1.56m, ReglasPrecio.Calcular(1.3m, TiposProducto.Salvaje, Lista(1.2m, 1.2m, impuestos: false, 0.01m), Parametros));

    [Fact]
    public void Sin_base_o_sin_factor_para_el_tipo_no_hay_precio()
    {
        var lista = Lista(2m, 1.3m, impuestos: false, 50m) with { FactorCriadero = null };

        Assert.Null(ReglasPrecio.Calcular(null, TiposProducto.Salvaje, lista, Parametros));
        Assert.Null(ReglasPrecio.Calcular(0m, TiposProducto.Salvaje, lista, Parametros));
        Assert.Null(ReglasPrecio.Calcular(1000m, TiposProducto.Criadero, lista, Parametros));
    }
}

public class ProductoServiceTests
{
    [Fact]
    public async Task Sin_codigo_se_asigna_el_siguiente_del_grupo_de_5_en_5()
    {
        var mundo = new CatalogosEnMemoria();
        var servicio = mundo.ProductoService();

        var primero = await servicio.CrearAsync(mundo.Solicitud("PC0160"), verCostos: false);
        var siguiente = await servicio.CrearAsync(mundo.Solicitud() with { NombreCientifico = "Otro pez" }, verCostos: false);

        Assert.True(primero.Exitoso);
        Assert.Equal("PC0165", siguiente.Valor.Codigo);
    }

    [Fact]
    public async Task El_codigo_debe_llevar_el_prefijo_del_grupo_y_no_repetirse()
    {
        var mundo = new CatalogosEnMemoria();
        var servicio = mundo.ProductoService();
        await servicio.CrearAsync(mundo.Solicitud("PC0005"), verCostos: false);

        var otroPrefijo = await servicio.CrearAsync(mundo.Solicitud("AP005"), verCostos: false);
        var repetido = await servicio.CrearAsync(mundo.Solicitud("pc0005"), verCostos: false);

        Assert.Equal(TipoError.Validacion, otroPrefijo.Error!.Tipo);
        Assert.Equal(TipoError.Conflicto, repetido.Error!.Tipo);
    }

    [Fact]
    public async Task Sin_permiso_de_costos_no_se_ve_ni_se_fija_el_costo()
    {
        var mundo = new CatalogosEnMemoria();
        var servicio = mundo.ProductoService();
        var costo = mundo.ListaPorCodigo("COSTO");
        var standard = mundo.ListaPorCodigo("STANDARD");
        var creado = await servicio.CrearAsync(
            mundo.Solicitud(precios: new Dictionary<Guid, decimal?> { [costo.Id] = 3000m, [standard.Id] = 1m }), verCostos: true);

        var detalle = (await servicio.ObtenerAsync(creado.Valor.Id, verCostos: false)).Valor;
        var intento = await servicio.ActualizarAsync(
            creado.Valor.Id, mundo.Solicitud(precios: new Dictionary<Guid, decimal?> { [costo.Id] = 1m }), verCostos: false);
        var historial = (await servicio.HistorialAsync(creado.Valor.Id, verCostos: false)).Valor;

        Assert.False(detalle.Precios.ContainsKey(costo.Id));
        Assert.Equal(1m, detalle.Precios[standard.Id]);
        Assert.Equal(TipoError.Prohibido, intento.Error!.Tipo);
        Assert.Equal(3000m, mundo.Precios[(creado.Valor.Id, costo.Id)]);
        Assert.DoesNotContain(
            historial.SelectMany(r => JsonSerializer.Deserialize<List<CambioAuditado>>(r.CambiosJson)!),
            c => c.Campo.EndsWith("(COSTO)", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Solo_cambian_los_precios_indicados_y_cero_quita_el_precio()
    {
        var mundo = new CatalogosEnMemoria();
        var servicio = mundo.ProductoService();
        var standard = mundo.ListaPorCodigo("STANDARD");
        var minorista = mundo.ListaPorCodigo("MINORISTA_EXTERIOR");
        var creado = await servicio.CrearAsync(
            mundo.Solicitud(precios: new Dictionary<Guid, decimal?> { [standard.Id] = 1.3m, [minorista.Id] = 1.56m }), verCostos: false);

        await servicio.ActualizarAsync(
            creado.Valor.Id, mundo.Solicitud(precios: new Dictionary<Guid, decimal?> { [minorista.Id] = 0m }), verCostos: false);

        Assert.Equal(1.3m, mundo.Precios[(creado.Valor.Id, standard.Id)]);
        Assert.False(mundo.Precios.ContainsKey((creado.Valor.Id, minorista.Id)));
    }

    [Fact]
    public async Task Talla_y_bolsa_validan_sus_rangos_y_maximo_tres_centros()
    {
        var mundo = new CatalogosEnMemoria();
        var servicio = mundo.ProductoService();
        var otros = Enumerable.Range(0, 3).Select(i =>
        {
            var c = new CentroAcopio { Nombre = $"Centro {i}", CuencaId = mundo.Cuencas[0].Id };
            mundo.Centros[c.Id] = c;
            return c.Id;
        });

        var talla = await servicio.CrearAsync(mundo.Solicitud() with { TallaMinCm = 6, TallaMaxCm = 5 }, false);
        var bolsa = await servicio.CrearAsync(mundo.Solicitud() with { BolsaMin = 60, BolsaMax = 50 }, false);
        var centros = await servicio.CrearAsync(mundo.Solicitud() with { CentrosAcopio = [mundo.Leticia.Id, .. otros] }, false);

        Assert.Equal(TipoError.Validacion, talla.Error!.Tipo);
        Assert.Equal(TipoError.Validacion, bolsa.Error!.Tipo);
        Assert.Equal(TipoError.Validacion, centros.Error!.Tipo);
    }
}

public class PrecioServiceTests
{
    [Fact]
    public async Task Recalcular_aplica_la_regla_a_los_productos_con_base()
    {
        var mundo = new CatalogosEnMemoria();
        var productos = mundo.ProductoService();
        var costo = mundo.ListaPorCodigo("COSTO");
        var mayorista = mundo.ListaPorCodigo("MAYORISTA_NACIONAL");
        var conCosto = await productos.CrearAsync(mundo.Solicitud(precios: new Dictionary<Guid, decimal?> { [costo.Id] = 3000m }), true);
        await productos.CrearAsync(mundo.Solicitud() with { NombreCientifico = "Sin costo" }, true);

        var resultado = await mundo.PrecioService().RecalcularAsync(mayorista.Id, verCostos: true);

        Assert.Equal(new ResultadoRecalculo(1, 1, 1), resultado.Valor);
        Assert.Equal(4800m, mundo.Precios[(conCosto.Valor.Id, mayorista.Id)]);
    }

    [Fact]
    public async Task Una_lista_no_puede_depender_de_si_misma_ni_de_otra_moneda()
    {
        var mundo = new CatalogosEnMemoria();
        var servicio = mundo.PrecioService();
        var standard = mundo.ListaPorCodigo("STANDARD");
        var minorista = mundo.ListaPorCodigo("MINORISTA_EXTERIOR");
        var costo = mundo.ListaPorCodigo("COSTO");
        GuardarListaPreciosRequest Solicitud(Guid? baseId) =>
            new(null, "Standard", "USD", false, null, 0, true, baseId, 1.1m, 1.1m, 1.1m, false, 0.01m);

        var ciclo = await servicio.ActualizarListaAsync(standard.Id, Solicitud(minorista.Id), verCostos: true);
        var moneda = await servicio.ActualizarListaAsync(standard.Id, Solicitud(costo.Id), verCostos: true);

        Assert.Equal(TipoError.Validacion, ciclo.Error!.Tipo);
        Assert.Equal(TipoError.Validacion, moneda.Error!.Tipo);
    }

    [Fact]
    public async Task Las_listas_de_costo_no_existen_sin_permiso()
    {
        var mundo = new CatalogosEnMemoria();
        var servicio = mundo.PrecioService();

        var listas = await servicio.ListasAsync(verCostos: false);
        var recalculo = await servicio.RecalcularAsync(mundo.ListaPorCodigo("COSTO").Id, verCostos: false);

        Assert.DoesNotContain(listas, l => l.EsCosto);
        Assert.Equal(TipoError.NoEncontrado, recalculo.Error!.Tipo);
    }
}

public class ProveedorTests
{
    [Theory]
    [InlineData("8.028.183-5", "8028183-5")]
    [InlineData(" 1121707658 ", "1121707658")]
    public void La_identificacion_se_normaliza_sin_puntos_ni_espacios(string entrada, string esperado) =>
        Assert.Equal(esperado, ProveedorService.NormalizarIdentificacion(entrada));
}
