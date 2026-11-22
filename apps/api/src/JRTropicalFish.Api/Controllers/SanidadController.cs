using JRTropicalFish.Api.Auth;
using JRTropicalFish.Api.Comun;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Sanidad;
using JRTropicalFish.Domain.Seguridad;
using JRTropicalFish.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace JRTropicalFish.Api.Controllers;

/// <summary>Maestros de sanidad: medicamentos, alimentos, dietas, fichas técnicas, protocolos, parámetros y responsables.</summary>
[ApiController]
[Route("api/v1/sanidad")]
public class SanidadMaestrosController(SanidadMaestrosService service, ServicioAuditoria auditoria) : ControllerBase
{
    /// <summary>Entidades con historial consultable desde sanidad.</summary>
    private static readonly HashSet<string> EntidadesHistorial =
    [
        SanidadMaestrosService.EntidadMedicamento, SanidadMaestrosService.EntidadAlimento, SanidadMaestrosService.EntidadDieta,
        SanidadMaestrosService.EntidadFicha, SanidadMaestrosService.EntidadProtocolo, SanidadMaestrosService.EntidadParametros,
        SanidadService.EntidadTratamiento,
    ];

    [HttpGet("medicamentos")]
    [RequierePermiso(Permisos.SanidadLeer)]
    public async Task<IActionResult> Medicamentos(CancellationToken ct) => Ok(await service.MedicamentosAsync(ct));

    [HttpPost("medicamentos")]
    [RequierePermiso(Permisos.SanidadGestionar)]
    public async Task<IActionResult> CrearMedicamento(GuardarMedicamentoRequest r, CancellationToken ct) =>
        Creado(await service.GuardarMedicamentoAsync(null, r, ct));

    [HttpPut("medicamentos/{id:guid}")]
    [RequierePermiso(Permisos.SanidadGestionar)]
    public async Task<IActionResult> EditarMedicamento(Guid id, GuardarMedicamentoRequest r, CancellationToken ct) =>
        Creado(await service.GuardarMedicamentoAsync(id, r, ct));

    [HttpGet("alimentos")]
    [RequierePermiso(Permisos.SanidadLeer)]
    public async Task<IActionResult> Alimentos(CancellationToken ct) => Ok(await service.AlimentosAsync(ct));

    [HttpPost("alimentos")]
    [RequierePermiso(Permisos.SanidadGestionar)]
    public async Task<IActionResult> CrearAlimento(GuardarAlimentoRequest r, CancellationToken ct) =>
        Creado(await service.GuardarAlimentoAsync(null, r, ct));

    [HttpPut("alimentos/{id:guid}")]
    [RequierePermiso(Permisos.SanidadGestionar)]
    public async Task<IActionResult> EditarAlimento(Guid id, GuardarAlimentoRequest r, CancellationToken ct) =>
        Creado(await service.GuardarAlimentoAsync(id, r, ct));

    [HttpGet("dietas")]
    [RequierePermiso(Permisos.SanidadLeer)]
    public async Task<IActionResult> Dietas(CancellationToken ct) => Ok(await service.DietasAsync(ct));

    [HttpPost("dietas")]
    [RequierePermiso(Permisos.SanidadGestionar)]
    public async Task<IActionResult> CrearDieta(GuardarDietaRequest r, CancellationToken ct) => Creado(await service.GuardarDietaAsync(null, r, ct));

    [HttpPut("dietas/{id:guid}")]
    [RequierePermiso(Permisos.SanidadGestionar)]
    public async Task<IActionResult> EditarDieta(Guid id, GuardarDietaRequest r, CancellationToken ct) =>
        Creado(await service.GuardarDietaAsync(id, r, ct));

    [HttpDelete("dietas/{id:guid}")]
    [RequierePermiso(Permisos.SanidadGestionar)]
    public async Task<IActionResult> EliminarDieta(Guid id, CancellationToken ct) => (await service.EliminarDietaAsync(id, ct)).ARespuesta();

    [HttpGet("fichas")]
    [RequierePermiso(Permisos.SanidadLeer)]
    public async Task<IActionResult> Fichas(CancellationToken ct) => Ok(await service.FichasAsync(ct));

    [HttpGet("fichas/{productoId:guid}")]
    [RequierePermiso(Permisos.SanidadLeer)]
    public async Task<IActionResult> Ficha(Guid productoId, CancellationToken ct) => (await service.FichaAsync(productoId, ct)).ARespuesta();

    [HttpPut("fichas/{productoId:guid}")]
    [RequierePermiso(Permisos.SanidadGestionar)]
    public async Task<IActionResult> GuardarFicha(Guid productoId, GuardarFichaRequest r, CancellationToken ct) =>
        (await service.GuardarFichaAsync(productoId, r, ct)).ARespuesta();

