using JRTropicalFish.Api.Auth;
using JRTropicalFish.Api.Comun;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Exportaciones;
using JRTropicalFish.Domain.Exportaciones;
using JRTropicalFish.Domain.Seguridad;
using JRTropicalFish.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace JRTropicalFish.Api.Controllers;

/// <summary>Configuración de los documentos de exportación: empresa y logo, tarifas, cajas, bolsas, cargos y datos del cliente.</summary>
[ApiController]
[Route("api/v1/exportaciones")]
public class ConfiguracionExportacionController(ConfiguracionExportacionService service) : ControllerBase
{
    private const long LimitePeticionLogo = ConfiguracionExportacionService.TamanoMaximoLogoBytes + 512 * 1024;

    [HttpGet("configuracion")]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> Obtener(CancellationToken ct) => Ok(await service.ObtenerAsync(ct));

    [HttpPut("configuracion/empresa")]
    [RequierePermiso(Permisos.ExportacionesConfigurar)]
    public async Task<IActionResult> Empresa(GuardarDatosEmpresaRequest request, CancellationToken ct) =>
        (await service.ActualizarEmpresaAsync(request, ct)).ARespuesta();

    [HttpGet("configuracion/logo")]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> Logo(CancellationToken ct)
    {
        var resultado = await service.DescargarLogoAsync(ct);
        if (!resultado.Exitoso)
        {
            return ResultadoHttp.Error(resultado.Error!);
        }

        Response.Headers.CacheControl = "private, no-cache";
        return File(resultado.Valor.Contenido, resultado.Valor.TipoMime);
    }

    /// <summary>Multipart con el campo <c>archivo</c>: PNG o JPG de hasta 2 MB (se valida por contenido).</summary>
    [HttpPost("configuracion/logo")]
    [RequierePermiso(Permisos.ExportacionesConfigurar)]
    [RequestSizeLimit(LimitePeticionLogo)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimitePeticionLogo)]
    public async Task<IActionResult> SubirLogo(IFormFile? archivo, CancellationToken ct)
    {
        if (archivo is null || archivo.Length == 0)
        {
            return BadRequest(new { error = "Selecciona una imagen." });
        }

        await using var contenido = archivo.OpenReadStream();
        return (await service.CambiarLogoAsync(contenido, ct)).ARespuesta();
    }

    [HttpPut("configuracion/tarifas")]
    [RequierePermiso(Permisos.ExportacionesConfigurar)]
    public async Task<IActionResult> Tarifas(TarifasFleteInfo request, CancellationToken ct) =>
        (await service.ActualizarTarifasAsync(request, ct)).ARespuesta();

    [HttpPost("configuracion/cajas")]
    [RequierePermiso(Permisos.ExportacionesConfigurar)]
    public async Task<IActionResult> Caja(GuardarTipoCaja request, CancellationToken ct) =>
        (await service.GuardarTipoCajaAsync(request, ct)).ARespuesta();

    [HttpPost("configuracion/bolsas")]
    [RequierePermiso(Permisos.ExportacionesConfigurar)]
    public async Task<IActionResult> Bolsa(GuardarTipoBolsa request, CancellationToken ct) =>
        (await service.GuardarTipoBolsaAsync(request, ct)).ARespuesta();

    [HttpPut("configuracion/cargos")]
    [RequierePermiso(Permisos.ExportacionesConfigurar)]
    public async Task<IActionResult> Cargo(GuardarCargo request, CancellationToken ct) =>
        (await service.ActualizarCargoAsync(request, ct)).ARespuesta();

    [HttpGet("clientes/{clienteId:guid}")]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> Cliente(Guid clienteId, CancellationToken ct) => (await service.ClienteAsync(clienteId, ct)).ARespuesta();

    [HttpPut("clientes/{clienteId:guid}")]
    [RequierePermiso(Permisos.ExportacionesGestionar)]
    public async Task<IActionResult> GuardarCliente(Guid clienteId, GuardarClienteExportacionRequest request, CancellationToken ct) =>
        (await service.GuardarClienteAsync(clienteId, request, ct)).ARespuesta();
}

