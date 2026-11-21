using JRTropicalFish.Domain.Comun;

namespace JRTropicalFish.Domain.Catalogos;

/// <summary>Origen del producto. Decide el factor de precio nacional (ver <see cref="ListaPrecios"/>).</summary>
public static class TiposProducto
{
    public const string Salvaje = "salvaje";
    public const string Criadero = "criadero";
    public const string Especial = "especial";

    public static readonly IReadOnlyList<string> Todos = [Salvaje, Criadero, Especial];
}

/// <summary>
/// Producto del catálogo (stock list): una especie en una presentación (talla). El código
/// (<c>AP020</c>, <c>PC0165</c>) lleva el prefijo de su grupo. El stock real llegará con Inventario;
/// mientras tanto <see cref="Disponibilidad"/> es lo que se publica a los clientes ("1 - 2 Bags").
/// </summary>
public class Producto : EntidadBase
{
    public required string Codigo { get; set; }

    public Guid GrupoId { get; set; }

    public required string Tipo { get; set; }

    public required string NombreCientifico { get; set; }

    /// <summary>Nombres comerciales en inglés (pueden ser varios, separados por " | ").</summary>
    public string? NombreComun { get; set; }

    public string? NombreEspanol { get; set; }

    public decimal? TallaMinCm { get; set; }

    public decimal? TallaMaxCm { get; set; }

    /// <summary>Talla descriptiva cuando no hay medida: small, medium, big…</summary>
    public string? TallaEtiqueta { get; set; }

    /// <summary>Peces por bolsa estándar (rango: "40 ~ 50").</summary>
    public int? BolsaMin { get; set; }

    public int? BolsaMax { get; set; }

    /// <summary>Disponibilidad publicada en el stocklist ("1 - 2 Bags", "Not available").</summary>
    public string? Disponibilidad { get; set; }

    public bool Activo { get; set; } = true;

    public string? Notas { get; set; }

    public DateTime? EliminadoEn { get; set; }
}

/// <summary>Centro de acopio de origen de un producto (hasta 3, en orden).</summary>
public class ProductoCentroAcopio
{
    public Guid ProductoId { get; set; }

    public Guid CentroAcopioId { get; set; }

    public int Orden { get; set; }
}

/// <summary>Lugar donde se acopian los peces (Leticia, Inírida, Villavicencio…), con su cuenca.</summary>
public class CentroAcopio : EntidadBase
{
    public required string Nombre { get; set; }

    public Guid CuencaId { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime? EliminadoEn { get; set; }
}

/// <summary>
/// Lista de precios (Standard, Glaser, Mayorista nacional…). Puede ser de un cliente (David Ramírez).
///
/// <para>
/// Una lista <b>derivada</b> tiene regla: precio = precio en <see cref="ListaBaseId"/> × factor del tipo
/// de producto × (1 + IVA) × (1 + pasarela) si <see cref="AplicaImpuestos"/>, redondeado al múltiplo
/// de <see cref="Redondeo"/>. La regla sirve para recalcular en bloque; cada precio sigue siendo editable.
/// </para>
/// </summary>
public class ListaPrecios : EntidadBase
{
    /// <summary>Código estable (STANDARD, COSTO…). No cambia: lo usan la auditoría y la importación.</summary>
    public required string Codigo { get; set; }

    public required string Nombre { get; set; }

    public required string Moneda { get; set; }

    /// <summary>Lista de costos: solo la ve quien tiene <c>catalogos:ver-costos</c>.</summary>
    public bool EsCosto { get; set; }

    public Guid? ClienteId { get; set; }

    public int Orden { get; set; }

    public bool Activo { get; set; } = true;

    public Guid? ListaBaseId { get; set; }

    public decimal? FactorSalvaje { get; set; }

    public decimal? FactorCriadero { get; set; }

    public decimal? FactorEspecial { get; set; }

    public bool AplicaImpuestos { get; set; }

    /// <summary>Múltiplo al que se redondea (50 en COP, 0.01 en USD).</summary>
    public decimal Redondeo { get; set; } = 0.01m;
}

/// <summary>Precio de un producto en una lista. Sin fila = sin precio en esa lista.</summary>
public class PrecioProducto
{
    public Guid ProductoId { get; set; }

    public Guid ListaPreciosId { get; set; }

    public decimal Precio { get; set; }

    public DateTime ActualizadoEn { get; set; }
}

/// <summary>Parámetros de precios. Una sola fila (<c>Id = 1</c>).</summary>
public class ParametrosPrecios
{
    public int Id { get; set; } = 1;

    /// <summary>IVA nacional (%), ej. 19.</summary>
    public decimal Iva { get; set; }

    /// <summary>Comisión de la pasarela de pagos (Wompi, %), ej. 3.15.</summary>
    public decimal Pasarela { get; set; }

    /// <summary>TRM de referencia para cotizar (COP por USD). Las ventas guardan su propia TRM.</summary>
    public decimal TrmReferencia { get; set; }

    public DateTime? ActualizadoEn { get; set; }
}

/// <summary>Proveedor nacional de peces (pescador, acopiador), asociado a centros de acopio.</summary>
public class Proveedor : EntidadBase
{
    public required string Nombre { get; set; }

    public Guid TipoIdentificacionId { get; set; }

    public required string NumeroIdentificacion { get; set; }

    public string? Telefono { get; set; }

    public string? Contacto { get; set; }

    public string? Email { get; set; }

    public string? Notas { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime? EliminadoEn { get; set; }
}

public class ProveedorCentroAcopio
{
    public Guid ProveedorId { get; set; }

    public Guid CentroAcopioId { get; set; }
}
