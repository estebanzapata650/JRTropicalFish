using System.Text.RegularExpressions;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Domain.Crm;

namespace JRTropicalFish.Application.Crm;

/// <summary>
/// Máquina de estados del pipeline. Regla pura (sin base de datos) para poder probarla:
/// <list type="bullet">
/// <item>Solo se avanza: a cualquier etapa posterior (se permite saltar) o a Perdida.</item>
/// <item>Ganada y Perdida son terminales: desde ahí no hay transición.</item>
/// <item>Perdida exige motivo.</item>
/// <item>Ganada solo se alcanza con "Ganar", que además crea la venta.</item>
/// </list>
/// El procedimiento repite la comprobación de la etapa actual al escribir (comparar y escribir).
/// </summary>
public static class ReglasPipeline
{
    public const int LongitudMinimaMotivo = 3;

    private static int Posicion(string etapa) => EtapasPipeline.Todos.ToList().IndexOf(etapa);

    /// <summary>Etapas a las que se puede pasar desde <paramref name="actual"/>, en orden (incluye Ganada y Perdida).</summary>
    public static IReadOnlyList<string> Siguientes(string actual) =>
        EtapasPipeline.EsTerminal(actual)
            ? []
            : [.. EtapasPipeline.Todos.Where(e => e == EtapasPipeline.Perdida || Posicion(e) > Posicion(actual))];

    /// <summary>Valida un cambio de etapa por el endpoint de cambio (no por "Ganar").</summary>
    public static Error? ValidarCambio(string actual, string? nueva, string? motivoPerdida)
    {
        if (nueva is null || !EtapasPipeline.Todos.Contains(nueva))
        {
            return Error.Validacion("La etapa indicada no existe.");
        }

        if (EtapasPipeline.EsTerminal(actual))
        {
            return Error.Conflicto("La oportunidad ya está cerrada (ganada o perdida) y no cambia de etapa.");
        }

        if (nueva == actual)
        {
            return Error.Validacion("La oportunidad ya está en esa etapa.");
        }

        if (nueva == EtapasPipeline.Ganada)
        {
            return Error.Validacion("Para ganar la oportunidad usa «Ganar»: registra el pago y crea la venta.");
        }

        if (nueva == EtapasPipeline.Perdida)
        {
            return string.IsNullOrWhiteSpace(motivoPerdida) || motivoPerdida.Trim().Length < LongitudMinimaMotivo
                ? Error.Validacion("Indica el motivo por el que se perdió la oportunidad.")
                : null;
        }

        return Posicion(nueva) > Posicion(actual)
            ? null
            : Error.Validacion("El pipeline solo avanza: no se puede volver a una etapa anterior.");
    }

    public static Error? ValidarGanar(string actual) =>
        EtapasPipeline.EsTerminal(actual)
            ? Error.Conflicto("La oportunidad ya está cerrada (ganada o perdida).")
            : null;
}

/// <summary>Normalización y validación de campos compartida por los servicios del CRM.</summary>
public static partial class ValidacionCrm
{
    /// <summary>Recorta y colapsa espacios; vacío pasa a <c>null</c>.</summary>
    public static string? Limpiar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        return Espacios().Replace(valor.Trim(), " ");
    }

    /// <summary>Como <see cref="Limpiar"/> pero conserva los saltos de línea (notas).</summary>
    public static string? LimpiarTextoLargo(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    public static Error? Requerido(string? valor, string campo, int maximo, int minimo = 1)
    {
        if (valor is null || valor.Length < minimo)
        {
            return Error.Validacion(minimo > 1 ? $"{campo}: mínimo {minimo} caracteres." : $"{campo} es obligatorio.");
        }

        return valor.Length > maximo ? Error.Validacion($"{campo}: máximo {maximo} caracteres.") : null;
    }

    public static Error? Opcional(string? valor, string campo, int maximo) =>
        valor is not null && valor.Length > maximo ? Error.Validacion($"{campo}: máximo {maximo} caracteres.") : null;

    public static Error? EnLista(string? valor, IReadOnlyList<string> validos, string campo) =>
        valor is not null && validos.Contains(valor) ? null : Error.Validacion($"{campo}: valor no válido.");

    public static Error? NoNegativo(decimal valor, string campo) =>
        valor < 0 ? Error.Validacion($"{campo} no puede ser negativo.") : null;

    public static Error? NoNegativo(int valor, string campo) =>
        valor < 0 ? Error.Validacion($"{campo} no puede ser negativo.") : null;

    /// <summary>Primer error de la lista, o <c>null</c>.</summary>
    public static Error? Primero(params Error?[] errores) => errores.FirstOrDefault(e => e is not null);

    public static bool EmailValido(string email) => EmailRegex().IsMatch(email);

    /// <summary>Teléfono: dígitos con +, espacios, guiones, puntos o paréntesis; al menos 7 dígitos.</summary>
    public static bool TelefonoValido(string telefono) =>
        TelefonoRegex().IsMatch(telefono) && telefono.Count(char.IsDigit) >= 7;

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacios();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^\+?[0-9 ().-]+$")]
    private static partial Regex TelefonoRegex();
}