/// <summary>
/// Embarques de exportación: de la preinvoice al balance, con sus documentos en PDF. Un comercial sin
/// <c>crm:ver-todo</c> solo ve los suyos.
/// </summary>
[ApiController]
[Route("api/v1/exportaciones/embarques")]
public class EmbarquesController(EmbarqueService service, IUsuarioActual usuario) : CrmControllerBase(usuario)
{
    [HttpGet]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await service.ListarAsync(Alcance, ct));

    [HttpGet("{id:guid}")]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct) => (await service.ObtenerAsync(id, Alcance, ct)).ARespuesta();

    /// <summary>El embarque de una venta (204 si aún no tiene).</summary>
    [HttpGet("por-venta/{ventaId:guid}")]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> PorVenta(Guid ventaId, CancellationToken ct) =>
        await service.PorVentaAsync(ventaId, Alcance, ct) is { } id ? Ok(new { id }) : NoContent();

    [HttpGet("{id:guid}/historial")]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> Historial(Guid id, CancellationToken ct) => (await service.HistorialAsync(id, Alcance, ct)).ARespuesta();

    [HttpPost]
    [RequierePermiso(Permisos.ExportacionesGestionar)]
    public async Task<IActionResult> Crear(CrearEmbarqueRequest request, CancellationToken ct) =>
        RespuestaCreado(await service.CrearAsync(request, Alcance, ct));

    [HttpPut("{id:guid}")]
    [RequierePermiso(Permisos.ExportacionesGestionar)]
    public async Task<IActionResult> Actualizar(Guid id, GuardarEmbarqueRequest request, CancellationToken ct) =>
        (await service.ActualizarAsync(id, request, Alcance, ct)).ARespuesta();

    [HttpPut("{id:guid}/lineas")]
    [RequierePermiso(Permisos.ExportacionesGestionar)]
    public async Task<IActionResult> Lineas(Guid id, GuardarLineasRequest request, CancellationToken ct)
    {
        var resultado = await service.GuardarLineasAsync(id, request, Alcance, ct);
        return resultado.Exitoso ? Ok(new { avisos = resultado.Valor }) : ResultadoHttp.Error(resultado.Error!);
    }

    [HttpPut("{id:guid}/empaque")]
    [RequierePermiso(Permisos.ExportacionesEmpacar)]
    public async Task<IActionResult> Empaque(Guid id, GuardarEmpaqueRequest request, CancellationToken ct) =>
        (await service.GuardarEmpaqueAsync(id, request, Alcance, ct)).ARespuesta();

    [HttpPut("{id:guid}/doa")]
    [RequierePermiso(Permisos.ExportacionesGestionar)]
    public async Task<IActionResult> Doa(Guid id, GuardarDoaRequest request, CancellationToken ct) =>
        (await service.GuardarDoaAsync(id, request, Alcance, ct)).ARespuesta();

    /// <summary>
    /// Cambio de estado manual. Bodega (<c>exportaciones:empacar</c>) solo puede marcar «empacado» o
    /// devolverlo a «confirmado»; el resto exige <c>exportaciones:gestionar</c>.
    /// </summary>
    [HttpPut("{id:guid}/estado")]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> Estado(Guid id, CambiarEstadoEmbarqueRequest request, CancellationToken ct)
    {
        var gestiona = User.HasClaim(JwtClaimTypes.Permiso, Permisos.ExportacionesGestionar);
        var empacaSolo = User.HasClaim(JwtClaimTypes.Permiso, Permisos.ExportacionesEmpacar)
                         && (request.Estado == EstadosEmbarque.Empacado
                             || (request.EstadoActual == EstadosEmbarque.Empacado && request.Estado == EstadosEmbarque.Confirmado));
        if (!gestiona && !empacaSolo)
        {
            return Forbid();
        }

        return (await service.CambiarEstadoAsync(id, request, Alcance, ct)).ARespuesta();
    }

    [HttpPost("{id:guid}/facturar")]
    [RequierePermiso(Permisos.ExportacionesGestionar)]
    public async Task<IActionResult> Facturar(Guid id, CancellationToken ct)
    {
        var resultado = await service.FacturarAsync(id, Alcance, ct);
        return resultado.Exitoso ? Ok(new { numero = resultado.Valor }) : ResultadoHttp.Error(resultado.Error!);
    }

    /// <summary>PDF del documento (preinvoice, flete, orden_cargue, empaque, factura, nota_credito, balance).</summary>
    [HttpGet("{id:guid}/documentos/{tipo}")]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> Documento(Guid id, string tipo, CancellationToken ct)
    {
        var resultado = await service.DocumentoAsync(id, tipo, Alcance, ct);
        if (!resultado.Exitoso)
        {
            return ResultadoHttp.Error(resultado.Error!);
        }

        Response.Headers.CacheControl = "private, no-store";
        return File(resultado.Valor.Contenido, "application/pdf", resultado.Valor.NombreArchivo);
    }

    [HttpPost("{id:guid}/documentos/{tipo}/enviar")]
    [RequierePermiso(Permisos.ExportacionesGestionar)]
    public async Task<IActionResult> Enviar(Guid id, string tipo, EnviarDocumentoRequest request, CancellationToken ct) =>
        (await service.EnviarAsync(id, tipo, request, Alcance, ct)).ARespuesta();
}

