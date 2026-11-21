using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Inventario;
using JRTropicalFish.Domain.Inventario;

namespace JRTropicalFish.Tests.Inventario;

public class ReglasStockTests
{
    private static readonly Guid Lote = Guid.NewGuid();
    private static readonly Guid A = Guid.NewGuid();
    private static readonly Guid B = Guid.NewGuid();

    private static OperacionStock Op(
        string tipo, Guid? origen, string? condOrigen, Guid? destino, string? condDestino, int cantidad = 10, string? nota = null) =>
        new(tipo, Lote, origen, condOrigen, destino, condDestino, cantidad, nota);

    public static TheoryData<OperacionStock> Validas => new()
    {
        Op(TiposMovimiento.Ubicacion, null, Condiciones.Sano, A, Condiciones.Sano),
        Op(TiposMovimiento.Ubicacion, null, Condiciones.Enfermo, A, Condiciones.Enfermo),
        Op(TiposMovimiento.Traslado, A, Condiciones.Sano, B, Condiciones.Sano),
        Op(TiposMovimiento.Enfermo, A, Condiciones.Sano, A, Condiciones.Enfermo),
        Op(TiposMovimiento.Enfermo, A, Condiciones.Sano, B, Condiciones.Enfermo),
        Op(TiposMovimiento.Aliviado, B, Condiciones.Enfermo, A, Condiciones.Sano),
        Op(TiposMovimiento.Mortalidad, A, Condiciones.Enfermo, null, null),
    };

    [Theory]
    [MemberData(nameof(Validas))]
    public void Las_operaciones_de_bodega_validas_pasan(OperacionStock operacion) =>
        Assert.Null(ReglasStock.Validar(operacion, puedeAjustar: false));

    public static TheoryData<OperacionStock> Invalidas => new()
    {
        Op(TiposMovimiento.Ubicacion, A, Condiciones.Sano, B, Condiciones.Sano),           // ubicar es desde «por ubicar»
        Op(TiposMovimiento.Ubicacion, null, Condiciones.Sano, null, Condiciones.Sano),     // ubicar exige acuario de destino
        Op(TiposMovimiento.Ubicacion, null, Condiciones.Sano, A, Condiciones.Enfermo),     // ubicar no cambia la condición
        Op(TiposMovimiento.Traslado, A, Condiciones.Sano, A, Condiciones.Sano),            // mismo acuario
        Op(TiposMovimiento.Traslado, A, Condiciones.Sano, B, Condiciones.Enfermo),         // traslado no cambia la condición
        Op(TiposMovimiento.Enfermo, A, Condiciones.Enfermo, A, Condiciones.Sano),          // al revés es «aliviado»
        Op(TiposMovimiento.Aliviado, A, Condiciones.Sano, A, Condiciones.Sano),
        Op(TiposMovimiento.Mortalidad, A, Condiciones.Sano, B, Condiciones.Sano),          // la mortalidad no tiene destino
        Op(TiposMovimiento.Despacho, A, Condiciones.Sano, null, null),                     // el despacho va por el pedido
        Op(TiposMovimiento.Entrada, null, null, A, Condiciones.Sano),                      // la entrada va por la recepción
        Op(TiposMovimiento.Traslado, A, Condiciones.Sano, B, Condiciones.Sano, cantidad: 0),
        Op(TiposMovimiento.Traslado, A, "muerto", B, "muerto"),
    };

    [Theory]
    [MemberData(nameof(Invalidas))]
    public void Las_operaciones_invalidas_se_rechazan(OperacionStock operacion) =>
        Assert.Equal(TipoError.Validacion, ReglasStock.Validar(operacion, puedeAjustar: true)!.Tipo);

    [Fact]
    public void El_ajuste_exige_permiso_motivo_y_un_solo_lado()
    {
        var suma = Op(TiposMovimiento.Ajuste, null, null, A, Condiciones.Sano, nota: "Conteo físico");

        Assert.Equal(TipoError.Prohibido, ReglasStock.Validar(suma, puedeAjustar: false)!.Tipo);
        Assert.Null(ReglasStock.Validar(suma, puedeAjustar: true));
        Assert.NotNull(ReglasStock.Validar(suma with { Nota = " " }, puedeAjustar: true));
        Assert.NotNull(ReglasStock.Validar(Op(TiposMovimiento.Ajuste, A, Condiciones.Sano, B, Condiciones.Sano, nota: "x"), puedeAjustar: true));
    }
}

public class ReglasPedidoTests
{
    [Theory]
    [InlineData(EstadosPedidoProveedor.Borrador, EstadosPedidoProveedor.Enviado, true)]
    [InlineData(EstadosPedidoProveedor.Enviado, EstadosPedidoProveedor.EnTransito, true)]
    [InlineData(EstadosPedidoProveedor.RecibidoParcial, EstadosPedidoProveedor.Recibido, true)]
    [InlineData(EstadosPedidoProveedor.Enviado, EstadosPedidoProveedor.Recibido, false)]
    [InlineData(EstadosPedidoProveedor.Recibido, EstadosPedidoProveedor.Cancelado, false)]
    [InlineData(EstadosPedidoProveedor.EnTransito, EstadosPedidoProveedor.Borrador, false)]
    public void Pedido_a_proveedor_solo_cambia_por_transiciones_validas(string actual, string nuevo, bool valida) =>
        Assert.Equal(valida, ReglasPedido.ValidarProveedor(actual, nuevo) is null);

