using JRTropicalFish.Api.Auth;
using JRTropicalFish.Api.Comun;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Crm;
using JRTropicalFish.Application.Inventario;
using JRTropicalFish.Domain.Seguridad;
using JRTropicalFish.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace JRTropicalFish.Api.Controllers;

/// <summary>Existencias, lotes, acuarios, operaciones de stock, saldo inicial, necesidades y dashboard.</summary>
[ApiController]
[Route("api/v1/inventario")]
public class InventarioController(InventarioService service, IUsuarioActual usuario) : CrmControllerBase(usuario)
{
    [HttpGet("dashboard")]
    [RequierePermiso(Permisos.InventarioLeer)]
    public async Task<IActionResult> Dashboard(CancellationToken ct) => Ok(await service.DashboardAsync(Alcance.FiltroComercial, ct));

    [HttpGet("existencias")]
    [RequierePermiso(Permisos.InventarioLeer)]
    public async Task<IActionResult> Existencias(
        [FromQuery] Guid? productoId, [FromQuery] Guid? loteId, [FromQuery] Guid? acuarioId, CancellationToken ct) =>
        Ok(await service.ExistenciasAsync(productoId, loteId, acuarioId, ct));

    [HttpGet("lotes")]
    [RequierePermiso(Permisos.InventarioLeer)]
    public async Task<IActionResult> Lotes([FromQuery] Guid? productoId, [FromQuery] bool soloConStock, CancellationToken ct) =>
        Ok(await service.LotesAsync(productoId, soloConStock, ct));

    /// <summary>Trazabilidad: el lote, dónde está hoy y todos sus movimientos.</summary>
    [HttpGet("lotes/{id:guid}")]
    [RequierePermiso(Permisos.InventarioLeer)]
    public async Task<IActionResult> Lote(Guid id, CancellationToken ct)
    {
        var resultado = await service.LoteAsync(id, ct);
        return resultado.Exitoso
            ? Ok(new { lote = resultado.Valor.Lote, existencias = resultado.Valor.Existencias, movimientos = resultado.Valor.Movimientos })
            : ResultadoHttp.Error(resultado.Error!);
    }

    [HttpGet("movimientos")]
    [RequierePermiso(Permisos.InventarioLeer)]
    public async Task<IActionResult> Movimientos([FromQuery] int limite, CancellationToken ct) =>
        Ok(await service.MovimientosAsync(limite == 0 ? 200 : limite, ct));

    /// <summary>Ubicar, trasladar, enfermo, aliviado, mortalidad (y ajuste con <c>inventario:ajustar</c>). Todo o nada.</summary>
    [HttpPost("operaciones")]
    [RequierePermiso(Permisos.InventarioOperar)]
    public async Task<IActionResult> Operar(OperacionesStockRequest request, CancellationToken ct) =>
        (await service.OperarAsync(request, User.HasClaim(JwtClaimTypes.Permiso, Permisos.InventarioAjustar), ct)).ARespuesta();

    /// <summary>Stock inicial de un lote migrado de la app anterior (conserva su código).</summary>
    [HttpPost("saldo-inicial")]
    [RequierePermiso(Permisos.InventarioAjustar)]
    public async Task<IActionResult> SaldoInicial(SaldoInicialRequest request, CancellationToken ct)
    {
        var resultado = await service.SaldoInicialAsync(request, ct);
        return resultado.Exitoso ? StatusCode(StatusCodes.Status201Created, new { id = resultado.Valor }) : ResultadoHttp.Error(resultado.Error!);
    }

    [HttpGet("necesidades")]
    [RequierePermiso(Permisos.InventarioLeer)]
    public async Task<IActionResult> Necesidades(CancellationToken ct) => Ok(await service.NecesidadesAsync(ct));

    [HttpGet("acuarios")]
    [RequierePermiso(Permisos.InventarioLeer)]
    public async Task<IActionResult> Acuarios(CancellationToken ct) => Ok(await service.AcuariosAsync(ct));

