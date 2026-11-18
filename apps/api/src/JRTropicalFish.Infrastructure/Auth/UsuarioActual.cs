using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using JRTropicalFish.Application.Abstractions;

namespace JRTropicalFish.Infrastructure.Auth;

/// <summary>Lee la identidad del usuario autenticado desde los claims del JWT de la petición actual.</summary>
public class UsuarioActual(IHttpContextAccessor httpContextAccessor) : IUsuarioActual
{
    public Guid? UsuarioId
    {
        get
        {
            var sub = httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Nombre => httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value;
}
