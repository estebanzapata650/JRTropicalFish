namespace JRTropicalFish.Infrastructure.Auth;

/// <summary>Nombres de claims propios usados en los JWT emitidos por la API.</summary>
public static class JwtClaimTypes
{
    public const string Perfil = "perfil";

    /// <summary>Un claim por cada permiso del usuario, en formato <c>modulo:accion</c>.</summary>
    public const string Permiso = "permiso";
}