    [HttpPost("acuarios")]
    [RequierePermiso(Permisos.InventarioAjustar)]
    public async Task<IActionResult> CrearAcuario(GuardarAcuarioRequest request, CancellationToken ct)
    {
        var resultado = await service.CrearAcuarioAsync(request, ct);
        return resultado.Exitoso ? StatusCode(StatusCodes.Status201Created, new { id = resultado.Valor }) : ResultadoHttp.Error(resultado.Error!);
    }

    [HttpPut("acuarios/{id:guid}")]
    [RequierePermiso(Permisos.InventarioAjustar)]
    public async Task<IActionResult> ActualizarAcuario(Guid id, GuardarAcuarioRequest request, CancellationToken ct) =>
        (await service.ActualizarAcuarioAsync(id, request, ct)).ARespuesta();
}

/// <summary>Disponibilidad de proveedores, pedidos a proveedor y recepciones. El costo solo con <c>catalogos:ver-costos</c>.</summary>
[ApiController]
[Route("api/v1/compras")]
public class ComprasController(ComprasService service) : ControllerBase
{
    private bool VerCostos => User.HasClaim(JwtClaimTypes.Permiso, Permisos.CatalogosVerCostos);

    [HttpGet("ofertas")]
    [RequierePermiso(Permisos.InventarioLeer)]
    public async Task<IActionResult> Ofertas(CancellationToken ct) => Ok(await service.OfertasAsync(ct));

    [HttpGet("ofertas/{id:guid}")]
    [RequierePermiso(Permisos.InventarioLeer)]
    public async Task<IActionResult> Oferta(Guid id, CancellationToken ct) => (await service.OfertaAsync(id, ct)).ARespuesta();

    [HttpPost("ofertas")]
    [RequierePermiso(Permisos.ComprasGestionar)]
    public async Task<IActionResult> CrearOferta(GuardarOfertaRequest request, CancellationToken ct) =>
        Creado(await service.GuardarOfertaAsync(null, request, ct));

    [HttpPut("ofertas/{id:guid}")]
    [RequierePermiso(Permisos.ComprasGestionar)]
    public async Task<IActionResult> ActualizarOferta(Guid id, GuardarOfertaRequest request, CancellationToken ct) =>
        (await service.GuardarOfertaAsync(id, request, ct)).ARespuesta();

    [HttpGet("pedidos")]
    [RequierePermiso(Permisos.InventarioLeer)]
    public async Task<IActionResult> Pedidos(CancellationToken ct) => Ok(await service.PedidosAsync(VerCostos, ct));

    [HttpGet("pedidos/{id:guid}")]
    [RequierePermiso(Permisos.InventarioLeer)]
    public async Task<IActionResult> Pedido(Guid id, CancellationToken ct) => (await service.PedidoAsync(id, VerCostos, ct)).ARespuesta();

    [HttpPost("pedidos")]
    [RequierePermiso(Permisos.ComprasGestionar)]
    public async Task<IActionResult> CrearPedido(GuardarPedidoProveedorRequest request, CancellationToken ct) =>
        Creado(await service.GuardarPedidoAsync(null, request, VerCostos, ct));

    [HttpPut("pedidos/{id:guid}")]
    [RequierePermiso(Permisos.ComprasGestionar)]
    public async Task<IActionResult> ActualizarPedido(Guid id, GuardarPedidoProveedorRequest request, CancellationToken ct) =>
        (await service.GuardarPedidoAsync(id, request, VerCostos, ct)).ARespuesta();

    [HttpPut("pedidos/{id:guid}/estado")]
    [RequierePermiso(Permisos.ComprasGestionar)]
    public async Task<IActionResult> CambiarEstado(Guid id, CambiarEstadoRequest request, CancellationToken ct) =>
        (await service.CambiarEstadoPedidoAsync(id, request, ct)).ARespuesta();

