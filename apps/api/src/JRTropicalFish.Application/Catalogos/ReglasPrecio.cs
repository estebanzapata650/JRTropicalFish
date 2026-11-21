using JRTropicalFish.Domain.Catalogos;

namespace JRTropicalFish.Application.Catalogos;

/// <summary>
/// Regla de una lista de precios derivada (regla pura, probada; el web la repite en
/// <c>lib/catalogos.ts</c> para sugerir precios):
/// <code>precio = base × factor(tipo) × [(1 + IVA) × (1 + pasarela)] → redondeado al múltiplo</code>
/// Deducida del Excel «JR MAIN STOCK LIST»: p. ej. costo $3.000 criadero → 3.000 × 1,3 × 1,0315 × 1,19
/// = 4.787 → $4.800 mayorista; × 1,5 → $7.200 al detal.
/// </summary>
public static class ReglasPrecio
{
    /// <summary>Factor de la lista para el tipo de producto, o <c>null</c> si la regla no lo cubre.</summary>
    public static decimal? Factor(ListaPreciosFila lista, string tipoProducto) => tipoProducto switch
    {
        TiposProducto.Salvaje => lista.FactorSalvaje,
        TiposProducto.Criadero => lista.FactorCriadero,
        TiposProducto.Especial => lista.FactorEspecial,
        _ => null,
    };

    /// <returns>El precio calculado, o <c>null</c> si no hay base o la lista no tiene factor para el tipo.</returns>
    public static decimal? Calcular(decimal? precioBase, string tipoProducto, ListaPreciosFila lista, ParametrosPreciosInfo parametros)
    {
        if (precioBase is not > 0 || Factor(lista, tipoProducto) is not { } factor || factor <= 0)
        {
            return null;
        }

        var valor = precioBase.Value * factor;
        if (lista.AplicaImpuestos)
        {
            valor *= (1 + parametros.Iva / 100) * (1 + parametros.Pasarela / 100);
        }

        return Redondear(valor, lista.Redondeo);
    }

    /// <summary>Al múltiplo más cercano (mitades hacia arriba): 4.787 → 4.800 con múltiplo 50.</summary>
    public static decimal Redondear(decimal valor, decimal multiplo) =>
        multiplo <= 0 ? valor : Math.Round(valor / multiplo, 0, MidpointRounding.AwayFromZero) * multiplo;
}
