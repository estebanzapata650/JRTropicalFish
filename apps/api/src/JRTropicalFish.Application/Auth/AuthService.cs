using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Seguridad;

namespace JRTropicalFish.Application.Auth;

public record AuthResult(string Token, DateTime ExpiraEn, UsuarioSesion Usuario);

/// <summary>Lo que el web necesita saber del usuario para pintar el menú sin volver a preguntar.</summary>
public record UsuarioSesion(Guid Id, string Nombre, string Email, string Perfil, IReadOnlyList<string> Permisos);

public interface IJwtTokenService
{
    /// <summary>
    /// Emite el token con los permisos como claims y la fecha de emisión (<c>iat</c>), que es lo que
    /// permite revocarlo después con <c>usuarios.sesion_invalida_antes</c>.
    /// </summary>
    AuthResult GenerarToken(UsuarioSesion usuario);
}

public class AuthService(
    IUsuarioRepository usuarioRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    public const string MensajeCredencialesInvalidas = "Correo o contraseña incorrectos.";

    /// <summary>
    /// Inicia sesión. Email inexistente y clave incorrecta dan el mismo mensaje a propósito:
    /// distinguirlos le diría a un atacante qué correos existen. Tras
    /// <see cref="ReglasBloqueo.IntentosMaximos"/> fallos seguidos la cuenta se bloquea un rato.
    /// </summary>
    public async Task<Resultado<AuthResult>> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var invalidas = Error.NoAutenticado(MensajeCredencialesInvalidas);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            return invalidas;
        }

        var usuario = await usuarioRepository.BuscarPorEmailAsync(email.Trim().ToLowerInvariant(), ct);
        if (usuario is null)
        {
            return invalidas;
        }

        var ahora = reloj.AhoraUtc;
        if (ReglasBloqueo.EstaBloqueado(usuario.BloqueadoHasta, ahora))
        {
            return Bloqueada(usuario.BloqueadoHasta!.Value, ahora);
        }

        if (!passwordHasher.Verify(password, usuario.PasswordHash))
        {
            var bloqueo = ReglasBloqueo.BloqueoTrasFallo(usuario.IntentosFallidos, ahora);
            await usuarioRepository.RegistrarFalloAsync(usuario.Id, bloqueo, ct);
            return bloqueo is { } hasta ? Bloqueada(hasta, ahora) : invalidas;
        }

        // Se dice solo después de validar la clave: así no revela a un tercero que la cuenta existe.
        if (!usuario.Activo)
        {
            return Error.Prohibido("Tu usuario está inactivo. Contacta al administrador.");
        }

        await usuarioRepository.RegistrarAccesoAsync(usuario.Id, ahora, ct);
        return await EmitirAsync(usuario, ct);
    }

    /// <summary>
    /// El propio usuario cambia su contraseña. Las demás sesiones abiertas quedan revocadas y se
    /// devuelve un token nuevo para no sacar al usuario de la que está usando.
    /// </summary>
    public async Task<Resultado<AuthResult>> CambiarPasswordAsync(
        Guid usuarioId, CambiarPasswordRequest request, CancellationToken ct = default)
    {
        var usuario = await usuarioRepository.BuscarParaAccesoPorIdAsync(usuarioId, ct);
        if (usuario is null || !usuario.Activo)
        {
            return Error.NoAutenticado("La sesión no es válida.");
        }

        if (!passwordHasher.Verify(request.PasswordActual ?? string.Empty, usuario.PasswordHash))
        {
            return Error.Validacion("La contraseña actual no es correcta.");
        }

        if (ReglasPassword.Validar(request.PasswordNueva) is { } motivo)
        {
            return Error.Validacion(motivo);
        }

        if (request.PasswordNueva == request.PasswordActual)
        {
            return Error.Validacion("La nueva contraseña debe ser distinta de la actual.");
        }

        await usuarioRepository.CambiarPasswordAsync(usuarioId, passwordHasher.Hash(request.PasswordNueva), reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(
            UsuarioService.Entidad, usuarioId, [new CambioAuditado("Contraseña", null, "Cambiada por el propio usuario")], ct);

        return await EmitirAsync(usuario, ct);
    }

    private async Task<Resultado<AuthResult>> EmitirAsync(UsuarioConPerfil usuario, CancellationToken ct)
    {
        var permisos = await usuarioRepository.ObtenerPermisosAsync(usuario.Id, ct);
        return jwtTokenService.GenerarToken(
            new UsuarioSesion(usuario.Id, usuario.Nombre, usuario.Email, usuario.PerfilNombre, permisos));
    }

    private static Error Bloqueada(DateTime hasta, DateTime ahora) =>
        Error.Prohibido(
            $"La cuenta está bloqueada por varios intentos fallidos. Intenta de nuevo en {ReglasBloqueo.MinutosRestantes(hasta, ahora)} minuto(s) o pide al administrador que la desbloquee.");
}
