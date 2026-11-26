using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Catalogos;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Crm;
using JRTropicalFish.Domain.Exportaciones;
using static JRTropicalFish.Application.Crm.ValidacionCrm;

namespace JRTropicalFish.Application.Exportaciones;

/// <summary>
/// AUNAP: el listado de especies autorizadas, el enlace de cada producto con su especie y el permiso de cada
/// embarque. Lo que se declara a la autoridad sale siempre de lo real (ver <see cref="ReglasAunap"/>).
/// </summary>
public class AunapService(
    IAunapRepository repositorio,
    IEmbarqueRepository embarques,
    IConfiguracionExportacionRepository configuracion,
    IGeneradorInformeAunapExcel generadorExcel,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    public const string EntidadParametros = "ParametrosAunap";
    public const string EntidadOrigen = "OrigenAunap";

    /// <summary>Id fijo para la auditoría del encabezado del informe (fila única).</summary>
    public static readonly Guid IdParametros = new("0193a5c0-0000-7000-8000-00000000a0a1");

    /// <summary>Estados desde los que se puede generar la proforma: el cliente ya aprobó la preinvoice.</summary>
    private static readonly HashSet<string> EstadosConProforma =
        [EstadosEmbarque.Confirmado, EstadosEmbarque.Empacado, EstadosEmbarque.Facturado, EstadosEmbarque.Cerrado];

    public Task<IReadOnlyList<EspecieAunapFila>> EspeciesAsync(CancellationToken ct = default) => repositorio.EspeciesAsync(ct);

    public Task<IReadOnlyList<ProductoAunapFila>> ProductosAsync(CancellationToken ct = default) => repositorio.ProductosAsync(ct);

    /// <summary>Carga o actualiza el listado de la resolución (por código). No borra: una especie retirada se desactiva a mano.</summary>
    public async Task<Resultado<ImportacionAunap>> ImportarAsync(ImportarAunapRequest r, CancellationToken ct = default)
    {
        var especies = r.Especies ?? [];
        if (ReglasAunap.ValidarImportacion(especies) is { } error)
        {
            return Error.Validacion(error);
        }

        return await repositorio.ImportarAsync(especies, reloj.AhoraUtc, ct);
    }

    /// <summary>Enlaza los productos cuyo nombre científico es idéntico al de una especie AUNAP. Devuelve cuántos.</summary>
    public Task<int> EnlazarAutomaticoAsync(CancellationToken ct = default) => repositorio.EnlazarAutomaticoAsync(reloj.AhoraUtc, ct);

    /// <summary>
    /// Enlace manual (sinónimos taxonómicos, p. ej. Ariopsis seemanni = Sciades seemanni). Nulo quita el enlace.
    /// Queda en el historial del producto.
    /// </summary>
    public async Task<Resultado> EnlazarProductoAsync(Guid productoId, Guid? especieAunapId, CancellationToken ct = default)
    {
        var productos = await repositorio.ProductosAsync(ct);
        if (productos.FirstOrDefault(p => p.ProductoId == productoId) is not { } producto)
        {
            return Error.NoEncontrado("El producto no existe.");
        }

        EspecieAunapFila? especie = null;
        if (especieAunapId is { } id && ((especie = (await repositorio.EspeciesAsync(ct)).FirstOrDefault(e => e.Id == id)) is null || !especie.Activo))
        {
            return Error.Validacion("Elige una especie AUNAP vigente.");
        }

        await repositorio.EnlazarProductoAsync(productoId, especieAunapId, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(
            ProductoService.Entidad, productoId,
            [new CambioAuditado("Especie AUNAP", Describir(producto.AunapCodigo, producto.AunapNombreCientifico), Describir(especie?.Codigo, especie?.NombreCientifico))],
            ct);
        return Resultado.Ok();
    }

    /// <summary>Solicitado (pedido), autorizado (permiso) y empacado por especie AUNAP, con los productos sin código.</summary>
    public async Task<Resultado<ResumenAunap>> ResumenAsync(Guid embarqueId, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await embarques.ObtenerAsync(embarqueId, ct) is not { } e || !alcance.PuedeVer(e.ComercialId))
        {
            return Error.NoEncontrado("El embarque no existe.");
        }

        return await ResumenDeAsync(embarqueId, ct);
    }

    public async Task<ResumenAunap> ResumenDeAsync(Guid embarqueId, CancellationToken ct = default)
    {
        var mapa = (await repositorio.ProductosAsync(ct)).ToDictionary(p => p.ProductoId);
        var lineas = await embarques.LineasAsync(embarqueId, ct);
        var empaque = await embarques.EmpaqueAsync(embarqueId, ct);
        var permiso = await repositorio.PermisoAsync(embarqueId, ct);
        var autorizados = (await repositorio.PermisoLineasAsync(embarqueId, ct)).ToDictionary(l => l.EspecieAunapId, l => l.CantidadAutorizada);
        return ReglasAunap.Resumen(
            [.. lineas.Select(l => (l.ProductoId, l.ProductoCodigo, l.NombreCientifico, l.Cantidad))],
            [.. empaque.Select(x => (x.ProductoId, x.Cantidad))],
            mapa, permiso, autorizados);
    }

    /// <summary>Mapa producto → especie AUNAP (para los documentos y los avisos del embarque).</summary>
    public async Task<IReadOnlyDictionary<Guid, ProductoAunapFila>> MapaAsync(CancellationToken ct = default) =>
        (await repositorio.ProductosAsync(ct)).ToDictionary(p => p.ProductoId);

    /// <summary>Registra el permiso: número, fecha y la cantidad autorizada por especie (lo que diga el acto de la AUNAP).</summary>
    public async Task<Resultado> GuardarPermisoAsync(Guid embarqueId, GuardarPermisoAunapRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await embarques.ObtenerAsync(embarqueId, ct) is not { } e || !alcance.PuedeVer(e.ComercialId))
        {
            return Error.NoEncontrado("El embarque no existe.");
        }

        if (e.Estado == EstadosEmbarque.Cancelado)
        {
            return Error.Conflicto("El embarque está cancelado.");
        }

        var lineas = (r.Lineas ?? []).Where(l => l.Cantidad > 0).ToList();
        var especies = (await repositorio.EspeciesAsync(ct)).ToDictionary(x => x.Id);
        var error = Primero(
            Opcional(Limpiar(r.Numero), "N.º de trámite VUCE", 60),
            Opcional(Limpiar(r.DexNumero), "N.º del DEX", 40),
            Opcional(Limpiar(r.FacturaDian), "Factura DIAN", 40),
            Opcional(Limpiar(r.CitesNumero), "N.º CITES", 60),
            r.Fecha is { } aprobada && r.VuceFechaSolicitud is { } solicitada && aprobada < solicitada
                ? Error.Validacion("La VUCE no puede aprobarse antes de solicitarse.")
                : null,
            Opcional(LimpiarTextoLargo(r.Notas), "Notas", 1000),
            (r.Lineas ?? []).Any(l => l.Cantidad is < 0 or > 10_000_000) ? Error.Validacion("Las cantidades autorizadas van de 0 a 10.000.000.") : null,
            lineas.Any(l => !especies.ContainsKey(l.EspecieAunapId)) ? Error.Validacion("Hay una especie AUNAP que no existe.") : null,
            lineas.GroupBy(l => l.EspecieAunapId).Any(g => g.Count() > 1) ? Error.Validacion("Una especie aparece dos veces en el permiso.") : null);
        if (error is not null)
        {
            return error;
        }

        var anterior = await repositorio.PermisoAsync(embarqueId, ct);
        var lineasAntes = await repositorio.PermisoLineasAsync(embarqueId, ct);
        var limpio = r with
        {
            Numero = Limpiar(r.Numero), Notas = LimpiarTextoLargo(r.Notas), Lineas = lineas, DexNumero = Limpiar(r.DexNumero),
            FacturaDian = Limpiar(r.FacturaDian), CitesNumero = Limpiar(r.CitesNumero),
        };
        await repositorio.GuardarPermisoAsync(embarqueId, limpio, reloj.AhoraUtc, ct);

        string Resumen(IEnumerable<(Guid Especie, int Cantidad)> filas) =>
            string.Join(", ", filas.OrderBy(f => especies.GetValueOrDefault(f.Especie)?.NombreCientifico)
                .Select(f => $"{especies.GetValueOrDefault(f.Especie)?.NombreCientifico ?? "?"} {f.Cantidad}"));
        await auditoria.CambiosAsync(
            EmbarqueService.Entidad, embarqueId,
            [
                .. ComparadorAuditoria.Comparar(
                    new PermisoAuditable(anterior?.Numero, anterior?.VuceFechaSolicitud, anterior?.Fecha, anterior?.DexNumero,
                        anterior?.FacturaDian, anterior?.CitesNumero, anterior?.CitesFecha, anterior?.Notas,
                        Resumen(lineasAntes.Select(l => (l.EspecieAunapId, l.CantidadAutorizada)))),
                    new PermisoAuditable(limpio.Numero, limpio.VuceFechaSolicitud, limpio.Fecha, limpio.DexNumero, limpio.FacturaDian,
                        limpio.CitesNumero, limpio.CitesFecha, limpio.Notas, Resumen(lineas.Select(l => (l.EspecieAunapId, l.Cantidad))))),
            ],
            ct);
        return Resultado.Ok();
    }

    private static string? Describir(string? codigo, string? nombre) => codigo is null ? null : $"{codigo} {nombre}";

    /// <summary>
    /// Numera la proforma (PRO-001…) cuando el cliente aprobó la preinvoice. Es la base de la VUCE y de los demás
    /// trámites; sale de las especies y cantidades reales del pedido. Si ya tiene número, lo conserva.
    /// </summary>
    public async Task<Resultado<string>> GenerarProformaAsync(Guid embarqueId, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await embarques.ObtenerAsync(embarqueId, ct) is not { } e || !alcance.PuedeVer(e.ComercialId))
        {
            return Error.NoEncontrado("El embarque no existe.");
        }

        if (!EstadosConProforma.Contains(e.Estado))
        {
            return Error.Conflicto("La proforma se genera cuando el cliente aprueba la preinvoice (embarque confirmado).");
        }

        var resumen = await ResumenDeAsync(embarqueId, ct);
        if (resumen.Filas.Count == 0)
        {
            return Error.Validacion("El embarque no tiene especies con código AUNAP.");
        }

        if (resumen.SinCodigo.Count > 0)
        {
            return Error.Validacion($"Hay especies sin código AUNAP ({string.Join(", ", resumen.SinCodigo.Select(s => s.ProductoCodigo))}): no se pueden declarar.");
        }

        var anterior = resumen.Permiso?.ProformaNumero;
        var numero = await repositorio.AsignarProformaAsync(embarqueId, reloj.Hoy, reloj.AhoraUtc, ct);
        if (anterior is null)
        {
            await auditoria.CambiosAsync(EmbarqueService.Entidad, embarqueId, [new CambioAuditado("Proforma", null, numero)], ct);
        }

        return numero;
    }

    public Task<PermisoAunapFila?> TramitesAsync(Guid embarqueId, CancellationToken ct = default) => repositorio.PermisoAsync(embarqueId, ct);

    public Task<IReadOnlyList<MunicipioAunapFila>> MunicipiosAsync(CancellationToken ct = default) => repositorio.MunicipiosAsync(ct);

    public Task<IReadOnlyList<CentroAcopioAunapFila>> CentrosAsync(CancellationToken ct = default) => repositorio.CentrosAsync(ct);

    /// <summary>Municipio de origen que se declara para lo que llega del centro (o «no se reporta», criadero).</summary>
    public async Task<Resultado> GuardarCentroAsync(Guid centroId, GuardarCentroAunapRequest r, CancellationToken ct = default)
    {
        var centros = await repositorio.CentrosAsync(ct);
        if (centros.FirstOrDefault(c => c.CentroAcopioId == centroId) is not { } centro)
        {
            return Error.NoEncontrado("El centro de acopio no existe.");
        }

        var municipios = await repositorio.MunicipiosAsync(ct);
        var municipio = r.MunicipioAunapId is { } id ? municipios.FirstOrDefault(m => m.Id == id) : null;
        if (r.MunicipioAunapId is not null && municipio is null)
        {
            return Error.Validacion("El municipio no existe en el listado de la AUNAP.");
        }

        var municipioId = r.Excluir ? null : r.MunicipioAunapId;
        await repositorio.GuardarCentroAsync(centroId, municipioId, r.Excluir, reloj.AhoraUtc, ct);
        string Origen(bool excluir, string? departamento, string? nombre) =>
            excluir ? "No se reporta (criadero)" : nombre is null ? "Sin definir" : $"{nombre} ({departamento})";
        await auditoria.CambiosAsync(
            EntidadOrigen, centroId,
            [new CambioAuditado($"Origen AUNAP de {centro.CentroNombre}", Origen(centro.Excluir, centro.Departamento, centro.Municipio),
                Origen(r.Excluir, r.Excluir ? null : municipio?.Departamento, r.Excluir ? null : municipio?.Municipio))],
            ct);
        return Resultado.Ok();
    }

    public Task<ParametrosAunapFila> ParametrosAsync(CancellationToken ct = default) => repositorio.ParametrosAsync(ct);

    /// <summary>Encabezado del informe mensual: representante legal, resolución y vigencia del permiso de comercialización.</summary>
    public async Task<Resultado> GuardarParametrosAsync(GuardarParametrosAunapRequest r, CancellationToken ct = default)
    {
        var limpio = new GuardarParametrosAunapRequest(
            Limpiar(r.RepresentanteLegal), Limpiar(r.Resolucion), r.VigenciaDesde, r.VigenciaHasta, Limpiar(r.Direccion),
            Limpiar(r.Correo)?.ToLowerInvariant(), Limpiar(r.Telefono));
        var error = Primero(
            Opcional(limpio.RepresentanteLegal, "Representante legal", 150),
            Opcional(limpio.Resolucion, "Resolución", 100),
            Opcional(limpio.Direccion, "Dirección", 250),
            Opcional(limpio.Correo, "Correo", 320),
            Opcional(limpio.Telefono, "Teléfono", 60),
            limpio.VigenciaDesde is { } desde && limpio.VigenciaHasta is { } hasta && hasta < desde
                ? Error.Validacion("La vigencia termina antes de empezar.")
                : null);
        if (error is not null)
        {
            return error;
        }

        var anterior = await repositorio.ParametrosAsync(ct);
        await repositorio.GuardarParametrosAsync(limpio, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(
            EntidadParametros, IdParametros,
            ComparadorAuditoria.Comparar(
                new ParametrosAuditables(anterior.RepresentanteLegal, anterior.Resolucion, anterior.VigenciaDesde, anterior.VigenciaHasta,
                    anterior.Direccion, anterior.Correo, anterior.Telefono),
                new ParametrosAuditables(limpio.RepresentanteLegal, limpio.Resolucion, limpio.VigenciaDesde, limpio.VigenciaHasta,
                    limpio.Direccion, limpio.Correo, limpio.Telefono)),
            ct);
        return Resultado.Ok();
    }

    /// <summary>Informe mensual AUNAP: lo que salió de bodega en las exportaciones del mes, con sus avisos.</summary>
    public async Task<Resultado<InformeAunap>> InformeAsync(int anio, int mes, CancellationToken ct = default)
    {
        if (anio is < 2020 or > 2100 || mes is < 1 or > 12)
        {
            return Error.Validacion("Mes no válido.");
        }

        var (desde, hasta) = ReglasInformeAunap.Rango(anio, mes);
        var despachos = await repositorio.DespachosAsync(desde, hasta, ct);
        var (filas, avisos, excluidos) = ReglasInformeAunap.Armar(despachos);
        var empresa = await configuracion.DatosEmpresaAsync(ct);
        return new InformeAunap(
            anio, mes, empresa.RazonSocial, await repositorio.ParametrosAsync(ct), filas, avisos, excluidos,
            despachos.Select(d => d.EmbarqueId).Distinct().Count());
    }

    public async Task<Resultado<ArchivoGenerado>> InformeExcelAsync(int anio, int mes, CancellationToken ct = default)
    {
        var informe = await InformeAsync(anio, mes, ct);
        if (!informe.Exitoso)
        {
            return informe.Error!;
        }

        var nombre = $"Informe AUNAP {ReglasInformeAunap.NombreMes(anio, mes)} {informe.Valor.RazonSocial}.xlsx";
        return new ArchivoGenerado(
            generadorExcel.Generar(informe.Valor), nombre, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    private sealed record PermisoAuditable(
        string? VuceTramite,
        DateOnly? VuceSolicitud,
        DateOnly? VuceAprobacion,
        string? Dex,
        string? FacturaDian,
        string? Cites,
        DateOnly? FechaCites,
        string? NotasTramites,
        string AprobadoVuce);

    private sealed record ParametrosAuditables(
        string? RepresentanteLegal,
        string? Resolucion,
        DateOnly? VigenciaDesde,
        DateOnly? VigenciaHasta,
        string? Direccion,
        string? Correo,
        string? Telefono);
}
