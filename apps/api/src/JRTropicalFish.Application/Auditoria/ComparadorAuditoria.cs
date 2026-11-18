using System.Globalization;
using System.Reflection;
using JRTropicalFish.Application.Abstractions;

namespace JRTropicalFish.Application.Auditoria;

/// <summary>
/// Compara dos versiones de una entidad y devuelve solo los campos que cambiaron, para el control
/// de cambios (toda modificación de datos maestros e inventario queda auditada).
/// </summary>
public static class ComparadorAuditoria
{
    private static readonly string[] CamposIgnorados = ["Id", "CreadoEn", "ActualizadoEn"];

    public static IReadOnlyList<CambioAuditado> Comparar<T>(T anterior, T nuevo)
    {
        var cambios = new List<CambioAuditado>();

        foreach (var prop in PropiedadesComparables<T>())
        {
            var valorAnterior = Formatear(prop.GetValue(anterior));
            var valorNuevo = Formatear(prop.GetValue(nuevo));

            if (!string.Equals(valorAnterior, valorNuevo, StringComparison.Ordinal))
            {
                cambios.Add(new CambioAuditado(prop.Name, valorAnterior, valorNuevo));
            }
        }

        return cambios;
    }

    /// <summary>Estado completo de la entidad — se usa al crear o eliminar, donde no hay "anterior".</summary>
    public static IReadOnlyList<CambioAuditado> Instantanea<T>(T entidad, bool comoValorNuevo = true)
    {
        return PropiedadesComparables<T>()
            .Select(prop =>
            {
                var valor = Formatear(prop.GetValue(entidad));
                return comoValorNuevo
                    ? new CambioAuditado(prop.Name, null, valor)
                    : new CambioAuditado(prop.Name, valor, null);
            })
            .ToList();
    }

    private static IEnumerable<PropertyInfo> PropiedadesComparables<T>() =>
        typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !CamposIgnorados.Contains(p.Name));

    private static string? Formatear(object? valor) => valor switch
    {
        null => null,
        bool b => b ? "true" : "false",
        // "G29" normaliza la escala: 50000000.00 y 50000000 son el mismo valor y no deben
        // registrarse como un cambio (Postgres devuelve numeric con la escala de la columna).
        decimal d => d.ToString("G29", CultureInfo.InvariantCulture),
        DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
        // Sin este caso caería en IFormattable y saldría con el patrón de la cultura invariante
        // ("05/01/1990"), que se lee al revés en español. ISO es inequívoco.
        DateOnly f => f.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => valor.ToString(),
    };
}
