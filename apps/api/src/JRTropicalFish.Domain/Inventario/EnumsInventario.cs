namespace JRTropicalFish.Domain.Inventario;

/// <summary>Condición de los peces en una existencia. Lo enfermo nunca se vende.</summary>
public static class Condiciones
{
    public const string Sano = "sano";
    public const string Enfermo = "enfermo";

    public static readonly IReadOnlyList<string> Todos = [Sano, Enfermo];
}

/// <summary>
/// Tipos de movimiento del kardex. Todos son casos de la misma primitiva (<c>sp_inventario_mover</c>):
/// origen (lote, acuario, condición) o nada → destino o nada.
/// </summary>
public static class TiposMovimiento
{
    /// <summary>Nada → por ubicar (recepción de un pedido a proveedor).</summary>
    public const string Entrada = "entrada";

    /// <summary>Nada → existencia (carga inicial migrada del Excel).</summary>
    public const string SaldoInicial = "saldo_inicial";

    /// <summary>Por ubicar → acuario.</summary>
    public const string Ubicacion = "ubicacion";

    /// <summary>Acuario → otro acuario, misma condición.</summary>
    public const string Traslado = "traslado";

    /// <summary>Sano → enfermo (mismo u otro acuario).</summary>
    public const string Enfermo = "enfermo";

    /// <summary>Enfermo → sano.</summary>
    public const string Aliviado = "aliviado";

    /// <summary>Existencia → nada (muertos, también los que llegan muertos en una recepción).</summary>
    public const string Mortalidad = "mortalidad";

    /// <summary>Existencia sana → nada, contra una línea de pedido de cliente.</summary>
    public const string Despacho = "despacho";

    /// <summary>Corrección manual con motivo (entra o sale).</summary>
    public const string Ajuste = "ajuste";

    public static readonly IReadOnlyList<string> Todos =
        [Entrada, SaldoInicial, Ubicacion, Traslado, Enfermo, Aliviado, Mortalidad, Despacho, Ajuste];
}

public static class EstadosOferta
{
    public const string Abierta = "abierta";
    public const string Cerrada = "cerrada";

    public static readonly IReadOnlyList<string> Todos = [Abierta, Cerrada];
}

/// <summary>Pedido a proveedor. Recibido parcial y recibido los pone la recepción, no el usuario.</summary>
public static class EstadosPedidoProveedor
{
    public const string Borrador = "borrador";
    public const string Enviado = "enviado";
    public const string EnTransito = "en_transito";
    public const string RecibidoParcial = "recibido_parcial";
    public const string Recibido = "recibido";
    public const string Cancelado = "cancelado";

    public static readonly IReadOnlyList<string> Todos = [Borrador, Enviado, EnTransito, RecibidoParcial, Recibido, Cancelado];

    /// <summary>Estados en los que la mercancía está "en camino" (cuenta como en tránsito).</summary>
    public static readonly IReadOnlyList<string> Pendientes = [Enviado, EnTransito, RecibidoParcial];
}

/// <summary>Pedido de cliente. Despachado lo pone el despacho al completar todas las líneas.</summary>
public static class EstadosPedidoCliente
{
    public const string Borrador = "borrador";
    public const string Confirmado = "confirmado";
    public const string EnPreparacion = "en_preparacion";
    public const string Despachado = "despachado";
    public const string Cancelado = "cancelado";

    public static readonly IReadOnlyList<string> Todos = [Borrador, Confirmado, EnPreparacion, Despachado, Cancelado];

    /// <summary>Estados cuyo pendiente cuenta como demanda (necesidades de compra, pedidos sin despachar).</summary>
    public static readonly IReadOnlyList<string> Activos = [Confirmado, EnPreparacion];
}
