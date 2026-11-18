using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Seguridad;
using JRTropicalFish.Domain.Seguridad;

namespace JRTropicalFish.Tests.Seguridad;

public class PerfilServiceTests
{
    [Fact]
    public async Task Crear_descarta_permisos_inventados_y_exige_al_menos_uno_real()
    {
        var mundo = new SeguridadEnMemoria();
        var servicio = mundo.PerfilService();

        var soloInventados = await servicio.CrearAsync(new GuardarPerfilRequest("Bodega", null, true, ["bodega:volar"]));
        Assert.Equal(TipoError.Validacion, soloInventados.Error!.Tipo);

        var mezclado = await servicio.CrearAsync(
            new GuardarPerfilRequest("Bodega", null, true, ["bodega:volar", Permisos.UsuariosLeer, Permisos.UsuariosLeer]));
        Assert.True(mezclado.Exitoso);
        Assert.Equal([Permisos.UsuariosLeer], mundo.Perfiles[mezclado.Valor].Permisos);
    }

    [Fact]
    public async Task El_nombre_no_se_repite_sin_importar_mayusculas()
    {
        var mundo = new SeguridadEnMemoria();

        var resultado = await mundo.PerfilService().CrearAsync(new GuardarPerfilRequest("consulta", null, true, [Permisos.UsuariosLeer]));

        Assert.Equal(TipoError.Conflicto, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task Un_perfil_base_no_se_elimina_ni_se_renombra()
    {
        var mundo = new SeguridadEnMemoria();
        var servicio = mundo.PerfilService();

        Assert.Equal(TipoError.Conflicto, (await servicio.EliminarAsync(mundo.Consulta.Id)).Error!.Tipo);

        var renombrar = await servicio.ActualizarAsync(
            mundo.Consulta.Id, new GuardarPerfilRequest("Lectores", null, true, mundo.Consulta.Permisos));
        Assert.Equal(TipoError.Validacion, renombrar.Error!.Tipo);
    }

    [Fact]
    public async Task Un_perfil_con_usuarios_no_se_elimina()
    {
        var mundo = new SeguridadEnMemoria();
        var bodega = mundo.AgregarPerfil("Bodega", [Permisos.UsuariosLeer]);
        mundo.AgregarUsuario("Wilmer", bodega);

        var resultado = await mundo.PerfilService().EliminarAsync(bodega.Id);

        Assert.Equal(TipoError.Conflicto, resultado.Error!.Tipo);
        Assert.True(mundo.Perfiles.ContainsKey(bodega.Id));
    }

    [Fact]
    public async Task Quitar_la_administracion_al_unico_perfil_que_la_da_se_impide()
    {
        var mundo = new SeguridadEnMemoria();
        mundo.AgregarUsuario("Admin", mundo.Administrador);

        var resultado = await mundo.PerfilService().ActualizarAsync(
            mundo.Administrador.Id,
            new GuardarPerfilRequest(PerfilesPlantilla.Administrador, null, true, [Permisos.UsuariosLeer]));

        Assert.Equal(TipoError.Conflicto, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task Cambiar_permisos_revoca_las_sesiones_de_sus_usuarios_y_audita_el_conjunto()
    {
        var mundo = new SeguridadEnMemoria();
        mundo.AgregarUsuario("Admin", mundo.Administrador);
        var ana = mundo.AgregarUsuario("Ana", mundo.Consulta);

        var resultado = await mundo.PerfilService().ActualizarAsync(
            mundo.Consulta.Id,
            new GuardarPerfilRequest(PerfilesPlantilla.Consulta, "Solo lectura", true, [Permisos.UsuariosLeer]));

        Assert.True(resultado.Exitoso);
        Assert.Equal(mundo.Ahora, ana.SesionInvalidaAntes);
        var json = Assert.Single(mundo.Auditoria).CambiosJson;
        Assert.Contains("Permisos", json);
        Assert.Contains("Descripcion", json);
    }

    [Fact]
    public async Task Cambiar_solo_la_descripcion_no_saca_a_nadie()
    {
        var mundo = new SeguridadEnMemoria();
        var ana = mundo.AgregarUsuario("Ana", mundo.Consulta);

        await mundo.PerfilService().ActualizarAsync(
            mundo.Consulta.Id,
            new GuardarPerfilRequest(PerfilesPlantilla.Consulta, "Nueva descripción", true, mundo.Consulta.Permisos));

        Assert.Null(ana.SesionInvalidaAntes);
    }
}
