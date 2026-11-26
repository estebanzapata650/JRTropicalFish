using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Crm;

namespace JRTropicalFish.Application.Exportaciones;

/// <summary>
/// Empaque: estándar por producto (especie y talla), costos de empaque y DOA por forma de empaque. El costo de un
/// despacho se calcula con su empaque real (ver <see cref="ReglasEmpaque"/>).
/// </summary>
public class EmpaqueService(
    IEmpaqueRepository repositorio,
    IConfiguracionExportacionRepository configuracion,
    EmbarqueService embarques,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    public const string EntidadEstandar = "EstandarEmpaque";
    public const string EntidadCostos = "CostosEmpaque";

    /// <summary>Id fijo para la auditoría de los costos de empaque (son de configuración, no de un registro).</summary>
    public static readonly Guid IdCostos = new("0193a5c0-0000-7000-8000-00000000e0a1");

    public Task<IReadOnlyList<EstandarEmpaqueFila>> EstandaresAsync(CancellationToken ct = default) => repositorio.EstandaresAsync(ct);

    public Task<IReadOnlyList<RegistroAuditoriaLectura>> HistorialAsync(Guid productoId, CancellationToken ct = default) =>
        auditoria.HistorialAsync(EntidadEstandar, productoId, ct);

    public async Task<CostosEmpaque> CostosAsync(CancellationToken ct = default) =>
        new(await repositorio.ParametrosAsync(ct), await repositorio.BolsasAsync(ct), await repositorio.AditivosAsync(ct));

    public async Task<Resultado> GuardarEstandarAsync(Guid productoId, GuardarEstandarEmpaqueRequest r, CancellationToken ct = default)
    {
        var estandares = await repositorio.EstandaresAsync(ct);
        if (estandares.FirstOrDefault(e => e.ProductoId == productoId) is not { } anterior)
        {
            return Error.NoEncontrado("El producto no existe o está inactivo.");
        }

        var costos = await CostosAsync(ct);
        var aditivos = (r.Aditivos ?? []).Distinct().ToList();
        if (ReglasEmpaque.Validar(r with { Aditivos = aditivos }, costos.Bolsas.Select(b => b.TipoBolsaId).ToHashSet(),
                costos.Aditivos.Select(a => a.MedicamentoId).ToHashSet()) is { } error)
        {
            return Error.Validacion(error);
        }

        var notas = string.IsNullOrWhiteSpace(r.Notas) ? null : r.Notas.Trim();
        var limpio = r with { Notas = notas, Aditivos = aditivos };
        await repositorio.GuardarEstandarAsync(productoId, limpio, reloj.AhoraUtc, ct);

        string Bolsa(Guid? id) => id is { } x ? costos.Bolsas.FirstOrDefault(b => b.TipoBolsaId == x)?.Nombre ?? "?" : "—";
        string Aditivos(IEnumerable<Guid> ids) =>
            string.Join(", ", ids.Select(id => costos.Aditivos.FirstOrDefault(a => a.MedicamentoId == id)?.Nombre ?? "?").Order(StringComparer.Ordinal));
        var antes = anterior.TieneEstandar
            ? new EstandarAuditable(Bolsa(anterior.TipoBolsaId), anterior.PecesMin, anterior.PecesMax, anterior.LitrosAgua, anterior.Notas,
                anterior.Revisado ? "Sí" : "No", Aditivos(anterior.Aditivos))
            : null;
        var despues = new EstandarAuditable(Bolsa(limpio.TipoBolsaId), limpio.PecesMin, limpio.PecesMax, limpio.LitrosAgua, limpio.Notas,
            limpio.Revisado ? "Sí" : "No", Aditivos(aditivos));
        if (antes is null)
        {
            await auditoria.CreacionAsync(EntidadEstandar, productoId, despues, ct: ct);
        }
        else
        {
            await auditoria.CambiosAsync(EntidadEstandar, productoId, ComparadorAuditoria.Comparar(antes, despues), ct);
        }

        return Resultado.Ok();
    }

    /// <summary>Quita el estándar: el producto vuelve a usar los peces por bolsa del catálogo.</summary>
    public async Task<Resultado> EliminarEstandarAsync(Guid productoId, CancellationToken ct = default)
    {
        if ((await repositorio.EstandaresAsync(ct)).FirstOrDefault(e => e.ProductoId == productoId) is not { TieneEstandar: true })
        {
            return Error.NoEncontrado("El producto no tiene estándar de empaque.");
        }

        await repositorio.EliminarEstandarAsync(productoId, ct);
        await auditoria.CambiosAsync(EntidadEstandar, productoId, [new CambioAuditado("Estándar de empaque", "Definido", "Eliminado")], ct);
        return Resultado.Ok();
    }

    public async Task<Resultado> GuardarParametrosAsync(GuardarParametrosEmpaqueRequest r, CancellationToken ct = default)
    {
        if (r.CostoHeatPackCop is < 0 or > 10_000_000 || r.CostoOxigenoBolsaCop is < 0 or > 10_000_000)
        {
            return Error.Validacion("Los costos van de 0 a 10.000.000 COP.");
        }

        var antes = await repositorio.ParametrosAsync(ct);
        await repositorio.GuardarParametrosAsync(r, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(EntidadCostos, IdCostos,
        [
            .. Cambio("Costo heat pack (COP)", antes.CostoHeatPackCop, r.CostoHeatPackCop),
            .. Cambio("Costo oxígeno por bolsa (COP)", antes.CostoOxigenoBolsaCop, r.CostoOxigenoBolsaCop),
        ], ct);
        return Resultado.Ok();
    }

    public async Task<Resultado> GuardarLitrosBolsaAsync(Guid tipoBolsaId, GuardarLitrosBolsaRequest r, CancellationToken ct = default)
    {
        if ((await repositorio.BolsasAsync(ct)).FirstOrDefault(b => b.TipoBolsaId == tipoBolsaId) is not { } bolsa)
        {
            return Error.NoEncontrado("El tipo de bolsa no existe.");
        }

        if (r.LitrosAgua is <= 0 or > 50)
        {
            return Error.Validacion("Los litros de agua por bolsa van de más de 0 a 50.");
        }

        await repositorio.GuardarLitrosBolsaAsync(tipoBolsaId, r.LitrosAgua, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(EntidadCostos, IdCostos, [.. Cambio($"Litros de agua · bolsa {bolsa.Nombre}", bolsa.LitrosAgua, r.LitrosAgua)], ct);
        return Resultado.Ok();
    }

    public async Task<Resultado> GuardarCostoAditivoAsync(Guid medicamentoId, GuardarCostoAditivoRequest r, CancellationToken ct = default)
    {
        if ((await repositorio.AditivosAsync(ct)).FirstOrDefault(a => a.MedicamentoId == medicamentoId) is not { } aditivo)
        {
            return Error.NoEncontrado("El aditivo no existe o no se dosifica por volumen de agua.");
        }

        if (r.CostoCopUnidad is < 0 or > 10_000_000)
        {
            return Error.Validacion("El costo va de 0 a 10.000.000 COP.");
        }

        await repositorio.GuardarCostoAditivoAsync(medicamentoId, r.CostoCopUnidad, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(EntidadCostos, IdCostos,
            [.. Cambio($"Costo {aditivo.Nombre} por {aditivo.Unidad} (COP)", aditivo.CostoCopUnidad, r.CostoCopUnidad)], ct);
        return Resultado.Ok();
    }

    /// <summary>Costo de empaque del despacho y las filas que se salen del estándar.</summary>
    public async Task<Resultado<CostoEmpaqueEmbarque>> CostoEmbarqueAsync(Guid embarqueId, AlcanceCrm alcance, CancellationToken ct = default)
    {
        var detalle = await embarques.ObtenerAsync(embarqueId, alcance, ct);
        if (!detalle.Exitoso)
        {
            return detalle.Error!;
        }

        var d = detalle.Valor;
        var estandares = (await repositorio.EstandaresAsync(ct)).ToDictionary(e => e.ProductoId);
        var tiposCaja = (await configuracion.TiposCajaAsync(ct)).ToDictionary(t => t.Id);
        return ReglasEmpaque.Costo(d.Empaque, d.CajasReales, tiposCaja, estandares, await CostosAsync(ct));
    }

    public async Task<Resultado<IReadOnlyList<DoaEmpaqueFila>>> DoaAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct = default)
    {
        var fin = hasta ?? reloj.Hoy;
        var inicio = desde ?? fin.AddMonths(-12);
        if (inicio > fin)
        {
            return Error.Validacion("La fecha inicial es posterior a la final.");
        }

        return Resultado<IReadOnlyList<DoaEmpaqueFila>>.Ok(await repositorio.DoaAsync(inicio, fin, ct));
    }

    private static IEnumerable<CambioAuditado> Cambio(string campo, decimal? antes, decimal? despues) =>
        antes == despues ? [] : [new CambioAuditado(campo, antes?.ToString("0.##"), despues?.ToString("0.##"))];

    private sealed record EstandarAuditable(
        string TipoBolsa, int? PecesMinimo, int? PecesMaximo, decimal? LitrosAgua, string? Notas, string Revisado, string Aditivos);
}
