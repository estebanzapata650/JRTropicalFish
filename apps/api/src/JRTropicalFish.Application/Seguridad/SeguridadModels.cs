namespace JRTropicalFish.Application.Seguridad;

// --- Permisos ---

public record PermisoLectura(Guid Id, string Codigo, string? Descripcion);

// --- Perfiles ---

public record PerfilResumen(
    Guid Id,
    string Nombre,
    string? Descripcion,
    bool EsPlantilla,
    bool Activo,
    long TotalPermisos,
    long TotalUsuarios);

public record PerfilDetalle(
    Guid Id,
    string Nombre,
    string? Descripcion,
    bool EsPlantilla,
    bool Activo,
    IReadOnlyList<string> Permisos);

public record GuardarPerfilRequest(
    string Nombre,
    string? Descripcion,
    bool Activo,
    IReadOnlyList<string> Permisos);

// --- Usuarios ---

/// <remarks>⚠️ Dapper lo construye por posición: el orden debe coincidir con <c>fn_usuarios_listar</c>.</remarks>
public record UsuarioResumen(
    Guid Id,
    string Nombre,
    string Email,
    Guid PerfilId,
    string PerfilNombre,
    bool Activo,
    DateTime? BloqueadoHasta,
    DateTime? UltimoAccesoEn,
    DateTime CreadoEn);

public record CrearUsuarioRequest(string Nombre, string Email, Guid PerfilId, string Password);

/// <param name="Password">Vacía o nula significa "no cambiar la contraseña".</param>
public record ActualizarUsuarioRequest(string Nombre, string Email, Guid PerfilId, string? Password);

public record CambiarEstadoUsuarioRequest(bool Activo);

public record CambiarPasswordRequest(string PasswordActual, string PasswordNueva);

/// <summary>Lo que se audita de un usuario. Se audita el nombre del perfil, no su id, para que se lea.</summary>
public record UsuarioAuditable(string Nombre, string Email, string Perfil, bool Activo);

/// <summary>Lo que se audita de un perfil. Los permisos se comparan como conjunto.</summary>
public record PerfilAuditable(string Nombre, string? Descripcion, bool Activo);
