using JRTropicalFish.Api.Auth;
using JRTropicalFish.Api.Comun;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Crm;
using JRTropicalFish.Domain.Seguridad;
using JRTropicalFish.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace JRTropicalFish.Api.Controllers;

/// <summary>
/// Base de los controladores del CRM: arma el <see cref="AlcanceCrm"/> con los permisos del token.
/// Un comercial (<c>crm:vender</c>) sin <c>crm:ver-todo</c> solo ve y gestiona lo suyo.
/// </summary>
public abstract class CrmControllerBase(IUsuarioActual usuario) : ControllerBase
{
    protected AlcanceCrm Alcance => new(
        usuario.UsuarioId ?? Guid.Empty,
        User.HasClaim(JwtClaimTypes.Permiso, Permisos.CrmVender),
        User.HasClaim(JwtClaimTypes.Permiso, Permisos.CrmVerTodo));

    protected IActionResult RespuestaCreado(Resultado<Creado> resultado) =>
        resultado.Exitoso ? StatusCode(StatusCodes.Status201Created, resultado.Valor) : ResultadoHttp.Error(resultado.Error!);
}

[ApiController]
[Route("api/v1/crm/clientes")]
public class ClientesController(
    ClienteService clientes,
    OportunidadService oportunidades,
    VentaService ventas,
    TrackingService tracking,
    TareaService tareas,
    IUsuarioActual usuario) : CrmControllerBase(usuario)
{
    [HttpGet]
    [RequierePermiso(Permisos.ClientesLeer)]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda, CancellationToken ct) =>
        Ok(await clientes.ListarAsync(busqueda, ct));

    [HttpGet("{id:guid}")]
    [RequierePermiso(Permisos.ClientesLeer)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct) => (await clientes.ObtenerAsync(id, ct)).ARespuesta();

    /// <summary>Oportunidades, ventas, vuelos y tareas del cliente (según el alcance de quien pide).</summary>
    [HttpGet("{id:guid}/relacionados")]
    [RequierePermiso(Permisos.ClientesLeer)]
    [RequierePermiso(Permisos.CrmLeer)]
    public async Task<IActionResult> Relacionados(Guid id, CancellationToken ct)
    {
        var cliente = await clientes.ObtenerAsync(id, ct);
        if (!cliente.Exitoso)
        {
            return ResultadoHttp.Error(cliente.Error!);
        }

        var alcance = Alcance;
        return Ok(new ClienteRelacionados(
            await oportunidades.ListarAsync(alcance, id, ct),
            await ventas.ListarAsync(alcance, id, ct: ct),
            await tracking.ListarAsync(alcance, id, ct),
            await tareas.ListarAsync(alcance, id, ct)));
    }

    [HttpGet("{id:guid}/historial")]
    [RequierePermiso(Permisos.ClientesLeer)]
    public async Task<IActionResult> Historial(Guid id, CancellationToken ct) => (await clientes.HistorialAsync(id, ct)).ARespuesta();

    [HttpPost]
    [RequierePermiso(Permisos.ClientesEditar)]
    public async Task<IActionResult> Crear(GuardarClienteRequest request, CancellationToken ct) =>
        RespuestaCreado(await clientes.CrearAsync(request, ct));

    [HttpPut("{id:guid}")]
    [RequierePermiso(Permisos.ClientesEditar)]
    public async Task<IActionResult> Actualizar(Guid id, GuardarClienteRequest request, CancellationToken ct) =>
        (await clientes.ActualizarAsync(id, request, ct)).ARespuesta();

    [HttpDelete("{id:guid}")]
    [RequierePermiso(Permisos.ClientesEditar)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct) => (await clientes.EliminarAsync(id, ct)).ARespuesta();
}

[ApiController]
[Route("api/v1/crm/comerciales")]
public class ComercialesController(OportunidadService oportunidades) : ControllerBase
{
    /// <summary>Usuarios habilitados como comerciales (para selectores y metas).</summary>
    [HttpGet]
    [RequierePermiso(Permisos.CrmLeer)]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await oportunidades.ComercialesAsync(ct));
}

[ApiController]
[Route("api/v1/crm/oportunidades")]
public class OportunidadesController(OportunidadService service, IUsuarioActual usuario) : CrmControllerBase(usuario)
{
    [HttpGet]
    [RequierePermiso(Permisos.CrmLeer)]
    public async Task<IActionResult> Listar([FromQuery] Guid? clienteId, CancellationToken ct) =>
        Ok(await service.ListarAsync(Alcance, clienteId, ct));

