using JRTropicalFish.Application.Auth;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Seguridad;

namespace JRTropicalFish.Tests.Seguridad;

public class AuthServiceTests
{
    [Fact]
    public async Task Con_credenciales_correctas_devuelve_la_sesion_y_registra_el_acceso()
    {
        var mundo = new SeguridadEnMemoria();
        var ana = mundo.AgregarUsuario("Ana", mundo.Consulta);

        var resultado = await mundo.AuthService().LoginAsync("ANA@jrtf.local ", "Clave1234");

        Assert.True(resultado.Exitoso);
        Assert.Equal("Consulta", resultado.Valor.Usuario.Perfil);
        Assert.Contains("usuarios:leer", resultado.Valor.Usuario.Permisos);
        Assert.Equal(mundo.Ahora, ana.UltimoAccesoEn);
    }

    [Theory]
    [InlineData("ana@jrtf.local", "otra")]
    [InlineData("nadie@jrtf.local", "Clave1234")]
    [InlineData("", "Clave1234")]
    public async Task Cualquier_credencial_invalida_da_el_mismo_mensaje(string email, string password)
    {
        var mundo = new SeguridadEnMemoria();
        mundo.AgregarUsuario("Ana", mundo.Consulta);

        var resultado = await mundo.AuthService().LoginAsync(email, password);

        Assert.Equal(TipoError.NoAutenticado, resultado.Error!.Tipo);
        Assert.Equal(AuthService.MensajeCredencialesInvalidas, resultado.Error.Mensaje);
    }

    [Fact]
    public async Task Un_usuario_inactivo_no_entra_aunque_la_clave_sea_correcta()
    {
        var mundo = new SeguridadEnMemoria();
        mundo.AgregarUsuario("Ana", mundo.Consulta, activo: false);

        var resultado = await mundo.AuthService().LoginAsync("ana@jrtf.local", "Clave1234");

        Assert.Equal(TipoError.Prohibido, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task Al_quinto_fallo_la_cuenta_se_bloquea_y_ni_la_clave_correcta_entra()
    {
        var mundo = new SeguridadEnMemoria();
        var ana = mundo.AgregarUsuario("Ana", mundo.Consulta);
        var auth = mundo.AuthService();

        for (var i = 0; i < ReglasBloqueo.IntentosMaximos - 1; i++)
        {
            Assert.Equal(TipoError.NoAutenticado, (await auth.LoginAsync("ana@jrtf.local", "mal")).Error!.Tipo);
        }

        var quinto = await auth.LoginAsync("ana@jrtf.local", "mal");
        Assert.Equal(TipoError.Prohibido, quinto.Error!.Tipo);
        Assert.Equal(mundo.Ahora + ReglasBloqueo.DuracionBloqueo, ana.BloqueadoHasta);

        var conClaveCorrecta = await auth.LoginAsync("ana@jrtf.local", "Clave1234");
        Assert.Equal(TipoError.Prohibido, conClaveCorrecta.Error!.Tipo);
        Assert.Contains("15 minuto", conClaveCorrecta.Error.Mensaje);
    }

    [Fact]
    public async Task Pasado_el_bloqueo_vuelve_a_entrar_y_se_limpia_el_contador()
    {
        var mundo = new SeguridadEnMemoria();
        var ana = mundo.AgregarUsuario("Ana", mundo.Consulta);
        ana.BloqueadoHasta = mundo.Ahora.AddMinutes(-1);
        ana.IntentosFallidos = 3;

        var resultado = await mundo.AuthService().LoginAsync("ana@jrtf.local", "Clave1234");

        Assert.True(resultado.Exitoso);
        Assert.Equal(0, ana.IntentosFallidos);
        Assert.Null(ana.BloqueadoHasta);
    }

    [Fact]
    public async Task Cambiar_la_propia_clave_exige_la_actual_y_revoca_las_otras_sesiones()
    {
        var mundo = new SeguridadEnMemoria();
        var ana = mundo.AgregarUsuario("Ana", mundo.Consulta);
        var auth = mundo.AuthService();

        var conActualMala = await auth.CambiarPasswordAsync(ana.Id, new CambiarPasswordRequest("otra", "Nueva12345"));
        Assert.Equal(TipoError.Validacion, conActualMala.Error!.Tipo);

        var debil = await auth.CambiarPasswordAsync(ana.Id, new CambiarPasswordRequest("Clave1234", "soloLetras"));
        Assert.Equal(TipoError.Validacion, debil.Error!.Tipo);

        var bien = await auth.CambiarPasswordAsync(ana.Id, new CambiarPasswordRequest("Clave1234", "Nueva12345"));
        Assert.True(bien.Exitoso);
        Assert.True(mundo.Verify("Nueva12345", ana.PasswordHash));
        Assert.Equal(mundo.Ahora, ana.SesionInvalidaAntes);
        Assert.Single(mundo.Auditoria);
    }
}