    [HttpGet("recepciones")]
    [RequierePermiso(Permisos.InventarioLeer)]
    public async Task<IActionResult> Recepciones(CancellationToken ct) => Ok(await service.RecepcionesAsync(VerCostos, ct));

    [HttpGet("recepciones/{id:guid}")]
    [RequierePermiso(Permisos.InventarioLeer)]
    public async Task<IActionResult> Recepcion(Guid id, CancellationToken ct) => (await service.RecepcionAsync(id, VerCostos, ct)).ARespuesta();

    /// <summary>Registra la llegada: crea un lote por línea con el stock vivo por ubicar.</summary>
    [HttpPost("recepciones")]
    [RequierePermiso(Permisos.ComprasRecibir)]
    public async Task<IActionResult> Recibir(RegistrarRecepcionRequest request, CancellationToken ct)
    {
        var resultado = await service.RecibirAsync(request, VerCostos, ct);
        return resultado.Exitoso ? StatusCode(StatusCodes.Status201Created, resultado.Valor) : ResultadoHttp.Error(resultado.Error!);
    }

    private IActionResult Creado(Resultado<Creado> r) =>
        r.Exitoso ? StatusCode(StatusCodes.Status201Created, r.Valor) : ResultadoHttp.Error(r.Error!);
}

/// <summary>Pedidos de cliente (enlazados a la venta del CRM) y su despacho.</summary>
[ApiController]
[Route("api/v1/pedidos")]
public class PedidosClienteController(PedidoClienteService service, IUsuarioActual usuario) : CrmControllerBase(usuario)
{
    [HttpGet]
    [RequierePermiso(Permisos.InventarioLeer)]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await service.ListarAsync(Alcance, ct));

    [HttpGet("{id:guid}")]
    [RequierePermiso(Permisos.InventarioLeer)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct) => (await service.ObtenerAsync(id, Alcance, ct)).ARespuesta();

    [HttpGet("por-venta/{ventaId:guid}")]
    [RequierePermiso(Permisos.InventarioLeer)]
    public async Task<IActionResult> PorVenta(Guid ventaId, CancellationToken ct) =>
        await service.PorVentaAsync(ventaId, Alcance, ct) is { } p ? Ok(p) : NoContent();

    [HttpGet("{id:guid}/historial")]
    [RequierePermiso(Permisos.InventarioLeer)]
    public async Task<IActionResult> Historial(Guid id, CancellationToken ct) => (await service.HistorialAsync(id, Alcance, ct)).ARespuesta();

    [HttpPost]
    [RequierePermiso(Permisos.PedidosGestionar)]
    public async Task<IActionResult> Crear(GuardarPedidoClienteRequest request, CancellationToken ct) =>
        RespuestaCreado(await service.CrearAsync(request, Alcance, ct));

    [HttpPut("{id:guid}")]
    [RequierePermiso(Permisos.PedidosGestionar)]
    public async Task<IActionResult> Actualizar(Guid id, GuardarPedidoClienteRequest request, CancellationToken ct) =>
        (await service.ActualizarAsync(id, request, Alcance, ct)).ARespuesta();

    /// <summary>Confirmar, volver a borrador, cancelar, pasar a preparación o cerrar con faltantes.</summary>
    [HttpPut("{id:guid}/estado")]
    [RequierePermiso(Permisos.PedidosGestionar)]
    public async Task<IActionResult> CambiarEstado(Guid id, CambiarEstadoRequest request, CancellationToken ct) =>
        (await service.CambiarEstadoAsync(id, request, Alcance, ct)).ARespuesta();

    [HttpPost("{id:guid}/despachar")]
    [RequierePermiso(Permisos.PedidosDespachar)]
    public async Task<IActionResult> Despachar(Guid id, DespacharRequest request, CancellationToken ct) =>
        (await service.DespacharAsync(id, request, Alcance, ct)).ARespuesta();
}