    [HttpGet("{id:guid}")]
    [RequierePermiso(Permisos.CrmLeer)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct) => (await service.ObtenerAsync(id, Alcance, ct)).ARespuesta();

    [HttpGet("{id:guid}/historial")]
    [RequierePermiso(Permisos.CrmLeer)]
    public async Task<IActionResult> Historial(Guid id, CancellationToken ct) =>
        (await service.HistorialAsync(id, Alcance, ct)).ARespuesta();

    [HttpPost]
    [RequierePermiso(Permisos.CrmVender)]
    public async Task<IActionResult> Crear(GuardarOportunidadRequest request, CancellationToken ct) =>
        RespuestaCreado(await service.CrearAsync(request, Alcance, ct));

    [HttpPut("{id:guid}")]
    [RequierePermiso(Permisos.CrmVender)]
    public async Task<IActionResult> Actualizar(Guid id, GuardarOportunidadRequest request, CancellationToken ct) =>
        (await service.ActualizarAsync(id, request, Alcance, ct)).ARespuesta();

    /// <summary>Avanzar o marcar Perdida. Nunca retrocede; Ganada va por <c>/ganar</c>.</summary>
    [HttpPut("{id:guid}/etapa")]
    [RequierePermiso(Permisos.CrmVender)]
    public async Task<IActionResult> CambiarEtapa(Guid id, CambiarEtapaRequest request, CancellationToken ct) =>
        (await service.CambiarEtapaAsync(id, request, Alcance, ct)).ARespuesta();

    /// <summary>Cierra como ganada y crea la venta (y el tracking si es exportación).</summary>
    [HttpPost("{id:guid}/ganar")]
    [RequierePermiso(Permisos.CrmVender)]
    public async Task<IActionResult> Ganar(Guid id, GanarOportunidadRequest request, CancellationToken ct) =>
        (await service.GanarAsync(id, request, Alcance, ct)).ARespuesta();

    [HttpDelete("{id:guid}")]
    [RequierePermiso(Permisos.CrmVender)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct) => (await service.EliminarAsync(id, Alcance, ct)).ARespuesta();
}

[ApiController]
[Route("api/v1/crm/ventas")]
public class VentasController(VentaService service, IUsuarioActual usuario) : CrmControllerBase(usuario)
{
    [HttpGet]
    [RequierePermiso(Permisos.CrmLeer)]
    public async Task<IActionResult> Listar(
        [FromQuery] Guid? clienteId, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct) =>
        Ok(await service.ListarAsync(Alcance, clienteId, desde, hasta, ct));

    [HttpGet("{id:guid}")]
    [RequierePermiso(Permisos.CrmLeer)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct) => (await service.ObtenerAsync(id, Alcance, ct)).ARespuesta();

    [HttpGet("{id:guid}/historial")]
    [RequierePermiso(Permisos.CrmLeer)]
    public async Task<IActionResult> Historial(Guid id, CancellationToken ct) =>
        (await service.HistorialAsync(id, Alcance, ct)).ARespuesta();

    /// <summary>Venta directa (sin oportunidad): mostrador, web, WhatsApp.</summary>
    [HttpPost]
    [RequierePermiso(Permisos.VentasGestionar)]
    public async Task<IActionResult> Crear(GuardarVentaRequest request, CancellationToken ct) =>
        RespuestaCreado(await service.CrearAsync(request, Alcance, ct));

    [HttpPut("{id:guid}")]
    [RequierePermiso(Permisos.VentasGestionar)]
    public async Task<IActionResult> Actualizar(Guid id, GuardarVentaRequest request, CancellationToken ct) =>
        (await service.ActualizarAsync(id, request, Alcance, ct)).ARespuesta();
}

