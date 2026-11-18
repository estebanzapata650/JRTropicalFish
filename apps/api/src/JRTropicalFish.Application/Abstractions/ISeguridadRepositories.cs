using JRTropicalFish.Application.Seguridad;
using JRTropicalFish.Domain.Seguridad;

namespace JRTropicalFish.Application.Abstractions;

/// <summary>Usuario con su perfil y estado de acceso, tal como lo necesita el inicio de sesión.</summary>
/// <remarks>⚠️ Dapper lo construye por posición: el orden debe coincidir con <c>fn_usuario_por_email</c>.</remarks>
public record UsuarioConPerfil(
    Guid Id,
    string Nombre,
    string Email,
    string PasswordHash,
    Guid PerfilId,
    string PerfilNombre,
    bool Activo,
    int IntentosFallidos,
    DateTime? BloqueadoHasta);

public interface IUsuarioRepository
{
    Task<UsuarioConPerfil?> BuscarPorEmailAsync(string email, CancellationToken ct = default);

    Task<UsuarioConPerfil?> BuscarParaAccesoPorIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<string>> ObtenerPermisosAsync(Guid usuarioId, CancellationToken ct = default);

    Task<IReadOnlyList<UsuarioResumen>> ListarAsync(string? busqueda, CancellationToken ct = default);

    Task<UsuarioResumen?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);

    Task<bool> ExisteEmailAsync(string email, Guid? excluirId, CancellationToken ct = default);

    /// <summary>
    /// Usuarios activos que pueden administrar usuarios (permiso <c>usuarios:editar</c> en un perfil
    /// activo), sin contar al usuario ni al perfil indicados. Evita dejar el sistema sin administrador.
    /// </summary>
    Task<long> ContarAdministradoresAsync(Guid? excluirUsuarioId, Guid? excluirPerfilId, CancellationToken ct = default);

    Task CrearAsync(Usuario usuario, CancellationToken ct = default);

    /// <param name="invalidarSesiones">Si es true, los tokens emitidos antes de <paramref name="fecha"/> dejan de valer.</param>
    Task ActualizarAsync(Guid id, string nombre, string email, Guid perfilId, bool invalidarSesiones, DateTime fecha, CancellationToken ct = default);

    /// <summary>Activa o desactiva. Desactivar invalida las sesiones abiertas.</summary>
    Task CambiarEstadoAsync(Guid id, bool activo, DateTime fecha, CancellationToken ct = default);

    /// <summary>Cambia el hash, invalida sesiones previas a <paramref name="fecha"/> y limpia el bloqueo.</summary>
    Task CambiarPasswordAsync(Guid id, string passwordHash, DateTime fecha, CancellationToken ct = default);

    Task RegistrarAccesoAsync(Guid id, DateTime fecha, CancellationToken ct = default);

    /// <param name="bloquearHasta">Si tiene valor, la cuenta queda bloqueada y el contador vuelve a 0.</param>
    Task RegistrarFalloAsync(Guid id, DateTime? bloquearHasta, CancellationToken ct = default);

    Task DesbloquearAsync(Guid id, CancellationToken ct = default);

    /// <summary>El usuario sigue activo y el token (emitido en <paramref name="emitidoEn"/>) no fue revocado.</summary>
    Task<bool> SesionValidaAsync(Guid id, DateTime emitidoEn, CancellationToken ct = default);
}

public interface IPermisoRepository
{
    Task<IReadOnlyList<PermisoLectura>> ListarAsync(CancellationToken ct = default);
}

public interface IPerfilRepository
{
    Task<Guid?> BuscarIdPorNombreAsync(string nombre, CancellationToken ct = default);

    Task<IReadOnlyList<PerfilResumen>> ListarAsync(string? busqueda, CancellationToken ct = default);

    Task<PerfilDetalle?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);

    Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId, CancellationToken ct = default);

    Task<long> ContarUsuariosAsync(Guid id, CancellationToken ct = default);

    /// <summary>Crea el perfil y sus permisos en una sola transacción.</summary>
    Task CrearAsync(Perfil perfil, IReadOnlyList<string> permisos, CancellationToken ct = default);

    /// <summary>
    /// Actualiza el perfil y reemplaza sus permisos en una transacción. Si
    /// <paramref name="invalidarSesiones"/>, los usuarios del perfil deben volver a iniciar sesión.
    /// </summary>
    Task ActualizarAsync(Perfil perfil, IReadOnlyList<string> permisos, bool invalidarSesiones, CancellationToken ct = default);

    Task EliminarAsync(Guid id, CancellationToken ct = default);
}
