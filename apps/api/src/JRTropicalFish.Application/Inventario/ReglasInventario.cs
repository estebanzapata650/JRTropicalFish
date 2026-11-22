using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Domain.Inventario;

namespace JRTropicalFish.Application.Inventario;

/// <summary>
/// Qué operaciones de stock son válidas (regla pura; la base repite la de "no negativo"). Todas son
/// la misma primitiva origen → destino; aquí se decide qué combinación corresponde a cada tipo.
/// </summary>
public static class ReglasStock
{
    public const int CantidadMaxima = 1_000_000;

    /// <summary>Tipos que se registran a mano desde Existencias (no recepción, saldo inicial ni despacho).</summary>
    public static readonly IReadOnlyList<string> TiposManuales =
    [
        TiposMovimiento.Ubicacion, TiposMovimiento.Traslado, TiposMovimiento.Enfermo, TiposMovimiento.Aliviado,
        TiposMovimiento.Mortalidad, TiposMovimiento.Ajuste,
    ];

    public static Error? Validar(OperacionStock o, bool puedeAjustar)
    {
        if (o.Tipo is null || !TiposManuales.Contains(o.Tipo))
        {
            return Error.Validacion("Ese tipo de movimiento no se registra a mano.");
        }

        if (o.Cantidad is <= 0 or > CantidadMaxima)
        {
            return Error.Validacion("La cantidad debe ser mayor que cero.");
        }

        var origen = o.CondicionOrigen is not null;
        var destino = o.CondicionDestino is not null;
        if ((origen && !Condiciones.Todos.Contains(o.CondicionOrigen!)) || (destino && !Condiciones.Todos.Contains(o.CondicionDestino!)))
        {
            return Error.Validacion("La condición debe ser sano o enfermo.");
        }

        return o.Tipo switch
        {
            TiposMovimiento.Ubicacion when !origen || o.AcuarioOrigenId is not null || !destino || o.AcuarioDestinoId is null
                => Error.Validacion("Ubicar es pasar lo que está por ubicar a un acuario."),
            TiposMovimiento.Ubicacion when o.CondicionOrigen != o.CondicionDestino
                => Error.Validacion("Al ubicar no cambia la condición."),
            TiposMovimiento.Traslado when !origen || !destino || o.AcuarioOrigenId is null || o.AcuarioDestinoId is null
                => Error.Validacion("Un traslado va de un acuario a otro."),
            TiposMovimiento.Traslado when o.AcuarioOrigenId == o.AcuarioDestinoId
                => Error.Validacion("El acuario de destino debe ser distinto."),
            TiposMovimiento.Traslado when o.CondicionOrigen != o.CondicionDestino
                => Error.Validacion("Un traslado no cambia la condición; usa «enfermo» o «aliviado»."),
            TiposMovimiento.Enfermo when o.CondicionOrigen != Condiciones.Sano || o.CondicionDestino != Condiciones.Enfermo
                => Error.Validacion("«Enfermo» pasa peces sanos a enfermos."),
            TiposMovimiento.Aliviado when o.CondicionOrigen != Condiciones.Enfermo || o.CondicionDestino != Condiciones.Sano
                => Error.Validacion("«Aliviado» pasa peces enfermos a sanos."),
            TiposMovimiento.Mortalidad when !origen || destino
                => Error.Validacion("La mortalidad descuenta de una existencia."),
            TiposMovimiento.Ajuste when !puedeAjustar
                => Error.Prohibido("No tienes permiso para hacer ajustes de inventario."),
            TiposMovimiento.Ajuste when origen == destino
                => Error.Validacion("Un ajuste suma (solo destino) o resta (solo origen)."),
            TiposMovimiento.Ajuste when string.IsNullOrWhiteSpace(o.Nota)
                => Error.Validacion("Indica el motivo del ajuste."),
            _ => null,
        };
    }
}

/// <summary>Transiciones de estado manuales de los pedidos (las demás las pone la recepción o el despacho).</summary>
public static class ReglasPedido
{
    private static readonly Dictionary<string, string[]> Proveedor = new()
    {
        [EstadosPedidoProveedor.Borrador] = [EstadosPedidoProveedor.Enviado, EstadosPedidoProveedor.Cancelado],
        [EstadosPedidoProveedor.Enviado] = [EstadosPedidoProveedor.EnTransito, EstadosPedidoProveedor.Cancelado],
        [EstadosPedidoProveedor.EnTransito] = [EstadosPedidoProveedor.Cancelado],
        // Lo que no llegó se da por perdido: el pedido se cierra como recibido.
        [EstadosPedidoProveedor.RecibidoParcial] = [EstadosPedidoProveedor.Recibido],
    };

    private static readonly Dictionary<string, string[]> Cliente = new()
    {
        [EstadosPedidoCliente.Borrador] = [EstadosPedidoCliente.Confirmado, EstadosPedidoCliente.Cancelado],
        [EstadosPedidoCliente.Confirmado] =
            [EstadosPedidoCliente.Borrador, EstadosPedidoCliente.EnPreparacion, EstadosPedidoCliente.Cancelado],
        // Cerrar con faltantes: lo despachado es el real enviado.
        [EstadosPedidoCliente.EnPreparacion] = [EstadosPedidoCliente.Despachado],
    };

    public static IReadOnlyList<string> SiguientesProveedor(string actual) => Proveedor.GetValueOrDefault(actual) ?? [];

    public static IReadOnlyList<string> SiguientesCliente(string actual) => Cliente.GetValueOrDefault(actual) ?? [];

    public static Error? ValidarProveedor(string actual, string? nuevo) =>
        nuevo is not null && SiguientesProveedor(actual).Contains(nuevo)
            ? null
            : Error.Validacion($"El pedido no puede pasar de «{actual}» a «{nuevo}».");

    public static Error? ValidarCliente(string actual, string? nuevo, int despachadas)
    {
        if (nuevo is null || !SiguientesCliente(actual).Contains(nuevo))
        {
            return Error.Validacion($"El pedido no puede pasar de «{actual}» a «{nuevo}».");
        }

        return despachadas > 0 && nuevo is EstadosPedidoCliente.Borrador or EstadosPedidoCliente.Cancelado
            ? Error.Conflicto("El pedido ya tiene despachos: no se puede devolver a borrador ni cancelar.")
            : null;
    }

    /// <summary>Un pedido de cliente se edita en borrador o confirmado.</summary>
    public static bool ClienteEditable(string estado) => estado is EstadosPedidoCliente.Borrador or EstadosPedidoCliente.Confirmado;

    /// <summary>Un pedido a proveedor se edita hasta su primera recepción.</summary>
    public static bool ProveedorEditable(string estado, int recibidas) =>
        recibidas == 0 && estado is EstadosPedidoProveedor.Borrador or EstadosPedidoProveedor.Enviado or EstadosPedidoProveedor.EnTransito;
}

/// <summary>Traduce los SQLSTATE de inventario a errores de negocio.</summary>
public static class FallasInventario
{
    public static Error AError(FallaBaseDatos falla) => falla.SqlState switch
    {
        "JR001" => Error.Conflicto("Otra persona cambió este registro mientras tanto. Recarga la página."),
        "JR002" => Error.Conflicto($"No hay suficiente stock: {falla.Mensaje}"),
        "JR003" => Error.Validacion(falla.Mensaje),
        "JR004" => Error.Conflicto(falla.Mensaje),
        _ => Error.Validacion(falla.Mensaje),
    };
}