[ApiController]
[Route("api/v1/crm/tracking")]
public class TrackingController(TrackingService service, IUsuarioActual usuario) : CrmControllerBase(usuario)
{
    [HttpGet]
    [RequierePermiso(Permisos.CrmLeer)]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await service.ListarAsync(Alcance, ct: ct));

    [HttpGet("{id:guid}")]
    [RequierePermiso(Permisos.CrmLeer)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct) => (await service.ObtenerAsync(id, Alcance, ct)).ARespuesta();

    [HttpGet("{id:guid}/historial")]
    [RequierePermiso(Permisos.CrmLeer)]
    public async Task<IActionResult> Historial(Guid id, CancellationToken ct) =>
        (await service.HistorialAsync(id, Alcance, ct)).ARespuesta();

    [HttpPut("{id:guid}")]
    [RequierePermiso(Permisos.TrackingGestionar)]
    public async Task<IActionResult> Actualizar(Guid id, GuardarTrackingRequest request, CancellationToken ct) =>
        (await service.ActualizarAsync(id, request, Alcance, ct)).ARespuesta();

    [HttpPut("{id:guid}/fase")]
    [RequierePermiso(Permisos.TrackingGestionar)]
    public async Task<IActionResult> CambiarFase(Guid id, CambiarFaseRequest request, CancellationToken ct) =>
        (await service.CambiarFaseAsync(id, request, Alcance, ct)).ARespuesta();
}

[ApiController]
[Route("api/v1/crm/tareas")]
public class TareasController(TareaService service, IUsuarioActual usuario) : CrmControllerBase(usuario)
{
    [HttpGet]
    [RequierePermiso(Permisos.CrmLeer)]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await service.ListarAsync(Alcance, ct: ct));

    [HttpGet("{id:guid}")]
    [RequierePermiso(Permisos.CrmLeer)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct) => (await service.ObtenerAsync(id, Alcance, ct)).ARespuesta();

    [HttpGet("{id:guid}/historial")]
    [RequierePermiso(Permisos.CrmLeer)]
    public async Task<IActionResult> Historial(Guid id, CancellationToken ct) =>
        (await service.HistorialAsync(id, Alcance, ct)).ARespuesta();

    [HttpPost]
    [RequierePermiso(Permisos.CrmVender)]
    public async Task<IActionResult> Crear(GuardarTareaRequest request, CancellationToken ct) =>
        RespuestaCreado(await service.CrearAsync(request, Alcance, ct));

    [HttpPut("{id:guid}")]
    [RequierePermiso(Permisos.CrmVender)]
    public async Task<IActionResult> Actualizar(Guid id, GuardarTareaRequest request, CancellationToken ct) =>
        (await service.ActualizarAsync(id, request, Alcance, ct)).ARespuesta();

    [HttpPut("{id:guid}/estado")]
    [RequierePermiso(Permisos.CrmVender)]
    public async Task<IActionResult> CambiarEstado(Guid id, CambiarEstadoTareaRequest request, CancellationToken ct) =>
        (await service.CambiarEstadoAsync(id, request, Alcance, ct)).ARespuesta();

    [HttpDelete("{id:guid}")]
    [RequierePermiso(Permisos.CrmVender)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct) => (await service.EliminarAsync(id, Alcance, ct)).ARespuesta();
}

[ApiController]
[Route("api/v1/crm/metricas")]
public class MetricasController(MetricasService service, IUsuarioActual usuario) : CrmControllerBase(usuario)
{
    /// <summary>Dashboard ejecutivo del mes (<c>?mes=2026-11-01</c>; por defecto el mes actual).</summary>
    [HttpGet("dashboard")]
    [RequierePermiso(Permisos.MetasLeer)]
    public async Task<IActionResult> Dashboard([FromQuery] DateOnly? mes, CancellationToken ct) =>
        Ok(await service.DashboardAsync(mes, Alcance, ct));

    [HttpGet("parametros")]
    [RequierePermiso(Permisos.MetasLeer)]
    public async Task<IActionResult> Parametros(CancellationToken ct) => Ok(await service.ParametrosAsync(ct));

    [HttpPut("parametros")]
    [RequierePermiso(Permisos.MetasEditar)]
    public async Task<IActionResult> ActualizarParametros(GuardarParametrosRequest request, CancellationToken ct) =>
        (await service.ActualizarParametrosAsync(request, ct)).ARespuesta();

    [HttpGet("metas")]
    [RequierePermiso(Permisos.MetasLeer)]
    public async Task<IActionResult> Metas([FromQuery] DateOnly mes, CancellationToken ct) =>
        Ok(await service.MetasDelMesAsync(mes, ct));

    [HttpPut("metas")]
    [RequierePermiso(Permisos.MetasEditar)]
    public async Task<IActionResult> GuardarMetas(GuardarMetasRequest request, CancellationToken ct) =>
        (await service.GuardarMetasAsync(request, ct)).ARespuesta();
}
