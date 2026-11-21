using JRTropicalFish.Api.Auth;
using JRTropicalFish.Api.Comun;
using JRTropicalFish.Application.Administracion;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Maestros;
using JRTropicalFish.Domain.Seguridad;
using JRTropicalFish.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace JRTropicalFish.Api.Controllers;

/// <summary>
/// Maestros genéricos. <c>{clave}</c> se busca en <c>RegistroMaestros</c>; una clave desconocida da 404.
/// </summary>
[ApiController]
[Route("api/v1/maestros")]
public class MaestrosController(MaestroService service) : ControllerBase
{
    /// <summary>Los maestros disponibles (para el selector de la pantalla).</summary>
    [HttpGet]
    [RequierePermiso(Permisos.MaestrosLeer)]
    public IActionResult Disponibles() => Ok(MaestroService.Disponibles());

    /// <summary>
    /// Valores de un maestro. Con <c>soloActivos=true</c> lo puede pedir cualquier usuario con sesión
    /// (son las opciones de los formularios de otros módulos, ej. empleados); la lista completa
    /// exige <c>maestros:leer</c>.
    /// </summary>
    [HttpGet("{clave}")]
    public async Task<IActionResult> Listar(string clave, [FromQuery] bool soloActivos, CancellationToken ct)
    {
        if (!soloActivos && !User.HasClaim(JwtClaimTypes.Permiso, Permisos.MaestrosLeer))
        {
            return ResultadoHttp.Error(Error.Prohibido("No tienes permiso para consultar los maestros."));
        }

        return (await service.ListarAsync(clave, soloActivos, ct)).ARespuesta();
    }

    [HttpGet("{clave}/{id:guid}/historial")]
    [RequierePermiso(Permisos.MaestrosLeer)]
    public async Task<IActionResult> Historial(string clave, Guid id, CancellationToken ct) =>
        (await service.HistorialAsync(clave, id, ct)).ARespuesta();

    [HttpPost("{clave}")]
    [RequierePermiso(Permisos.MaestrosEditar)]
    public async Task<IActionResult> Crear(string clave, GuardarValorMaestroRequest request, CancellationToken ct)
    {
        var resultado = await service.CrearAsync(clave, request, ct);
        return resultado.Exitoso
            ? StatusCode(StatusCodes.Status201Created, new { id = resultado.Valor })
            : ResultadoHttp.Error(resultado.Error!);
    }

    [HttpPut("{clave}/{id:guid}")]
    [RequierePermiso(Permisos.MaestrosEditar)]
    public async Task<IActionResult> Actualizar(string clave, Guid id, GuardarValorMaestroRequest request, CancellationToken ct) =>
        (await service.ActualizarAsync(clave, id, request, ct)).ARespuesta();

    [HttpPut("{clave}/{id:guid}/estado")]
    [RequierePermiso(Permisos.MaestrosEditar)]
    public async Task<IActionResult> CambiarEstado(
        string clave, Guid id, CambiarEstadoValorMaestroRequest request, CancellationToken ct) =>
        (await service.CambiarEstadoAsync(clave, id, request.Activo, ct)).ARespuesta();

    [HttpDelete("{clave}/{id:guid}")]
    [RequierePermiso(Permisos.MaestrosEditar)]
    public async Task<IActionResult> Eliminar(string clave, Guid id, CancellationToken ct) =>
        (await service.EliminarAsync(clave, id, ct)).ARespuesta();
}

/// <summary>
/// Empleados. El salario solo viaja (en lectura, escritura e historial) si el token tiene
/// <c>empleados:ver-salario</c>.
/// </summary>
[ApiController]
[Route("api/v1/administracion/empleados")]
public class EmpleadosController(EmpleadoService service) : ControllerBase
{
    /// <summary>Margen sobre los 10 MB de la foto para las cabeceras del multipart.</summary>
    private const long LimitePeticionFoto = EmpleadoService.TamanoMaximoFotoBytes + 1024 * 1024;

    private bool VerSalario => User.HasClaim(JwtClaimTypes.Permiso, Permisos.EmpleadosVerSalario);