    [HttpGet("protocolos")]
    [RequierePermiso(Permisos.SanidadLeer)]
    public async Task<IActionResult> Protocolos(CancellationToken ct) => Ok(await service.ProtocolosAsync(ct));

    [HttpGet("protocolos/{id:guid}")]
    [RequierePermiso(Permisos.SanidadLeer)]
    public async Task<IActionResult> Protocolo(Guid id, CancellationToken ct) => (await service.ProtocoloAsync(id, ct)).ARespuesta();

    [HttpPost("protocolos")]
    [RequierePermiso(Permisos.SanidadGestionar)]
    public async Task<IActionResult> CrearProtocolo(GuardarProtocoloRequest r, CancellationToken ct) =>
        Creado(await service.GuardarProtocoloAsync(null, r, ct));

    [HttpPut("protocolos/{id:guid}")]
    [RequierePermiso(Permisos.SanidadGestionar)]
    public async Task<IActionResult> EditarProtocolo(Guid id, GuardarProtocoloRequest r, CancellationToken ct) =>
        Creado(await service.GuardarProtocoloAsync(id, r, ct));

    /// <summary>Aprobar exige <c>sanidad:aprobar-protocolo</c>; inactivar o reactivar, <c>sanidad:gestionar</c>.</summary>
    [HttpPut("protocolos/{id:guid}/estado")]
    [RequierePermiso(Permisos.SanidadGestionar)]
    public async Task<IActionResult> EstadoProtocolo(Guid id, CambiarEstadoRequest r, CancellationToken ct)
    {
        if (r.Estado == Domain.Sanidad.EstadosProtocolo.Aprobado && !Tiene(Permisos.SanidadAprobarProtocolo))
        {
            return ResultadoHttp.Error(Error.Prohibido("Aprobar protocolos es del veterinario o gerencia (permiso sanidad:aprobar-protocolo)."));
        }

        return (await service.CambiarEstadoProtocoloAsync(id, r, ct)).ARespuesta();
    }

    [HttpGet("parametros")]
    [RequierePermiso(Permisos.SanidadLeer)]
    public async Task<IActionResult> Parametros(CancellationToken ct) => Ok(await service.ParametrosAsync(ct));

    [HttpPut("parametros")]
    [RequierePermiso(Permisos.SanidadGestionar)]
    public async Task<IActionResult> GuardarParametros(ParametrosSanidadInfo r, CancellationToken ct) =>
        (await service.ActualizarParametrosAsync(r, ct)).ARespuesta();

    [HttpPut("acuarios/{id:guid}/responsable")]
    [RequierePermiso(Permisos.SanidadGestionar)]
    public async Task<IActionResult> Responsable(Guid id, AsignarResponsableRequest r, CancellationToken ct) =>
        (await service.AsignarResponsableAsync(id, r.ResponsableId, ct)).ARespuesta();

    [HttpGet("historial/{entidad}/{id:guid}")]
    [RequierePermiso(Permisos.SanidadLeer)]
    public async Task<IActionResult> Historial(string entidad, Guid id, CancellationToken ct) =>
        EntidadesHistorial.Contains(entidad) ? Ok(await auditoria.HistorialAsync(entidad, id, ct)) : NotFound(new { error = "No hay historial para eso." });

    private bool Tiene(string permiso) => User.HasClaim(JwtClaimTypes.Permiso, permiso);

    private static IActionResult Creado(Resultado<Guid> resultado) =>
        resultado.Exitoso ? new OkObjectResult(new { id = resultado.Valor }) : ResultadoHttp.Error(resultado.Error!);
}

/// <summary>Operación diaria: Mi día, tablero, tratamientos y dosis, alimentación y reservas de despacho.</summary>
[ApiController]
[Route("api/v1/sanidad")]
public class SanidadOperacionController(SanidadService service) : ControllerBase
{
    private const long LimitePeticionReporte = SanidadService.TamanoMaximoReporteBytes + 512 * 1024;

    [HttpGet("mi-dia")]
    [RequierePermiso(Permisos.SanidadLeer)]
    public async Task<IActionResult> MiDia(CancellationToken ct) => Ok(await service.MiDiaAsync(ct));

    [HttpGet("tablero")]
    [RequierePermiso(Permisos.SanidadLeer)]
    public async Task<IActionResult> Tablero(CancellationToken ct) => Ok(await service.TableroAsync(ct));

