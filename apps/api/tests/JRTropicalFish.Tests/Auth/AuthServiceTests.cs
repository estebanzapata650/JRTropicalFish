using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auth;
using JRTropicalFish.Domain.Seguridad;

namespace JRTropicalFish.Tests.Auth;

public class AuthServiceTests
{
    private sealed class UsuariosFalsos(UsuarioConPerfil? usuario) : IUsuarioRepository
    {
        public Task<UsuarioConPerfil?> BuscarPorEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(usuario is not null && usuario.Email == email ? usuario : null);

        public Task<IReadOnlyList<string>> ObtenerPermisosAsync(Guid usuarioId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<string>>(["usuarios:leer"]);

        public Task CrearAsync(Usuario usuario, CancellationToken ct = default) => Task.CompletedTask;
    }

    /// <summary>Hasher trivial: la "clave hasheada" es la clave con prefijo.</summary>
    private sealed class HasherFalso : IPasswordHasher
    {
        public string Hash(string password) => "h:" + password;
        public bool Verify(string password, string hash) => hash == "h:" + password;
    }

    private sealed class TokensFalsos : IJwtTokenService
    {
        public AuthResult GenerarToken(UsuarioSesion usuario) => new("token", DateTime.UtcNow, usuario);
    }

    private static UsuarioConPerfil Ana(bool activo = true) =>
        new(Guid.CreateVersion7(), "Ana", "ana@jrtf.local", "h:secreta", Guid.CreateVersion7(), "Consulta", activo);

    private static AuthService Servicio(UsuarioConPerfil? usuario) =>
        new(new UsuariosFalsos(usuario), new HasherFalso(), new TokensFalsos());

    [Fact]
    public async Task Con_credenciales_correctas_devuelve_la_sesion_con_permisos()
    {
        var resultado = await Servicio(Ana()).LoginAsync("ana@jrtf.local", "secreta");

        Assert.NotNull(resultado);
        Assert.Equal("Consulta", resultado.Usuario.Perfil);
        Assert.Equal(["usuarios:leer"], resultado.Usuario.Permisos);
    }

    [Theory]
    [InlineData("ana@jrtf.local", "otra")]
    [InlineData("nadie@jrtf.local", "secreta")]
    [InlineData("", "secreta")]
    public async Task Cualquier_fallo_devuelve_null_sin_decir_cual(string email, string password)
    {
        Assert.Null(await Servicio(Ana()).LoginAsync(email, password));
    }

    [Fact]
    public async Task Un_usuario_inactivo_no_inicia_sesion()
    {
        Assert.Null(await Servicio(Ana(activo: false)).LoginAsync("ana@jrtf.local", "secreta"));
    }
}
