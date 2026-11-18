using JRTropicalFish.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JRTropicalFish.Api.Controllers;

public record LoginRequest(string Email, string Password);

[ApiController]
[Route("api/v1/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    /// <summary>Inicia sesión. El web guarda el token en una cookie httpOnly; el navegador nunca lo ve.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var resultado = await authService.LoginAsync(request.Email, request.Password, ct);
        return resultado is null
            ? Unauthorized(new { error = "Correo o contraseña incorrectos." })
            : Ok(resultado);
    }

    /// <summary>Datos de la sesión actual, leídos del token.</summary>
    [HttpGet("yo")]
    public IActionResult Yo() => Ok(new
    {
        nombre = User.Identity?.Name,
        perfil = User.FindFirst(Infrastructure.Auth.JwtClaimTypes.Perfil)?.Value,
        permisos = User.FindAll(Infrastructure.Auth.JwtClaimTypes.Permiso).Select(c => c.Value),
    });
}