    [Fact]
    public void Un_pedido_de_cliente_con_despachos_no_vuelve_a_borrador_ni_se_cancela()
    {
        Assert.Null(ReglasPedido.ValidarCliente(EstadosPedidoCliente.Confirmado, EstadosPedidoCliente.Cancelado, despachadas: 0));
        Assert.Equal(
            TipoError.Conflicto,
            ReglasPedido.ValidarCliente(EstadosPedidoCliente.Confirmado, EstadosPedidoCliente.Cancelado, despachadas: 5)!.Tipo);
        Assert.Null(ReglasPedido.ValidarCliente(EstadosPedidoCliente.EnPreparacion, EstadosPedidoCliente.Despachado, despachadas: 5));
        Assert.NotNull(ReglasPedido.ValidarCliente(EstadosPedidoCliente.Despachado, EstadosPedidoCliente.Confirmado, despachadas: 5));
    }

    [Fact]
    public void Un_pedido_a_proveedor_se_edita_hasta_su_primera_recepcion()
    {
        Assert.True(ReglasPedido.ProveedorEditable(EstadosPedidoProveedor.EnTransito, recibidas: 0));
        Assert.False(ReglasPedido.ProveedorEditable(EstadosPedidoProveedor.EnTransito, recibidas: 3));
        Assert.False(ReglasPedido.ProveedorEditable(EstadosPedidoProveedor.Cancelado, recibidas: 0));
    }

    [Fact]
    public void Los_errores_de_la_base_se_traducen_a_errores_de_negocio()
    {
        Assert.Equal(TipoError.Conflicto, FallasInventario.AError(new FallaBaseDatos("JR001", "")).Tipo);
        var sinStock = FallasInventario.AError(new FallaBaseDatos("JR002", "Solo hay 13 disponibles en ese origen."));
        Assert.Equal(TipoError.Conflicto, sinStock.Tipo);
        Assert.Contains("13", sinStock.Mensaje, StringComparison.Ordinal);
        Assert.Equal(TipoError.Validacion, FallasInventario.AError(new FallaBaseDatos("JR003", "Solo quedan 2.")).Tipo);
    }
}

public class IndicadoresInventarioTests
{
    private static InventarioProductoFila Producto(int sanos, int porUbicar, int pendiente, int transito) =>
        new(Guid.NewGuid(), "AP005", "Crenicara punctulatum", null, null, null, sanos, porUbicar, 0, pendiente, transito, null, null, null);

    [Theory]
    [InlineData(10, 5, 100, 20, 65, false)]   // falta comprar 65
    [InlineData(50, 0, 60, 10, 0, true)]      // stock + tránsito lo cubren: «ya pedido»
    [InlineData(0, 0, 0, 0, 0, false)]        // sin demanda no hay nada que cubrir
    public void El_faltante_es_la_demanda_menos_stock_vendible_y_transito(
        int sanos, int porUbicar, int pendiente, int transito, int faltante, bool cubierto)
    {
        var p = Producto(sanos, porUbicar, pendiente, transito);

        Assert.Equal(faltante, p.Faltante);
        Assert.Equal(cubierto, p.Cubierto);
    }

    [Fact]
    public void La_trazabilidad_del_lote_calcula_lo_disponible_y_el_porcentaje_de_muertos()
    {
        var lote = new LoteFila(
            Guid.NewGuid(), "LOT-0001", Guid.NewGuid(), "AP005", "x", null, null, null, null, null, null,
            new DateOnly(2026, 11, 1), 20, null, Recibidos: 52, MuertosLlegada: 1, "recepcion", Sanos: 30, Enfermos: 2,
            PorUbicar: 0, Despachados: 15, Muertos: 2, DateTime.UtcNow);

        Assert.Equal(32, lote.Disponible);
        Assert.Equal(3.8m, lote.PorcentajeMuertos);
    }

    [Fact]
    public void El_cumplimiento_es_lo_entregado_sobre_lo_pedido()
    {
        var pedido = new PedidoClienteFila(
            Guid.NewGuid(), "PC-0001", Guid.NewGuid(), "c", Guid.NewGuid(), "v", null, null, null, new DateOnly(2026, 11, 1), null,
            EstadosPedidoCliente.Despachado, null, Unidades: 200, Despachadas: 170, null, DateTime.UtcNow);
        // Recibir de más (72 de 70 en una línea) no pasa del 100 %: el "en full" ya viene topado por línea.
        var compra = new CumplimientoCompraFila(
            Guid.NewGuid(), "PP-0001", Guid.NewGuid(), "Absalon", new DateOnly(2026, 11, 1), "recibido", Pedidas: 90, Recibidas: 82, RecibidasEnFull: 80);

        Assert.Equal(85m, pedido.Cumplimiento);
        Assert.Equal(88.9m, compra.Cumplimiento);
    }
}
