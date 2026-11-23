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
    IReloj reloj,
    ServicioAuditoria auditoria)
{
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
            Opcional(Limpiar(r.Numero), "Número del permiso", 60),
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
        var limpio = r with { Numero = Limpiar(r.Numero), Notas = LimpiarTextoLargo(r.Notas), Lineas = lineas };
        await repositorio.GuardarPermisoAsync(embarqueId, limpio, reloj.AhoraUtc, ct);

        string Resumen(IEnumerable<(Guid Especie, int Cantidad)> filas) =>
            string.Join(", ", filas.OrderBy(f => especies.GetValueOrDefault(f.Especie)?.NombreCientifico)
                .Select(f => $"{especies.GetValueOrDefault(f.Especie)?.NombreCientifico ?? "?"} {f.Cantidad}"));
        await auditoria.CambiosAsync(
            EmbarqueService.Entidad, embarqueId,
            [
                .. ComparadorAuditoria.Comparar(
                    new PermisoAuditable(anterior?.Numero, anterior?.Fecha, anterior?.Notas,
                        Resumen(lineasAntes.Select(l => (l.EspecieAunapId, l.CantidadAutorizada)))),
                    new PermisoAuditable(limpio.Numero, limpio.Fecha, limpio.Notas, Resumen(lineas.Select(l => (l.EspecieAunapId, l.Cantidad))))),
            ],
            ct);
        return Resultado.Ok();
    }

    private static string? Describir(string? codigo, string? nombre) => codigo is null ? null : $"{codigo} {nombre}";

    private sealed record PermisoAuditable(string? PermisoAunap, DateOnly? FechaPermisoAunap, string? NotasPermisoAunap, string AutorizadoAunap);
}