/// <summary>
/// AUNAP: listado de especies autorizadas, enlace de productos y permiso por embarque. Los documentos para
/// autoridades se piden por <c>GET /embarques/{id}/documentos/{solicitud_aunap|packing_aduana|factura_aduana}</c>.
/// </summary>
[ApiController]
[Route("api/v1/exportaciones")]
public class AunapController(AunapService service, IUsuarioActual usuario) : CrmControllerBase(usuario)
{
    [HttpGet("aunap/especies")]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> Especies(CancellationToken ct) => Ok(await service.EspeciesAsync(ct));

    [HttpPost("aunap/especies/importar")]
    [RequierePermiso(Permisos.ExportacionesConfigurar)]
    public async Task<IActionResult> Importar(ImportarAunapRequest request, CancellationToken ct) => (await service.ImportarAsync(request, ct)).ARespuesta();

    [HttpGet("aunap/productos")]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> Productos(CancellationToken ct) => Ok(await service.ProductosAsync(ct));

    [HttpPut("aunap/productos/{productoId:guid}")]
    [RequierePermiso(Permisos.ExportacionesConfigurar)]
    public async Task<IActionResult> Enlazar(Guid productoId, EnlazarProductoAunapRequest request, CancellationToken ct) =>
        (await service.EnlazarProductoAsync(productoId, request.EspecieAunapId, ct)).ARespuesta();

    /// <summary>Enlaza los productos cuyo nombre científico es idéntico al de la resolución.</summary>
    [HttpPost("aunap/productos/enlazar-automatico")]
    [RequierePermiso(Permisos.ExportacionesConfigurar)]
    public async Task<IActionResult> EnlazarAutomatico(CancellationToken ct) => Ok(new { enlazados = await service.EnlazarAutomaticoAsync(ct) });

    [HttpGet("embarques/{id:guid}/aunap")]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> Resumen(Guid id, CancellationToken ct) => (await service.ResumenAsync(id, Alcance, ct)).ARespuesta();

    /// <summary>Trámites: VUCE (TM, fechas, cantidades aprobadas), DEX, factura DIAN y CITES.</summary>
    [HttpPut("embarques/{id:guid}/aunap/permiso")]
    [RequierePermiso(Permisos.ExportacionesGestionar)]
    public async Task<IActionResult> Permiso(Guid id, GuardarPermisoAunapRequest request, CancellationToken ct) =>
        (await service.GuardarPermisoAsync(id, request, Alcance, ct)).ARespuesta();

    /// <summary>Numera la proforma (cuando el cliente aprobó la preinvoice).</summary>
    [HttpPost("embarques/{id:guid}/aunap/proforma")]
    [RequierePermiso(Permisos.ExportacionesGestionar)]
    public async Task<IActionResult> Proforma(Guid id, CancellationToken ct)
    {
        var resultado = await service.GenerarProformaAsync(id, Alcance, ct);
        return resultado.Exitoso ? Ok(new { numero = resultado.Valor }) : ResultadoHttp.Error(resultado.Error!);
    }

    [HttpGet("aunap/municipios")]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> Municipios(CancellationToken ct) => Ok(await service.MunicipiosAsync(ct));

    [HttpGet("aunap/centros")]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> Centros(CancellationToken ct) => Ok(await service.CentrosAsync(ct));

    [HttpPut("aunap/centros/{centroId:guid}")]
    [RequierePermiso(Permisos.ExportacionesConfigurar)]
    public async Task<IActionResult> GuardarCentro(Guid centroId, GuardarCentroAunapRequest request, CancellationToken ct) =>
        (await service.GuardarCentroAsync(centroId, request, ct)).ARespuesta();

