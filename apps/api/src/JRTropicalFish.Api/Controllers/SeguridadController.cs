using JRTropicalFish.Api.Auth;
using JRTropicalFish.Api.Comun;
using JRTropicalFish.Application.Seguridad;
using JRTropicalFish.Domain.Seguridad;
using Microsoft.AspNetCore.Mvc;

namespace JRTropicalFish.Api.Controllers;

[ApiController]
[Route("api/v1/seguridad/usuarios")]
public class UsuariosController(UsuarioService service) : ControllerBase
{
    [HttpGet]
    [RequierePermiso(Permisos.UsuariosLeer)]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda, CancellationToken ct) =>
        Ok(await service.ListarAsync(busqueda, ct));

    [HttpGet("{id:guid}")]
    [RequierePermiso(Permisos.UsuariosLeer)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct) =>
        (await service.ObtenerAsync(id, ct)).ARespuesta();

    [HttpGet("{id:guid}/historial")]
    [RequierePermiso(Permisos.UsuariosLeer)]
    public async Task<IActionResult> Historial(Guid id, CancellationToken ct) =>
        Ok(await service.HistorialAsync(id, ct));

    [HttpPost]
    [RequierePermiso(Permisos.UsuariosCrear)]
    public async Task<IActionResult> Crear(CrearUsuarioRequest request, CancellationToken ct)
    {
        var resultado = await service.CrearAsync(request, ct);
        return resultado.Exitoso
            ? CreatedAtAction(nameof(Obtener), new { id = resultado.Valor }, new { id = resultado.Valor })
            : ResultadoHttp.Error(resultado.Error!);
    }

    /// <summary>Datos, perfil y (opcional) nueva contraseña. Cambiar el perfil cierra sus sesiones.</summary>
    [HttpPut("{id:guid}")]
    [RequierePermiso(Permisos.UsuariosEditar)]
    public async Task<IActionResult> Actualizar(Guid id, ActualizarUsuarioRequest request, CancellationToken ct) =>
        (await service.ActualizarAsync(id, request, ct)).ARespuesta();

    /// <summary>Activar o desactivar. Desactivar cierra sus sesiones de inmediato.</summary>
    [HttpPut("{id:guid}/estado")]
    [RequierePermiso(Permisos.UsuariosEliminar)]
    public async Task<IActionResult> CambiarEstado(Guid id, CambiarEstadoUsuarioRequest request, CancellationToken ct) =>
        (await service.CambiarEstadoAsync(id, request.Activo, ct)).ARespuesta();

    [HttpPost("{id:guid}/desbloquear")]
    [RequierePermiso(Permisos.UsuariosEditar)]
    public async Task<IActionResult> Desbloquear(Guid id, CancellationToken ct) =>
        (await service.DesbloquearAsync(id, ct)).ARespuesta();
}

[ApiController]
[Route("api/v1/seguridad/perfiles")]
public class PerfilesController(PerfilService service) : ControllerBase
{
    [HttpGet("permisos")]
    [RequierePermiso(Permisos.PerfilesLeer)]
    public async Task<IActionResult> ListarPermisos(CancellationToken ct) =>
        Ok(await service.ListarPermisosAsync(ct));

    [HttpGet]
    [RequierePermiso(Permisos.PerfilesLeer)]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda, CancellationToken ct) =>
        Ok(await service.ListarAsync(busqueda, ct));

    [HttpGet("{id:guid}")]
    [RequierePermiso(Permisos.PerfilesLeer)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct) =>
        (await service.ObtenerAsync(id, ct)).ARespuesta();

    [HttpGet("{id:guid}/historial")]
    [RequierePermiso(Permisos.PerfilesLeer)]
    public async Task<IActionResult> Historial(Guid id, CancellationToken ct) =>
        Ok(await service.HistorialAsync(id, ct));

    [HttpPost]
    [RequierePermiso(Permisos.PerfilesCrear)]
    public async Task<IActionResult> Crear(GuardarPerfilRequest request, CancellationToken ct)
    {
        var resultado = await service.CrearAsync(request, ct);
        return resultado.Exitoso
            ? CreatedAtAction(nameof(Obtener), new { id = resultado.Valor }, new { id = resultado.Valor })
            : ResultadoHttp.Error(resultado.Error!);
    }

    /// <summary>Si cambian permisos o estado, los usuarios del perfil deben volver a iniciar sesión.</summary>
    [HttpPut("{id:guid}")]
    [RequierePermiso(Permisos.PerfilesEditar)]
    public async Task<IActionResult> Actualizar(Guid id, GuardarPerfilRequest request, CancellationToken ct) =>
        (await service.ActualizarAsync(id, request, ct)).ARespuesta();

    [HttpDelete("{id:guid}")]
    [RequierePermiso(Permisos.PerfilesEliminar)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        var resultado = await service.EliminarAsync(id, ct);
        return resultado.Exitoso ? NoContent() : ResultadoHttp.Error(resultado.Error!);
    }
}
