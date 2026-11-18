using JRTropicalFish.Domain.Seguridad;

namespace JRTropicalFish.Application.Abstractions;

/// <summary>Usuario con el nombre de su perfil, tal como lo devuelve <c>fn_usuario_por_email</c>.</summary>
public record UsuarioConPerfil(
    Guid Id,
    string Nombre,
    string Email,
    string PasswordHash,
    Guid PerfilId,
    string PerfilNombre,
    bool Activo);

public interface IUsuarioRepository
{
    Task<UsuarioConPerfil?> BuscarPorEmailAsync(string email, CancellationToken ct = default);

    Task<IReadOnlyList<string>> ObtenerPermisosAsync(Guid usuarioId, CancellationToken ct = default);

    Task CrearAsync(Usuario usuario, CancellationToken ct = default);
}

public interface IPerfilRepository
{
    Task<Guid?> BuscarIdPorNombreAsync(string nombre, CancellationToken ct = default);
}