    [HttpGet("aunap/parametros")]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> Parametros(CancellationToken ct) => Ok(await service.ParametrosAsync(ct));

    [HttpPut("aunap/parametros")]
    [RequierePermiso(Permisos.ExportacionesConfigurar)]
    public async Task<IActionResult> GuardarParametros(GuardarParametrosAunapRequest request, CancellationToken ct) =>
        (await service.GuardarParametrosAsync(request, ct)).ARespuesta();

    /// <summary>Informe mensual AUNAP (lo que salió de bodega en las exportaciones del mes).</summary>
    [HttpGet("aunap/informe/{anio:int}/{mes:int}")]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> Informe(int anio, int mes, CancellationToken ct) => (await service.InformeAsync(anio, mes, ct)).ARespuesta();

    [HttpGet("aunap/informe/{anio:int}/{mes:int}/excel")]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> InformeExcel(int anio, int mes, CancellationToken ct)
    {
        var resultado = await service.InformeExcelAsync(anio, mes, ct);
        if (!resultado.Exitoso)
        {
            return ResultadoHttp.Error(resultado.Error!);
        }

        Response.Headers.CacheControl = "private, no-store";
        return File(resultado.Valor.Contenido, resultado.Valor.TipoContenido, resultado.Valor.Nombre);
    }
}

/// <summary>Empaque: estándar por producto (especie y talla), costos de empaque, costo por despacho y DOA por forma de empaque.</summary>
[ApiController]
[Route("api/v1/exportaciones/empaque")]
public class EmpaqueController(EmpaqueService service, IUsuarioActual usuario) : CrmControllerBase(usuario)
{
    [HttpGet("estandares")]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> Estandares(CancellationToken ct) => Ok(await service.EstandaresAsync(ct));

    [HttpPut("estandares/{productoId:guid}")]
    [RequierePermiso(Permisos.ExportacionesConfigurar)]
    public async Task<IActionResult> GuardarEstandar(Guid productoId, GuardarEstandarEmpaqueRequest request, CancellationToken ct) =>
        (await service.GuardarEstandarAsync(productoId, request, ct)).ARespuesta();

    [HttpGet("estandares/{productoId:guid}/historial")]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> HistorialEstandar(Guid productoId, CancellationToken ct) => Ok(await service.HistorialAsync(productoId, ct));

    [HttpDelete("estandares/{productoId:guid}")]
    [RequierePermiso(Permisos.ExportacionesConfigurar)]
    public async Task<IActionResult> EliminarEstandar(Guid productoId, CancellationToken ct) =>
        (await service.EliminarEstandarAsync(productoId, ct)).ARespuesta();

    [HttpGet("costos")]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> Costos(CancellationToken ct) => Ok(await service.CostosAsync(ct));

    [HttpPut("costos/parametros")]
    [RequierePermiso(Permisos.ExportacionesConfigurar)]
    public async Task<IActionResult> GuardarParametros(GuardarParametrosEmpaqueRequest request, CancellationToken ct) =>
        (await service.GuardarParametrosAsync(request, ct)).ARespuesta();

    [HttpPut("costos/bolsas/{tipoBolsaId:guid}")]
    [RequierePermiso(Permisos.ExportacionesConfigurar)]
    public async Task<IActionResult> GuardarLitros(Guid tipoBolsaId, GuardarLitrosBolsaRequest request, CancellationToken ct) =>
        (await service.GuardarLitrosBolsaAsync(tipoBolsaId, request, ct)).ARespuesta();

    [HttpPut("costos/aditivos/{medicamentoId:guid}")]
    [RequierePermiso(Permisos.ExportacionesConfigurar)]
    public async Task<IActionResult> GuardarCostoAditivo(Guid medicamentoId, GuardarCostoAditivoRequest request, CancellationToken ct) =>
        (await service.GuardarCostoAditivoAsync(medicamentoId, request, ct)).ARespuesta();

    [HttpGet("embarques/{id:guid}")]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> CostoEmbarque(Guid id, CancellationToken ct) => (await service.CostoEmbarqueAsync(id, Alcance, ct)).ARespuesta();

    [HttpGet("doa")]
    [RequierePermiso(Permisos.ExportacionesLeer)]
    public async Task<IActionResult> Doa([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct) =>
        (await service.DoaAsync(desde, hasta, ct)).ARespuesta();
}
