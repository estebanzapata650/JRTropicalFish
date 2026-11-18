using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Seguridad;
using JRTropicalFish.Domain.Auditoria;

namespace JRTropicalFish.Tests.Seguridad;

public class UsuarioServiceTests
{
    [Fact]
    public async Task Crear_normaliza_el_email_y_audita()
    {
        var mundo = new SeguridadEnMemoria();

        var resultado = await mundo.UsuarioService().CrearAsync(
            new CrearUsuarioRequest("  Luis ", " LUIS@JRTF.local ", mundo.Consulta.Id, "Clave1234"));

        Assert.True(resultado.Exitoso);
        var luis = mundo.Usuarios[resultado.Valor];
        Assert.Equal("luis@jrtf.local", luis.Email);
        Assert.Equal("Luis", luis.Nombre);
        Assert.Equal(AccionAuditoria.Crear, Assert.Single(mundo.Auditoria).Accion);
    }

    [Fact]
    public async Task No_se_repite_un_email()
    {
        var mundo = new SeguridadEnMemoria();
        mundo.AgregarUsuario("Ana", mundo.Consulta);

        var resultado = await mundo.UsuarioService().CrearAsync(
            new CrearUsuarioRequest("Otra Ana", "ana@jrtf.local", mundo.Consulta.Id, "Clave1234"));

        Assert.Equal(TipoError.Conflicto, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task No_se_asigna_un_perfil_inactivo()
    {
        var mundo = new SeguridadEnMemoria();
        var viejo = mundo.AgregarPerfil("Viejo", ["usuarios:leer"], activo: false);

        var resultado = await mundo.UsuarioService().CrearAsync(
            new CrearUsuarioRequest("Luis", "luis@jrtf.local", viejo.Id, "Clave1234"));

        Assert.Equal(TipoError.Validacion, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task Nadie_puede_desactivarse_a_si_mismo()
    {
        var mundo = new SeguridadEnMemoria();
        var admin = mundo.AgregarUsuario("Admin", mundo.Administrador);
        mundo.AgregarUsuario("Otro", mundo.Administrador);
        mundo.UsuarioActualId = admin.Id;

        var resultado = await mundo.UsuarioService().CambiarEstadoAsync(admin.Id, false);

        Assert.Equal(TipoError.Conflicto, resultado.Error!.Tipo);
        Assert.True(admin.Activo);
    }

    [Fact]
    public async Task No_se_desactiva_al_ultimo_administrador()
    {
        var mundo = new SeguridadEnMemoria();
        var admin = mundo.AgregarUsuario("Admin", mundo.Administrador);
        var operador = mundo.AgregarUsuario("Operador", mundo.Administrador);
        mundo.UsuarioActualId = operador.Id;
        operador.Activo = false; // el único otro administrador ya está inactivo

        var resultado = await mundo.UsuarioService().CambiarEstadoAsync(admin.Id, false);

        Assert.Equal(TipoError.Conflicto, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task Desactivar_revoca_las_sesiones_y_audita()
    {
        var mundo = new SeguridadEnMemoria();
        mundo.AgregarUsuario("Admin", mundo.Administrador);
        var ana = mundo.AgregarUsuario("Ana", mundo.Consulta);

        var resultado = await mundo.UsuarioService().CambiarEstadoAsync(ana.Id, false);

        Assert.True(resultado.Exitoso);
        Assert.False(ana.Activo);
        Assert.Equal(mundo.Ahora, ana.SesionInvalidaAntes);
        Assert.Contains("\"Activo\"", Assert.Single(mundo.Auditoria).CambiosJson);
    }

    [Fact]
    public async Task Quitarle_el_perfil_de_administrador_al_ultimo_se_impide()
    {
        var mundo = new SeguridadEnMemoria();
        var admin = mundo.AgregarUsuario("Admin", mundo.Administrador);

        var resultado = await mundo.UsuarioService().ActualizarAsync(
            admin.Id, new ActualizarUsuarioRequest("Admin", admin.Email, mundo.Consulta.Id, null));

        Assert.Equal(TipoError.Conflicto, resultado.Error!.Tipo);
        Assert.Equal(mundo.Administrador.Id, admin.PerfilId);
    }

    [Fact]
    public async Task Cambiar_de_perfil_revoca_sesiones_pero_editar_el_nombre_no()
    {
        var mundo = new SeguridadEnMemoria();
        mundo.AgregarUsuario("Admin", mundo.Administrador);
        var ana = mundo.AgregarUsuario("Ana", mundo.Consulta);
        var servicio = mundo.UsuarioService();

        await servicio.ActualizarAsync(ana.Id, new ActualizarUsuarioRequest("Ana María", ana.Email, mundo.Consulta.Id, null));
        Assert.Null(ana.SesionInvalidaAntes);

        await servicio.ActualizarAsync(ana.Id, new ActualizarUsuarioRequest("Ana María", ana.Email, mundo.Administrador.Id, null));
        Assert.Equal(mundo.Ahora, ana.SesionInvalidaAntes);
    }

    [Fact]
    public async Task El_historial_registra_solo_los_campos_que_cambiaron_y_la_clave_sin_revelarla()
    {
        var mundo = new SeguridadEnMemoria();
        var ana = mundo.AgregarUsuario("Ana", mundo.Consulta);

        await mundo.UsuarioService().ActualizarAsync(
            ana.Id, new ActualizarUsuarioRequest("Ana María", ana.Email, mundo.Consulta.Id, "OtraClave99"));

        var json = Assert.Single(mundo.Auditoria).CambiosJson;
        Assert.Contains("\"Nombre\"", json);
        Assert.Contains("Contraseña", json);
        Assert.DoesNotContain("\"Email\"", json);
        Assert.DoesNotContain("OtraClave99", json);
    }

    [Fact]
    public async Task Desbloquear_limpia_el_bloqueo()
    {
        var mundo = new SeguridadEnMemoria();
        var ana = mundo.AgregarUsuario("Ana", mundo.Consulta);
        ana.BloqueadoHasta = mundo.Ahora.AddMinutes(10);
        ana.IntentosFallidos = 0;

        var resultado = await mundo.UsuarioService().DesbloquearAsync(ana.Id);

        Assert.True(resultado.Exitoso);
        Assert.Null(ana.BloqueadoHasta);
        Assert.Single(mundo.Auditoria);
    }
}
