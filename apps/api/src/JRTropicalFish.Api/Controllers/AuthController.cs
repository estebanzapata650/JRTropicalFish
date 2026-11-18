using JRTropicalFish.Api.Comun;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auth;
using JRTropicalFish.Application.Seguridad;
using JRTropicalFish.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JRTropicalFish.Api.Controllers;

public record LoginRequest(string Email, string Password);

[ApiController]
[Route("api/v1/auth")]
public class AuthController(AuthService authService, IUsuarioActual usuarioActual) : ControllerBase
{
    /// <summary>
    /// Inicia sesión. 401 credenciales inválidas; 403 cuenta bloqueada o inactiva. El web guarda el
    /// token en una cookie httpOnly; el navegador nunca lo ve.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct) =>
        (await authService.LoginAsync(request.Email, request.Password, ct)).ARespuesta();

    /// <summary>Datos de la sesión actual, leídos del token (ya validado contra la revocación).</summary>
    [HttpGet("yo")]
    public IActionResult Yo() => Ok(new
    {
        id = usuarioActual.UsuarioId,
        nombre = User.Identity?.Name,
        email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
        perfil = User.FindFirst(JwtClaimTypes.Perfil)?.Value,
        permisos = User.FindAll(JwtClaimTypes.Permiso).Select(c => c.Value),
    });

    /// <summary>
    /// El propio usuario cambia su contraseña (no requiere permiso). Devuelve un token nuevo: las
    /// demás sesiones abiertas quedan revocadas.
    /// </summary>
    [HttpPost("cambiar-password")]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordRequest request, CancellationToken ct) =>
        usuarioActual.UsuarioId is { } id
            ? (await authService.CambiarPasswordAsync(id, request, ct)).ARespuesta()
            : Unauthorized();
}
