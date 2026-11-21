using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Maestros;
using JRTropicalFish.Domain.Auditoria;

namespace JRTropicalFish.Tests.Administracion;

public class EmpleadoServiceTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0, 1, 2, 3];

    [Fact]
    public async Task Crear_normaliza_y_audita()
    {
        var mundo = new AdministracionEnMemoria();

        var resultado = await mundo.EmpleadoService().CrearAsync(
            mundo.Solicitud(numero: " 1.020.304 050 ") with { Nombres = "  Ana   María " }, verSalario: true);

        Assert.True(resultado.Exitoso);
        var ana = mundo.Empleados[resultado.Valor];
        Assert.Equal("1020304050", ana.NumeroIdentificacion);
        Assert.Equal("Ana María", ana.Nombres);
        Assert.Equal("ana@correo.com", ana.Email);
        Assert.Equal(2_000_000m, ana.Salario);
        Assert.Equal(AccionAuditoria.Crear, Assert.Single(mundo.Auditoria).Accion);
    }

    [Fact]
    public async Task No_se_repite_la_identificacion()
    {
        var mundo = new AdministracionEnMemoria();
        var servicio = mundo.EmpleadoService();
        await servicio.CrearAsync(mundo.Solicitud(), verSalario: true);

        var resultado = await servicio.CrearAsync(mundo.Solicitud(numero: "1020304050"), verSalario: true);

        Assert.Equal(TipoError.Conflicto, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task Valida_fechas()
    {
        var mundo = new AdministracionEnMemoria();
        var servicio = mundo.EmpleadoService();

        var futura = await servicio.CrearAsync(mundo.Solicitud() with { FechaNacimiento = new DateOnly(2030, 1, 1) }, true);
        var menor = await servicio.CrearAsync(mundo.Solicitud() with { FechaNacimiento = new DateOnly(2015, 1, 1) }, true);
        var retiro = await servicio.CrearAsync(mundo.Solicitud() with { FechaRetiro = new DateOnly(2023, 1, 1) }, true);
        var sinIngreso = await servicio.CrearAsync(mundo.Solicitud() with { FechaIngreso = default }, true);

        Assert.All([futura, menor, retiro, sinIngreso], r => Assert.Equal(TipoError.Validacion, r.Error!.Tipo));
    }

    [Fact]
    public async Task No_se_elige_un_maestro_inactivo_pero_se_conserva_el_que_ya_tiene()
    {
        var mundo = new AdministracionEnMemoria();
        var servicio = mundo.EmpleadoService();
        var creado = await servicio.CrearAsync(mundo.Solicitud(), true);
        mundo.Operario.Activo = false;

        var editar = await servicio.ActualizarAsync(creado.Valor, mundo.Solicitud() with { Ciudad = "Cali" }, true);
        var nuevo = await servicio.CrearAsync(mundo.Solicitud(numero: "999888"), true);

        Assert.True(editar.Exitoso);
        Assert.Equal(TipoError.Validacion, nuevo.Error!.Tipo);
    }

    [Fact]
    public async Task Sin_permiso_no_se_ve_ni_se_fija_el_salario()
    {
        var mundo = new AdministracionEnMemoria();
        var servicio = mundo.EmpleadoService();

        var conSalario = await servicio.CrearAsync(mundo.Solicitud(), verSalario: false);
        var sinSalario = await servicio.CrearAsync(mundo.Solicitud(salario: null), verSalario: false);
        var leido = await servicio.ObtenerAsync(sinSalario.Valor, verSalario: false);

        Assert.Equal(TipoError.Prohibido, conSalario.Error!.Tipo);
        Assert.Equal(0m, mundo.Empleados[sinSalario.Valor].Salario);
        Assert.Null(leido.Valor.Salario);
    }

    [Fact]
    public async Task Sin_permiso_editar_conserva_el_salario_y_el_historial_lo_oculta()
    {
        var mundo = new AdministracionEnMemoria();
        var servicio = mundo.EmpleadoService();
        var id = (await servicio.CrearAsync(mundo.Solicitud(), verSalario: true)).Valor;
        await servicio.ActualizarAsync(id, mundo.Solicitud(salario: 3_000_000m), verSalario: true);

        var editar = await servicio.ActualizarAsync(id, mundo.Solicitud(salario: null) with { Ciudad = "Cali" }, verSalario: false);
        var historial = await servicio.HistorialAsync(id, verSalario: false);
        var historialCompleto = await servicio.HistorialAsync(id, verSalario: true);

        Assert.True(editar.Exitoso);
        Assert.Equal(3_000_000m, mundo.Empleados[id].Salario);
        Assert.DoesNotContain(historial, r => r.CambiosJson.Contains("Salario"));
        Assert.Equal(3, historialCompleto.Count);
        Assert.Equal(2, historial.Count); // la edición que solo cambió el salario desaparece
    }

    [Fact]
    public async Task La_foto_se_valida_por_contenido_y_reemplaza_la_anterior()
    {
        var mundo = new AdministracionEnMemoria();
        var servicio = mundo.EmpleadoService();
        var id = (await servicio.CrearAsync(mundo.Solicitud(), true)).Valor;

        var texto = await servicio.CambiarFotoAsync(id, new MemoryStream("hola, no soy imagen"u8.ToArray()));
        var primera = await servicio.CambiarFotoAsync(id, new MemoryStream(Png));
        var segunda = await servicio.CambiarFotoAsync(id, new MemoryStream(Png));

        Assert.Equal(TipoError.Validacion, texto.Error!.Tipo);
        Assert.NotEqual(primera.Valor, segunda.Valor);
        var clave = Assert.Single(mundo.Archivos).Key;
        Assert.StartsWith($"empleados/{id:N}/", clave);
        Assert.EndsWith(".png", clave);
        Assert.Equal(clave, mundo.Empleados[id].FotoClave);
    }

    [Fact]
    public async Task Eliminar_es_logico_y_libera_la_identificacion()
    {
        var mundo = new AdministracionEnMemoria();
        var servicio = mundo.EmpleadoService();
        var id = (await servicio.CrearAsync(mundo.Solicitud(), true)).Valor;

        await servicio.EliminarAsync(id);
        var obtener = await servicio.ObtenerAsync(id, true);
        var otraVez = await servicio.CrearAsync(mundo.Solicitud(), true);

        Assert.NotNull(mundo.Empleados[id].EliminadoEn);
        Assert.Equal(TipoError.NoEncontrado, obtener.Error!.Tipo);
        Assert.True(otraVez.Exitoso);
    }

    [Fact]
    public async Task El_area_es_opcional_pero_si_viene_debe_existir()
    {
        var mundo = new AdministracionEnMemoria();

        var resultado = await mundo.EmpleadoService().CrearAsync(mundo.Solicitud() with { AreaId = Guid.NewGuid() }, true);

        Assert.Equal(TipoError.Validacion, resultado.Error!.Tipo);
        Assert.Contains(RegistroMaestros.Obtener(RegistroMaestros.Areas).NombreSingular, resultado.Error.Mensaje);
    }
}
