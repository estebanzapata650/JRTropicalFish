using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Auth;
using JRTropicalFish.Application.Seguridad;
using JRTropicalFish.Domain.Auditoria;
using JRTropicalFish.Domain.Seguridad;

namespace JRTropicalFish.Tests.Seguridad;

/// <summary>
/// Implementación en memoria de los repositorios de seguridad, con las mismas reglas que las
/// funciones PL/pgSQL (conteo de administradores, revocación de sesiones). Permite probar los
/// servicios sin base de datos.
/// </summary>
public sealed class SeguridadEnMemoria :
    IUsuarioRepository, IPerfilRepository, IPermisoRepository, IAuditoriaRepository,
    IPasswordHasher, IReloj, IUsuarioActual, IJwtTokenService
{
    public sealed class UsuarioMem
    {
        public Guid Id { get; init; }
        public string Nombre { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public Guid PerfilId { get; set; }
        public bool Activo { get; set; } = true;
        public int IntentosFallidos { get; set; }
        public DateTime? BloqueadoHasta { get; set; }
        public DateTime? UltimoAccesoEn { get; set; }
        public DateTime? SesionInvalidaAntes { get; set; }
        public DateTime CreadoEn { get; set; }
    }

    public sealed class PerfilMem
    {
        public Guid Id { get; init; }
        public string Nombre { get; set; } = "";
        public string? Descripcion { get; set; }
        public bool EsPlantilla { get; set; }
        public bool Activo { get; set; } = true;
        public List<string> Permisos { get; set; } = [];
    }

    public Dictionary<Guid, UsuarioMem> Usuarios { get; } = [];
    public Dictionary<Guid, PerfilMem> Perfiles { get; } = [];
    public List<RegistroAuditoria> Auditoria { get; } = [];

    public DateTime Ahora { get; set; } = new(2026, 11, 18, 15, 0, 0, DateTimeKind.Utc);
    public Guid? UsuarioActualId { get; set; }

    public PerfilMem Administrador { get; }
    public PerfilMem Consulta { get; }

    public SeguridadEnMemoria()
    {
        Administrador = AgregarPerfil(PerfilesPlantilla.Administrador, [.. Permisos.Todos.Keys], plantilla: true);
        Consulta = AgregarPerfil(PerfilesPlantilla.Consulta, [Permisos.UsuariosLeer, Permisos.PerfilesLeer], plantilla: true);
    }

    public PerfilMem AgregarPerfil(string nombre, List<string> permisos, bool plantilla = false, bool activo = true)
    {
        var perfil = new PerfilMem { Id = Guid.CreateVersion7(), Nombre = nombre, Permisos = permisos, EsPlantilla = plantilla, Activo = activo };
        Perfiles[perfil.Id] = perfil;
        return perfil;
    }

    public UsuarioMem AgregarUsuario(string nombre, PerfilMem perfil, string password = "Clave1234", bool activo = true)
    {
        var usuario = new UsuarioMem
        {
            Id = Guid.CreateVersion7(),
            Nombre = nombre,
            Email = $"{nombre.ToLowerInvariant()}@jrtf.local",
            PasswordHash = Hash(password),
            PerfilId = perfil.Id,
            Activo = activo,
            CreadoEn = Ahora,
        };
        Usuarios[usuario.Id] = usuario;
        return usuario;
    }

    public UsuarioService UsuarioService() => new(this, this, this, this, this, new ServicioAuditoria(this, this, this));

    public PerfilService PerfilService() => new(this, this, this, this, new ServicioAuditoria(this, this, this));

    public AuthService AuthService() => new(this, this, this, this, new ServicioAuditoria(this, this, this));

    // --- IPasswordHasher: "hash" trivial y reconocible ---
    public string Hash(string password) => "h:" + password;
    public bool Verify(string password, string hash) => hash == "h:" + password;

    // --- IReloj / IUsuarioActual ---
    public DateTime AhoraUtc => Ahora;
    public DateOnly Hoy => DateOnly.FromDateTime(Ahora);
    public Guid? UsuarioId => UsuarioActualId;
    public string? Nombre => UsuarioActualId is { } id && Usuarios.TryGetValue(id, out var u) ? u.Nombre : null;

    // --- IJwtTokenService ---
    public AuthResult GenerarToken(UsuarioSesion usuario) => new($"token-{usuario.Id}", Ahora.AddHours(8), usuario);

    // --- IUsuarioRepository ---
    private UsuarioConPerfil? Acceso(UsuarioMem? u) => u is null
        ? null
        : new UsuarioConPerfil(u.Id, u.Nombre, u.Email, u.PasswordHash, u.PerfilId, Perfiles[u.PerfilId].Nombre, u.Activo, u.IntentosFallidos, u.BloqueadoHasta);

    private UsuarioResumen Resumen(UsuarioMem u) =>
        new(u.Id, u.Nombre, u.Email, u.PerfilId, Perfiles[u.PerfilId].Nombre, u.Activo, u.BloqueadoHasta, u.UltimoAccesoEn, u.CreadoEn);

    public Task<UsuarioConPerfil?> BuscarPorEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(Acceso(Usuarios.Values.FirstOrDefault(u => u.Email == email.Trim().ToLowerInvariant())));

    public Task<UsuarioConPerfil?> BuscarParaAccesoPorIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Acceso(Usuarios.GetValueOrDefault(id)));

    public Task<IReadOnlyList<string>> ObtenerPermisosAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var perfil = Perfiles[Usuarios[usuarioId].PerfilId];
        return Task.FromResult<IReadOnlyList<string>>(perfil.Activo ? [.. perfil.Permisos.Order()] : []);
    }

    public Task<IReadOnlyList<UsuarioResumen>> ListarAsync(string? busqueda, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<UsuarioResumen>>([.. Usuarios.Values.Select(Resumen)]);

    public Task<UsuarioResumen?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Usuarios.TryGetValue(id, out var u) ? Resumen(u) : null);

    public Task<bool> ExisteEmailAsync(string email, Guid? excluirId, CancellationToken ct = default) =>
        Task.FromResult(Usuarios.Values.Any(u => u.Email == email.Trim().ToLowerInvariant() && u.Id != excluirId));

    public Task<long> ContarAdministradoresAsync(Guid? excluirUsuarioId, Guid? excluirPerfilId, CancellationToken ct = default) =>
        Task.FromResult((long)Usuarios.Values.Count(u =>
            u.Activo && u.Id != excluirUsuarioId && u.PerfilId != excluirPerfilId
            && Perfiles[u.PerfilId] is { Activo: true } p && p.Permisos.Contains(Permisos.UsuariosEditar)));

    public Task CrearAsync(Usuario usuario, CancellationToken ct = default)
    {
        Usuarios[usuario.Id] = new UsuarioMem
        {
            Id = usuario.Id, Nombre = usuario.Nombre, Email = usuario.Email, PasswordHash = usuario.PasswordHash,
            PerfilId = usuario.PerfilId, Activo = usuario.Activo, CreadoEn = usuario.CreadoEn,
        };
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(Guid id, string nombre, string email, Guid perfilId, bool invalidarSesiones, DateTime fecha, CancellationToken ct = default)
    {
        var u = Usuarios[id];
        (u.Nombre, u.Email, u.PerfilId) = (nombre, email, perfilId);
        if (invalidarSesiones) u.SesionInvalidaAntes = fecha;
        return Task.CompletedTask;
    }

    public Task CambiarEstadoAsync(Guid id, bool activo, DateTime fecha, CancellationToken ct = default)
    {
        var u = Usuarios[id];
        u.Activo = activo;
        if (!activo) u.SesionInvalidaAntes = fecha;
        return Task.CompletedTask;
    }

    public Task CambiarPasswordAsync(Guid id, string passwordHash, DateTime fecha, CancellationToken ct = default)
    {
        var u = Usuarios[id];
        (u.PasswordHash, u.SesionInvalidaAntes, u.IntentosFallidos, u.BloqueadoHasta) = (passwordHash, fecha, 0, null);
        return Task.CompletedTask;
    }

    public Task RegistrarAccesoAsync(Guid id, DateTime fecha, CancellationToken ct = default)
    {
        var u = Usuarios[id];
        (u.UltimoAccesoEn, u.IntentosFallidos, u.BloqueadoHasta) = (fecha, 0, null);
        return Task.CompletedTask;
    }

    public Task RegistrarFalloAsync(Guid id, DateTime? bloquearHasta, CancellationToken ct = default)
    {
        var u = Usuarios[id];
        u.IntentosFallidos = bloquearHasta is null ? u.IntentosFallidos + 1 : 0;
        u.BloqueadoHasta = bloquearHasta ?? u.BloqueadoHasta;
        return Task.CompletedTask;
    }

    public Task DesbloquearAsync(Guid id, CancellationToken ct = default)
    {
        (Usuarios[id].IntentosFallidos, Usuarios[id].BloqueadoHasta) = (0, null);
        return Task.CompletedTask;
    }

    public Task<bool> SesionValidaAsync(Guid id, DateTime emitidoEn, CancellationToken ct = default) =>
        Task.FromResult(Usuarios.TryGetValue(id, out var u) && u.Activo && (u.SesionInvalidaAntes is null || emitidoEn >= u.SesionInvalidaAntes));

    // --- IPermisoRepository ---
    Task<IReadOnlyList<PermisoLectura>> IPermisoRepository.ListarAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PermisoLectura>>([.. Permisos.Todos.Select(p => new PermisoLectura(Guid.Empty, p.Key, p.Value))]);

    // --- IPerfilRepository ---
    public Task<Guid?> BuscarIdPorNombreAsync(string nombre, CancellationToken ct = default) =>
        Task.FromResult(Perfiles.Values.FirstOrDefault(p => p.Nombre == nombre)?.Id);

    Task<IReadOnlyList<PerfilResumen>> IPerfilRepository.ListarAsync(string? busqueda, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PerfilResumen>>([.. Perfiles.Values.Select(p => new PerfilResumen(
            p.Id, p.Nombre, p.Descripcion, p.EsPlantilla, p.Activo, p.Permisos.Count, Usuarios.Values.Count(u => u.PerfilId == p.Id)))]);

    Task<PerfilDetalle?> IPerfilRepository.ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Perfiles.TryGetValue(id, out var p)
            ? new PerfilDetalle(p.Id, p.Nombre, p.Descripcion, p.EsPlantilla, p.Activo, [.. p.Permisos.Order()])
            : null);

    public Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId, CancellationToken ct = default) =>
        Task.FromResult(Perfiles.Values.Any(p => string.Equals(p.Nombre, nombre.Trim(), StringComparison.OrdinalIgnoreCase) && p.Id != excluirId));

    public Task<long> ContarUsuariosAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult((long)Usuarios.Values.Count(u => u.PerfilId == id));

    public Task CrearAsync(Perfil perfil, IReadOnlyList<string> permisos, CancellationToken ct = default)
    {
        Perfiles[perfil.Id] = new PerfilMem
        {
            Id = perfil.Id, Nombre = perfil.Nombre, Descripcion = perfil.Descripcion, Activo = perfil.Activo, Permisos = [.. permisos],
        };
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(Perfil perfil, IReadOnlyList<string> permisos, bool invalidarSesiones, CancellationToken ct = default)
    {
        var p = Perfiles[perfil.Id];
        (p.Nombre, p.Descripcion, p.Activo, p.Permisos) = (perfil.Nombre, perfil.Descripcion, perfil.Activo, [.. permisos]);
        if (invalidarSesiones)
        {
            foreach (var u in Usuarios.Values.Where(u => u.PerfilId == perfil.Id)) u.SesionInvalidaAntes = perfil.ActualizadoEn;
        }

        return Task.CompletedTask;
    }

    public Task EliminarAsync(Guid id, CancellationToken ct = default)
    {
        Perfiles.Remove(id);
        return Task.CompletedTask;
    }

    // --- IAuditoriaRepository ---
    public Task RegistrarAsync(RegistroAuditoria registro, CancellationToken ct = default)
    {
        Auditoria.Add(registro);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RegistroAuditoriaLectura>> ListarPorEntidadAsync(string entidad, Guid entidadId, int limite, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<RegistroAuditoriaLectura>>([.. Auditoria
            .Where(a => a.Entidad == entidad && a.EntidadId == entidadId)
            .Select(a => new RegistroAuditoriaLectura(a.Id, a.Entidad, a.EntidadId, a.Accion.ToString(), a.CambiosJson, a.UsuarioId, a.UsuarioNombre, a.FechaUtc))]);
}
