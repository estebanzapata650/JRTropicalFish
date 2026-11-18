using JRTropicalFish.Application.Abstractions;

namespace JRTropicalFish.Application.Auditoria;

/// <summary>
/// Audita las colecciones hijas de una entidad (contactos, sucursales, documentos...).
///
/// Existe porque <see cref="ComparadorAuditoria"/> compara un objeto contra otro, y con eso solo se
/// detectaban los cambios de la entidad padre: modificar únicamente un contacto no registraba nada
/// en el control de cambios. Aquí se empareja por identificador y se delega la comparación campo a
/// campo en el comparador de siempre, prefijando el nombre para saber a qué elemento pertenece.
///
/// Un alta o una baja se resumen en una sola fila legible (el detalle completo de un contacto nuevo
/// serían diez filas de "— → valor", que enterrarían al resto del historial).
/// </summary>
public static class ComparadorColecciones
{
    public static IReadOnlyList<CambioAuditado> Comparar<T>(
        string prefijo,
        IReadOnlyList<T> anteriores,
        IReadOnlyList<T> nuevos,
        Func<T, Guid> clave,
        Func<T, string> etiqueta)
    {
        var cambios = new List<CambioAuditado>();
        var porClaveAnterior = anteriores.ToDictionary(clave);
        var clavesNuevas = nuevos.Select(clave).ToHashSet();

        foreach (var nuevo in nuevos)
        {
            if (porClaveAnterior.TryGetValue(clave(nuevo), out var anterior))
            {
                var nombre = etiqueta(nuevo);
                cambios.AddRange(
                    ComparadorAuditoria.Comparar(anterior, nuevo)
                        .Select(c => c with { Campo = $"{prefijo}[{nombre}].{c.Campo}" }));
            }
            else
            {
                cambios.Add(new CambioAuditado($"{prefijo} (agregado)", null, etiqueta(nuevo)));
            }
        }

        cambios.AddRange(
            anteriores
                .Where(a => !clavesNuevas.Contains(clave(a)))
                .Select(a => new CambioAuditado($"{prefijo} (eliminado)", etiqueta(a), null)));

        return cambios;
    }

    /// <summary>
    /// Para conjuntos de códigos sin entidad propia (responsabilidades fiscales, sedes vinculadas):
    /// una sola fila con la lista antes y después, en vez de una por código.
    /// </summary>
    public static IReadOnlyList<CambioAuditado> CompararConjunto(
        string campo, IEnumerable<string> anteriores, IEnumerable<string> nuevos)
    {
        var antes = Normalizar(anteriores);
        var despues = Normalizar(nuevos);

        return antes.SequenceEqual(despues, StringComparer.OrdinalIgnoreCase)
            ? []
            : [new CambioAuditado(campo, Unir(antes), Unir(despues))];
    }

    private static List<string> Normalizar(IEnumerable<string> valores) =>
        valores
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string? Unir(List<string> valores) =>
        valores.Count == 0 ? null : string.Join(", ", valores);
}
