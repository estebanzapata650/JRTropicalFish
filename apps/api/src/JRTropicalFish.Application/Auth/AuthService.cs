using JRTropicalFish.Application.Abstractions;

namespace JRTropicalFish.Application.Auth;

public record AuthResult(string Token, DateTime ExpiraEn, UsuarioSesion Usuario);

/// <summary>Lo que el web necesita saber del usuario para pintar el menú sin volver a preguntar.</summary>
public record UsuarioSesion(Guid Id, string Nombre, string Email, string Perfil, IReadOnlyList<string> Permisos);

public interface IJwtTokenService
{
    /// <summary>
    /// Emite el token del usuario con sus permisos como claims. Los permisos van dentro del token,
    /// así que un cambio de perfil solo surte efecto cuando el usuario vuelve a iniciar sesión.
    /// </summary>
    AuthResult GenerarToken(UsuarioSesion usuario);
}

public class AuthService(
    IUsuarioRepository usuarioRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService)
{
    /// <summary>
    /// Devuelve <c>null</c> ante cualquier fallo (email inexistente, usuario inactivo o clave
    /// incorrecta) a propósito: distinguirlos le diría a un atacante qué correos existen.
    /// </summary>
    public async Task<AuthResult?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        var usuario = await usuarioRepository.BuscarPorEmailAsync(email.Trim(), ct);
        if (usuario is null || !usuario.Activo || !passwordHasher.Verify(password, usuario.PasswordHash))
        {
            return null;
        }

        var permisos = await usuarioRepository.ObtenerPermisosAsync(usuario.Id, ct);

        return jwtTokenService.GenerarToken(
            new UsuarioSesion(usuario.Id, usuario.Nombre, usuario.Email, usuario.PerfilNombre, permisos));
    }
}
