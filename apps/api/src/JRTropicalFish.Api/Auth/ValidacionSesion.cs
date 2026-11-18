using System.IdentityModel.Tokens.Jwt;
using JRTropicalFish.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace JRTropicalFish.Api.Auth;

/// <summary>
/// Revocación de tokens. El JWT es válido por firma hasta que expira, pero el negocio necesita que
/// desactivar a un usuario, cambiarle la contraseña o quitarle permisos surta efecto ya. Para eso la
/// base guarda <c>usuarios.sesion_invalida_antes</c> y aquí se rechaza todo token emitido antes.
/// </summary>
public static class ValidacionSesion
{
    public static async Task ValidarAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        var sub = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var iat = principal?.FindFirst(JwtRegisteredClaimNames.Iat)?.Value;

        if (!Guid.TryParse(sub, out var usuarioId) || !long.TryParse(iat, out var segundos))
        {
            context.Fail("Token sin identificación de usuario o fecha de emisión.");
            return;
        }

        var emitidoEn = DateTimeOffset.FromUnixTimeSeconds(segundos).UtcDateTime;
        var usuarios = context.HttpContext.RequestServices.GetRequiredService<IUsuarioRepository>();

        if (!await usuarios.SesionValidaAsync(usuarioId, emitidoEn, context.HttpContext.RequestAborted))
        {
            context.Fail("La sesión fue revocada.");
        }
    }
}
