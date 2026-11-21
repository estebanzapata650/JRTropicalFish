using JRTropicalFish.Api.Auth;
using JRTropicalFish.Api.Comun;
using JRTropicalFish.Application.Catalogos;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Crm;
using JRTropicalFish.Domain.Seguridad;
using JRTropicalFish.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace JRTropicalFish.Api.Controllers;

/// <summary>Catálogo de productos. Las listas de costo solo viajan con <c>catalogos:ver-costos</c>.</summary>
[ApiController]
[Route("api/v1/catalogos/productos")]
public class ProductosController(ProductoService service) : ControllerBase
{
    private bool VerCostos => User.HasClaim(JwtClaimTypes.Permiso, Permisos.CatalogosVerCostos);

    /// <summary>Precios incluidos solo si además se tiene <c>precios:leer</c>.</summary>
    private bool VerPrecios => User.HasClaim(JwtClaimTypes.Permiso, Permisos.PreciosLeer);

    [HttpGet]
    [RequierePermiso(Permisos.CatalogosLeer)]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok((await service.ListarAsync(VerCostos, ct)).Select(SinPreciosSiNoPuede));

    [HttpGet("{id:guid}")]
    [RequierePermiso(Permisos.CatalogosLeer)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct)
    {
        var resultado = await service.ObtenerAsync(id, VerCostos, ct);
        return resultado.Exitoso ? Ok(SinPreciosSiNoPuede(resultado.Valor)) : ResultadoHttp.Error(resultado.Error!);
    }

    [HttpGet("{id:guid}/historial")]
    [RequierePermiso(Permisos.CatalogosLeer)]
    public async Task<IActionResult> Historial(Guid id, CancellationToken ct) =>
        (await service.HistorialAsync(id, VerCostos, ct)).ARespuesta();

    [HttpPost]
    [RequierePermiso(Permisos.CatalogosEditar)]
    public async Task<IActionResult> Crear(GuardarProductoRequest request, CancellationToken ct)
    {
        if (PideEditarPrecios(request))
        {
            return SinPermisoPrecios();
        }

        var resultado = await service.CrearAsync(request, VerCostos, ct);
        return resultado.Exitoso ? StatusCode(StatusCodes.Status201Created, resultado.Valor) : ResultadoHttp.Error(resultado.Error!);
    }

    [HttpPut("{id:guid}")]
    [RequierePermiso(Permisos.CatalogosEditar)]
    public async Task<IActionResult> Actualizar(Guid id, GuardarProductoRequest request, CancellationToken ct) =>
        PideEditarPrecios(request) ? SinPermisoPrecios() : (await service.ActualizarAsync(id, request, VerCostos, ct)).ARespuesta();

    [HttpDelete("{id:guid}")]
    [RequierePermiso(Permisos.CatalogosEditar)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct) => (await service.EliminarAsync(id, ct)).ARespuesta();

    private ProductoDetalle SinPreciosSiNoPuede(ProductoDetalle p) =>
        VerPrecios ? p : p with { Precios = new Dictionary<Guid, decimal>() };

    private bool PideEditarPrecios(GuardarProductoRequest r) =>
        r.Precios is { Count: > 0 } && !User.HasClaim(JwtClaimTypes.Permiso, Permisos.PreciosEditar);

    private static IActionResult SinPermisoPrecios() =>
        ResultadoHttp.Error(Error.Prohibido("No tienes permiso para editar precios."));
}

[ApiController]
[Route("api/v1/catalogos/precios")]
public class PreciosController(PrecioService service) : ControllerBase
{
    private bool VerCostos => User.HasClaim(JwtClaimTypes.Permiso, Permisos.CatalogosVerCostos);

    [HttpGet("listas")]
    [RequierePermiso(Permisos.PreciosLeer)]
    public async Task<IActionResult> Listas(CancellationToken ct) => Ok(await service.ListasAsync(VerCostos, ct));

    [HttpGet("listas/{id:guid}/historial")]
    [RequierePermiso(Permisos.PreciosLeer)]
    public async Task<IActionResult> HistorialLista(Guid id, CancellationToken ct) =>
        (await service.HistorialListaAsync(id, VerCostos, ct)).ARespuesta();

    [HttpPost("listas")]
    [RequierePermiso(Permisos.PreciosEditar)]
    public async Task<IActionResult> CrearLista(GuardarListaPreciosRequest request, CancellationToken ct)
    {
        var resultado = await service.CrearListaAsync(request, VerCostos, ct);
        return resultado.Exitoso ? StatusCode(StatusCodes.Status201Created, new { id = resultado.Valor }) : ResultadoHttp.Error(resultado.Error!);
    }