    [HttpGet]
    [RequierePermiso(Permisos.EmpleadosLeer)]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda, CancellationToken ct) =>
        Ok(await service.ListarAsync(busqueda, VerSalario, ct));

    [HttpGet("{id:guid}")]
    [RequierePermiso(Permisos.EmpleadosLeer)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct) =>
        (await service.ObtenerAsync(id, VerSalario, ct)).ARespuesta();

    [HttpGet("{id:guid}/historial")]
    [RequierePermiso(Permisos.EmpleadosLeer)]
    public async Task<IActionResult> Historial(Guid id, CancellationToken ct) =>
        Ok(await service.HistorialAsync(id, VerSalario, ct));

    [HttpPost]
    [RequierePermiso(Permisos.EmpleadosCrear)]
    public async Task<IActionResult> Crear(GuardarEmpleadoRequest request, CancellationToken ct)
    {
        var resultado = await service.CrearAsync(request, VerSalario, ct);
        return resultado.Exitoso
            ? CreatedAtAction(nameof(Obtener), new { id = resultado.Valor }, new { id = resultado.Valor })
            : ResultadoHttp.Error(resultado.Error!);
    }

    [HttpPut("{id:guid}")]
    [RequierePermiso(Permisos.EmpleadosEditar)]
    public async Task<IActionResult> Actualizar(Guid id, GuardarEmpleadoRequest request, CancellationToken ct) =>
        (await service.ActualizarAsync(id, request, VerSalario, ct)).ARespuesta();

    [HttpPut("{id:guid}/estado")]
    [RequierePermiso(Permisos.EmpleadosEliminar)]
    public async Task<IActionResult> CambiarEstado(Guid id, CambiarEstadoEmpleadoRequest request, CancellationToken ct) =>
        (await service.CambiarEstadoAsync(id, request.Activo, ct)).ARespuesta();

    [HttpDelete("{id:guid}")]
    [RequierePermiso(Permisos.EmpleadosEliminar)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct) =>
        (await service.EliminarAsync(id, ct)).ARespuesta();

    // --- Foto ---

    [HttpGet("{id:guid}/foto")]
    [RequierePermiso(Permisos.EmpleadosLeer)]
    public async Task<IActionResult> Foto(Guid id, CancellationToken ct)
    {
        var resultado = await service.DescargarFotoAsync(id, ct);
        if (!resultado.Exitoso)
        {
            return ResultadoHttp.Error(resultado.Error!);
        }

        Response.Headers.CacheControl = "private, no-cache";
        return File(resultado.Valor.Contenido, resultado.Valor.TipoMime);
    }

    /// <summary>Multipart con el campo <c>archivo</c>. JPG, PNG o WEBP de hasta 10 MB (se valida por contenido).</summary>
    [HttpPost("{id:guid}/foto")]
    [RequierePermiso(Permisos.EmpleadosEditar)]
    [RequestSizeLimit(LimitePeticionFoto)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimitePeticionFoto)]
    public async Task<IActionResult> SubirFoto(Guid id, IFormFile? archivo, CancellationToken ct)
    {
        if (archivo is null || archivo.Length == 0)
        {
            return BadRequest(new { error = "Selecciona una imagen." });
        }

        if (archivo.Length > EmpleadoService.TamanoMaximoFotoBytes)
        {
            return BadRequest(new { error = "La foto no puede pesar más de 10 MB." });
        }

        await using var contenido = archivo.OpenReadStream();
        var resultado = await service.CambiarFotoAsync(id, contenido, ct);
        return resultado.Exitoso ? Ok(new { fotoVersion = resultado.Valor }) : ResultadoHttp.Error(resultado.Error!);
    }

    [HttpDelete("{id:guid}/foto")]
    [RequierePermiso(Permisos.EmpleadosEditar)]
    public async Task<IActionResult> EliminarFoto(Guid id, CancellationToken ct) =>
        (await service.EliminarFotoAsync(id, ct)).ARespuesta();
}
