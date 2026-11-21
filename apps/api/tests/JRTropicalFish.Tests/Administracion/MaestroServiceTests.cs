using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Maestros;
using JRTropicalFish.Domain.Auditoria;

namespace JRTropicalFish.Tests.Administracion;

public class MaestroServiceTests
{
    [Fact]
    public async Task Una_clave_desconocida_no_existe()
    {
        var mundo = new AdministracionEnMemoria();

        var resultado = await mundo.MaestroService().CrearAsync("usuarios", new GuardarValorMaestroRequest("X", "Equis", true));

        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task Crear_normaliza_el_codigo_y_audita()
    {
        var mundo = new AdministracionEnMemoria();

        var resultado = await mundo.MaestroService().CrearAsync(
            RegistroMaestros.Cargos, new GuardarValorMaestroRequest(" sup ", "  Supervisor   de  planta ", true));

        Assert.True(resultado.Exitoso);
        var valor = mundo.Maestros["cargos"].Single(v => v.Id == resultado.Valor);
        Assert.Equal("SUP", valor.Codigo);
        Assert.Equal("Supervisor de planta", valor.Nombre);
        var registro = Assert.Single(mundo.Auditoria);
        Assert.Equal(AccionAuditoria.Crear, registro.Accion);
        Assert.Equal("Maestro:cargos", registro.Entidad);
    }

    [Fact]
    public async Task No_se_repite_el_codigo_ni_el_nombre()
    {
        var mundo = new AdministracionEnMemoria();
        var servicio = mundo.MaestroService();

        var codigo = await servicio.CrearAsync(RegistroMaestros.Cargos, new GuardarValorMaestroRequest("ope", "Otro", true));
        var nombre = await servicio.CrearAsync(RegistroMaestros.Cargos, new GuardarValorMaestroRequest("OTR", "operario", true));

        Assert.Equal(TipoError.Conflicto, codigo.Error!.Tipo);
        Assert.Equal(TipoError.Conflicto, nombre.Error!.Tipo);
    }

    [Fact]
    public async Task Un_valor_eliminado_libera_su_codigo()
    {
        var mundo = new AdministracionEnMemoria();
        var servicio = mundo.MaestroService();

        await servicio.EliminarAsync(RegistroMaestros.Cargos, mundo.Operario.Id);
        var resultado = await servicio.CrearAsync(RegistroMaestros.Cargos, new GuardarValorMaestroRequest("OPE", "Operario", true));

        Assert.True(resultado.Exitoso);
        Assert.Equal(AccionAuditoria.Eliminar, mundo.Auditoria[0].Accion);
    }

    [Fact]
    public async Task El_codigo_solo_admite_letras_numeros_y_guiones()
    {
        var mundo = new AdministracionEnMemoria();

        var resultado = await mundo.MaestroService().CrearAsync(
            RegistroMaestros.Areas, new GuardarValorMaestroRequest("A B", "Bodega", true));

        Assert.Equal(TipoError.Validacion, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task Cambiar_estado_audita_solo_si_cambia()
    {
        var mundo = new AdministracionEnMemoria();
        var servicio = mundo.MaestroService();

        await servicio.CambiarEstadoAsync(RegistroMaestros.Cargos, mundo.Operario.Id, true);
        await servicio.CambiarEstadoAsync(RegistroMaestros.Cargos, mundo.Operario.Id, false);

        Assert.False(mundo.Operario.Activo);
        Assert.Single(mundo.Auditoria);
    }
}