    [HttpGet("acuarios")]
    [RequierePermiso(Permisos.SanidadLeer)]
    public async Task<IActionResult> Acuarios(CancellationToken ct) => Ok(await service.AcuariosAsync(ct));

    [HttpGet("tratamientos")]
    [RequierePermiso(Permisos.SanidadLeer)]
    public async Task<IActionResult> Tratamientos([FromQuery] bool vigentes, CancellationToken ct) =>
        Ok(await service.TratamientosAsync(vigentes, ct));

    [HttpGet("tratamientos/{id:guid}")]
    [RequierePermiso(Permisos.SanidadLeer)]
    public async Task<IActionResult> Tratamiento(Guid id, CancellationToken ct) => (await service.TratamientoAsync(id, ct)).ARespuesta();

    /// <summary>Desde un protocolo aprobado (operario) o manual con pasos (requiere <c>sanidad:gestionar</c>).</summary>
    [HttpPost("tratamientos")]
    [RequierePermiso(Permisos.SanidadOperar)]
    public async Task<IActionResult> CrearTratamiento(CrearTratamientoRequest r, CancellationToken ct) =>
        (await service.CrearTratamientoAsync(r, User.HasClaim(JwtClaimTypes.Permiso, Permisos.SanidadGestionar), ct)).ARespuesta();

    [HttpPut("tratamientos/{id:guid}/estado")]
    [RequierePermiso(Permisos.SanidadOperar)]
    public async Task<IActionResult> EstadoTratamiento(Guid id, CambiarEstadoRequest r, CancellationToken ct) =>
        (await service.CambiarEstadoTratamientoAsync(id, r, ct)).ARespuesta();

    [HttpGet("tratamientos/{id:guid}/reporte")]
    [RequierePermiso(Permisos.SanidadLeer)]
    public async Task<IActionResult> Reporte(Guid id, CancellationToken ct)
    {
        var resultado = await service.DescargarReporteAsync(id, ct);
        if (!resultado.Exitoso)
        {
            return ResultadoHttp.Error(resultado.Error!);
        }

        Response.Headers.CacheControl = "private, no-cache";
        return File(resultado.Valor.Contenido, resultado.Valor.TipoMime);
    }

    /// <summary>Multipart con el campo <c>archivo</c>: el PDF del veterinario, hasta 10 MB.</summary>
    [HttpPost("tratamientos/{id:guid}/reporte")]
    [RequierePermiso(Permisos.SanidadGestionar)]
    [RequestSizeLimit(LimitePeticionReporte)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimitePeticionReporte)]
    public async Task<IActionResult> SubirReporte(Guid id, IFormFile? archivo, CancellationToken ct)
    {
        if (archivo is null || archivo.Length == 0)
        {
            return BadRequest(new { error = "Selecciona el PDF del reporte." });
        }

        await using var contenido = archivo.OpenReadStream();
        return (await service.SubirReporteAsync(id, contenido, ct)).ARespuesta();
    }

    [HttpPut("aplicaciones/{id:guid}/aplicar")]
    [RequierePermiso(Permisos.SanidadOperar)]
    public async Task<IActionResult> Aplicar(Guid id, RegistrarAplicacionRequest r, CancellationToken ct) =>
        (await service.RegistrarAplicacionAsync(id, aplicada: true, r, ct)).ARespuesta();

    [HttpPut("aplicaciones/{id:guid}/omitir")]
    [RequierePermiso(Permisos.SanidadOperar)]
    public async Task<IActionResult> Omitir(Guid id, RegistrarAplicacionRequest r, CancellationToken ct) =>
        (await service.RegistrarAplicacionAsync(id, aplicada: false, r, ct)).ARespuesta();

    [HttpPost("alimentacion")]
    [RequierePermiso(Permisos.SanidadOperar)]
    public async Task<IActionResult> Alimentar(RegistrarAlimentacionRequest r, CancellationToken ct) =>
        (await service.RegistrarAlimentacionAsync(r, ct)).ARespuesta();

    [HttpGet("reservas/pedido/{pedidoId:guid}")]
    [RequierePermiso(Permisos.InventarioLeer)]
    public async Task<IActionResult> Reservas(Guid pedidoId, CancellationToken ct) => (await service.ReservasPedidoAsync(pedidoId, ct)).ARespuesta();

    /// <summary>Bodega decide de qué acuario sale cada línea (el comercial decide qué especies).</summary>
    [HttpPut("reservas")]
    [RequierePermiso(Permisos.PedidosDespachar)]
    public async Task<IActionResult> FijarReserva(FijarReservaRequest r, CancellationToken ct) =>
        (await service.FijarReservaAsync(r, ct)).ARespuesta();
}
