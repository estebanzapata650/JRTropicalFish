using JRTropicalFish.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace JRTropicalFish.Api.Auth;

/// <summary>
/// Exige que el usuario tenga el permiso indicado (formato <c>modulo:accion</c>, constantes en
/// <c>Domain/Seguridad/Permisos.cs</c>). Los permisos viajan como claims dentro del JWT, así que un
/// cambio de perfil solo aplica cuando el usuario vuelve a iniciar sesión.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequierePermisoAttribute : AuthorizeAttribute
{
    public const string PrefijoPolitica = "permiso:";

    public RequierePermisoAttribute(string permiso) => Policy = PrefijoPolitica + permiso;
}

/// <summary>
/// Crea al vuelo una política por cada permiso solicitado, para no registrarlas una por una en
/// <c>Program.cs</c>.
/// </summary>
public class PermisoPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(RequierePermisoAttribute.PrefijoPolitica, StringComparison.Ordinal))
        {
            return await base.GetPolicyAsync(policyName);
        }

        var permiso = policyName[RequierePermisoAttribute.PrefijoPolitica.Length..];

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim(JwtClaimTypes.Permiso, permiso)
            .Build();
    }
}
