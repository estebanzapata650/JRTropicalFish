using JRTropicalFish.Domain.Comun;

namespace JRTropicalFish.Domain.Inventario;

/// <summary>Acuario de la bodega (ubicación): código A101 = sector A, nivel 1, posición 01.</summary>
public class Acuario : EntidadBase
{
    public required string Codigo { get; set; }

    public required string Sector { get; set; }

    public int Nivel { get; set; }

    public int Posicion { get; set; }

    public decimal? CapacidadLitros { get; set; }

    public bool Activo { get; set; } = true;
}

/// <summary>Disponibilidad que ofrece un proveedor (pre-disponibilidad, paso 2 del proceso).</summary>
public class OfertaProveedor : EntidadBase
{
    public required string Codigo { get; set; }

    public Guid ProveedorId { get; set; }

    public DateOnly Fecha { get; set; }

    public DateOnly? FechaEstimadaLlegada { get; set; }

    public required string Estado { get; set; }

    public string? Observaciones { get; set; }
}

public class OfertaProveedorLinea
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid OfertaId { get; set; }

    public Guid ProductoId { get; set; }

    public int Cantidad { get; set; }
}

/// <summary>Pedido a un proveedor (paso 7). La recepción actualiza lo recibido y el estado.</summary>
public class PedidoProveedor : EntidadBase
{
    public required string Codigo { get; set; }

    public Guid ProveedorId { get; set; }

    public Guid? OfertaId { get; set; }

    public DateOnly Fecha { get; set; }

    public DateOnly? FechaEstimadaLlegada { get; set; }

    public required string Estado { get; set; }

    public string? Observaciones { get; set; }
}

public class PedidoProveedorLinea
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid PedidoId { get; set; }

    public Guid ProductoId { get; set; }

    public int Cantidad { get; set; }

    /// <summary>Costo unitario acordado (COP). Dato reservado (<c>catalogos:ver-costos</c>).</summary>
    public decimal? CostoUnitario { get; set; }

    public string? Observacion { get; set; }
}

/// <summary>Llegada de un pedido a proveedor (paso 8). Cada línea crea un lote.</summary>
public class Recepcion : EntidadBase
{
    public required string Codigo { get; set; }

    public Guid PedidoProveedorId { get; set; }

    public DateOnly Fecha { get; set; }

    public string? Observaciones { get; set; }

    public Guid? UsuarioId { get; set; }
}

public class RecepcionLinea
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid RecepcionId { get; set; }

    public Guid PedidoLineaId { get; set; }

    public Guid ProductoId { get; set; }

    /// <summary>Total que llegó (vivos sanos + enfermos + muertos).</summary>
    public int Recibida { get; set; }

    public int Enferma { get; set; }

    public int Muerta { get; set; }

    public decimal CostoUnitario { get; set; }

    /// <summary>Columna generada: recibida × costo unitario.</summary>
    public decimal CostoTotal { get; set; }

    public Guid LoteId { get; set; }
}

/// <summary>
/// Lote: lo que llegó de un producto en una recepción (o del saldo inicial migrado). Su stock vive en
/// <see cref="Existencia"/> y cambia solo por <see cref="MovimientoInventario"/>.
/// </summary>
public class Lote : EntidadBase
{
    public required string Codigo { get; set; }

    public Guid ProductoId { get; set; }

    public Guid? ProveedorId { get; set; }

    public Guid? CentroAcopioId { get; set; }

    public Guid? RecepcionLineaId { get; set; }

    public DateOnly FechaIngreso { get; set; }

    public decimal? CostoUnitario { get; set; }

    /// <summary>Cantidad que llegó (incluye enfermos y muertos).</summary>
    public int Recibidos { get; set; }

    public int MuertosLlegada { get; set; }

    /// <summary><c>recepcion</c> o <c>saldo_inicial</c>.</summary>
    public required string Origen { get; set; }
}

/// <summary>
/// Saldo de un lote en un acuario y condición. <see cref="AcuarioId"/> nulo = recibido y aún por
/// ubicar. Materializado por <c>sp_inventario_mover</c>; nunca se edita directamente.
/// </summary>
public class Existencia
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid LoteId { get; set; }

    public Guid? AcuarioId { get; set; }

    public required string Condicion { get; set; }

    public int Cantidad { get; set; }

    public DateTime ActualizadoEn { get; set; }
}

/// <summary>Kardex: inmutable. Se corrige con otro movimiento, nunca editando.</summary>
public class MovimientoInventario
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public required string Tipo { get; set; }

    public Guid LoteId { get; set; }

    public Guid? AcuarioOrigenId { get; set; }

    public string? CondicionOrigen { get; set; }

    public Guid? AcuarioDestinoId { get; set; }

    public string? CondicionDestino { get; set; }

    public int Cantidad { get; set; }

    public Guid? PedidoClienteLineaId { get; set; }

    public Guid? RecepcionLineaId { get; set; }

    public string? Nota { get; set; }

    public Guid? UsuarioId { get; set; }

    public string? UsuarioNombre { get; set; }

    public DateTime Fecha { get; set; }
}

/// <summary>Pedido de cliente con líneas por producto. Puede nacer de una venta del CRM.</summary>
public class PedidoCliente : EntidadBase
{
    public required string Codigo { get; set; }

    public Guid ClienteId { get; set; }

    public Guid ComercialId { get; set; }

    /// <summary>Venta del CRM a la que pertenece (una venta, un pedido).</summary>
    public Guid? VentaId { get; set; }

    public DateOnly Fecha { get; set; }

    public string? Ciudad { get; set; }

    public required string Estado { get; set; }

    public string? Observaciones { get; set; }
}

public class PedidoClienteLinea
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid PedidoId { get; set; }

    public Guid ProductoId { get; set; }

    public int Cantidad { get; set; }

    public decimal? PrecioUnitario { get; set; }

    public string? Observacion { get; set; }
}
