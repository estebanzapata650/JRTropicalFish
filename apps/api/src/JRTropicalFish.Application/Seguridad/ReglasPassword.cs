namespace JRTropicalFish.Application.Seguridad;

/// <summary>Política de contraseñas. Pura y estática para poder probarla sin infraestructura.</summary>
public static class ReglasPassword
{
    public const int LargoMinimo = 8;
    public const int LargoMaximo = 128;

    /// <summary>Devuelve el motivo por el que la contraseña no sirve, o <c>null</c> si es válida.</summary>
    public static string? Validar(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < LargoMinimo)
        {
            return $"La contraseña debe tener al menos {LargoMinimo} caracteres.";
        }

        if (password.Length > LargoMaximo)
        {
            return $"La contraseña no puede superar {LargoMaximo} caracteres.";
        }

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            return "La contraseña debe combinar letras y números.";
        }

        return null;
    }
}

/// <summary>Protección contra fuerza bruta en el inicio de sesión.</summary>
public static class ReglasBloqueo
{
    public const int IntentosMaximos = 5;
    public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

    public static bool EstaBloqueado(DateTime? bloqueadoHasta, DateTime ahoraUtc) =>
        bloqueadoHasta is { } hasta && hasta > ahoraUtc;

    /// <summary>
    /// Con el número de fallos que ya tenía la cuenta, decide si este nuevo fallo la bloquea.
    /// Devuelve hasta cuándo, o <c>null</c> si todavía no corresponde bloquear.
    /// </summary>
    public static DateTime? BloqueoTrasFallo(int intentosPrevios, DateTime ahoraUtc) =>
        intentosPrevios + 1 >= IntentosMaximos ? ahoraUtc.Add(DuracionBloqueo) : null;

    public static int MinutosRestantes(DateTime bloqueadoHasta, DateTime ahoraUtc) =>
        Math.Max(1, (int)Math.Ceiling((bloqueadoHasta - ahoraUtc).TotalMinutes));
}