    [HttpPut("listas/{id:guid}")]
    [RequierePermiso(Permisos.PreciosEditar)]
    public async Task<IActionResult> ActualizarLista(Guid id, GuardarListaPreciosRequest request, CancellationToken ct) =>
        (await service.ActualizarListaAsync(id, request, VerCostos, ct)).ARespuesta();

    /// <summary>Recalcula la lista desde su lista base con la regla (sobrescribe los precios calculables).</summary>
    [HttpPost("listas/{id:guid}/recalcular")]
    [RequierePermiso(Permisos.PreciosEditar)]
    public async Task<IActionResult> Recalcular(Guid id, CancellationToken ct) =>
        (await service.RecalcularAsync(id, VerCostos, ct)).ARespuesta();

    [HttpGet("parametros")]
    [RequierePermiso(Permisos.PreciosLeer)]
    public async Task<IActionResult> Parametros(CancellationToken ct) => Ok(await service.ParametrosAsync(ct));

    [HttpPut("parametros")]
    [RequierePermiso(Permisos.PreciosEditar)]
    public async Task<IActionResult> ActualizarParametros(GuardarParametrosPreciosRequest request, CancellationToken ct) =>
        (await service.ActualizarParametrosAsync(request, ct)).ARespuesta();
}

[ApiController]
[Route("api/v1/catalogos/centros-acopio")]
public class CentrosAcopioController(CentroAcopioService service) : ControllerBase
{
    /// <summary>Lo pueden leer catálogos y proveedores (son opciones de ambos formularios).</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        User.HasClaim(JwtClaimTypes.Permiso, Permisos.CatalogosLeer) || User.HasClaim(JwtClaimTypes.Permiso, Permisos.ProveedoresLeer)
            ? Ok(await service.ListarAsync(ct))
            : ResultadoHttp.Error(Error.Prohibido("No tienes permiso para consultar los centros de acopio."));

    [HttpGet("{id:guid}/historial")]
    [RequierePermiso(Permisos.CatalogosLeer)]
    public async Task<IActionResult> Historial(Guid id, CancellationToken ct) => (await service.HistorialAsync(id, ct)).ARespuesta();

    [HttpPost]
    [RequierePermiso(Permisos.CatalogosEditar)]
    public async Task<IActionResult> Crear(GuardarCentroAcopioRequest request, CancellationToken ct)
    {
        var resultado = await service.CrearAsync(request, ct);
        return resultado.Exitoso ? StatusCode(StatusCodes.Status201Created, new { id = resultado.Valor }) : ResultadoHttp.Error(resultado.Error!);
    }

    [HttpPut("{id:guid}")]
    [RequierePermiso(Permisos.CatalogosEditar)]
    public async Task<IActionResult> Actualizar(Guid id, GuardarCentroAcopioRequest request, CancellationToken ct) =>
        (await service.ActualizarAsync(id, request, ct)).ARespuesta();

    [HttpDelete("{id:guid}")]
    [RequierePermiso(Permisos.CatalogosEditar)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct) => (await service.EliminarAsync(id, ct)).ARespuesta();
}

[ApiController]
[Route("api/v1/catalogos/proveedores")]
public class ProveedoresController(ProveedorService service) : ControllerBase
{
    [HttpGet]
    [RequierePermiso(Permisos.ProveedoresLeer)]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await service.ListarAsync(ct));

    [HttpGet("{id:guid}")]
    [RequierePermiso(Permisos.ProveedoresLeer)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct) => (await service.ObtenerAsync(id, ct)).ARespuesta();

    [HttpGet("{id:guid}/historial")]
    [RequierePermiso(Permisos.ProveedoresLeer)]
    public async Task<IActionResult> Historial(Guid id, CancellationToken ct) => (await service.HistorialAsync(id, ct)).ARespuesta();

    [HttpPost]
    [RequierePermiso(Permisos.ProveedoresEditar)]
    public async Task<IActionResult> Crear(GuardarProveedorRequest request, CancellationToken ct)
    {
        var resultado = await service.CrearAsync(request, ct);
        return resultado.Exitoso ? StatusCode(StatusCodes.Status201Created, new { id = resultado.Valor }) : ResultadoHttp.Error(resultado.Error!);
    }

    [HttpPut("{id:guid}")]
    [RequierePermiso(Permisos.ProveedoresEditar)]
    public async Task<IActionResult> Actualizar(Guid id, GuardarProveedorRequest request, CancellationToken ct) =>
        (await service.ActualizarAsync(id, request, ct)).ARespuesta();

    [HttpDelete("{id:guid}")]
    [RequierePermiso(Permisos.ProveedoresEditar)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct) => (await service.EliminarAsync(id, ct)).ARespuesta();
}
