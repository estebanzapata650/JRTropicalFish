using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;

namespace JRTropicalFish.Application.Crm;

/// <summary>
/// Metas, parámetros comerciales y dashboard ejecutivo.
///
/// <para><b>Supuestos de cálculo</b> (documentados en <c>docs/dominio/crm.md</c>):</para>
/// <list type="bullet">
/// <item>Venta del mes = Σ <c>total_cop</c> de las ventas con fecha de pedido en el mes (USD × su propia TRM).</item>
/// <item>Venta neta = venta del mes − flete aéreo − DOA reportado de esas ventas, pasados a COP con la TRM
///   de cada venta. La nota crédito al cliente es el mismo DOA: no se descuenta dos veces.</item>
/// <item>Pool de comisiones = venta neta × porcentaje de comisión, repartido por igual entre los
///   comerciales con meta ese mes (o, si nadie tiene meta, entre los comerciales habilitados).</item>
/// <item>Despachos = ventas de exportación del mes. Cliente nuevo = su primera venta cae en el mes.</item>
/// </list>
/// </summary>
public class MetricasService(
    IMetricasRepository metricas,
    ITrackingRepository tracking,
    ITareaRepository tareas,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    public const string EntidadParametros = "ParametrosComerciales";
    public const string EntidadMeta = "MetaComercial";

    /// <summary>Colombia no tiene horario de verano: el día de negocio empieza a las 05:00 UTC.</summary>
    private static readonly TimeSpan DesfaseBogota = TimeSpan.FromHours(5);

    /// <summary>La auditoría de parámetros usa un id fijo (la tabla tiene una sola fila).</summary>
    public static readonly Guid IdParametros = new("00000000-0000-0000-0000-000000000001");

    public Task<ParametrosComercialesInfo> ParametrosAsync(CancellationToken ct = default) => metricas.ObtenerParametrosAsync(ct);

    public async Task<Resultado> ActualizarParametrosAsync(GuardarParametrosRequest r, CancellationToken ct = default)
    {
        var error = ValidacionCrm.Primero(
            r.MetaEmpresaCop <= 0 ? Error.Validacion("La meta de la empresa debe ser mayor que cero.") : null,
            r.MetaComercialCop <= 0 ? Error.Validacion("La meta por comercial debe ser mayor que cero.") : null,
            r.PuntoEquilibrioDespachos is < 0 or > 1000 ? Error.Validacion("El punto de equilibrio no es válido.") : null,
            r.PorcentajeComision is < 0 or > 100 ? Error.Validacion("El porcentaje de comisión va de 0 a 100.") : null,
            r.UmbralDoa is < 0 or > 100 ? Error.Validacion("El umbral de DOA va de 0 a 100.") : null,
            r.MetaClientesNuevos is < 0 or > 1000 ? Error.Validacion("La meta de clientes nuevos no es válida.") : null);
        if (error is not null)
        {
            return error;
        }

        var anterior = await metricas.ObtenerParametrosAsync(ct);
        await metricas.ActualizarParametrosAsync(r, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(
            EntidadParametros, IdParametros,
            ComparadorAuditoria.Comparar(
                new ParametrosAuditable(
                    anterior.MetaEmpresaCop, anterior.MetaComercialCop, anterior.PuntoEquilibrioDespachos,
                    anterior.PorcentajeComision, anterior.UmbralDoa, anterior.MetaClientesNuevos),
                new ParametrosAuditable(
                    r.MetaEmpresaCop, r.MetaComercialCop, r.PuntoEquilibrioDespachos, r.PorcentajeComision, r.UmbralDoa,
                    r.MetaClientesNuevos)),
            ct);
        return Resultado.Ok();
    }

    public Task<IReadOnlyList<MetaMesFila>> MetasDelMesAsync(DateOnly mes, CancellationToken ct = default) =>
        metricas.MetasDelMesAsync(PrimerDia(mes), ct);

    /// <summary>Guarda (crea o reemplaza) las metas de varios comerciales para un mes.</summary>
    public async Task<Resultado> GuardarMetasAsync(GuardarMetasRequest r, CancellationToken ct = default)
    {
        var mes = PrimerDia(r.Mes);
        var metas = r.Metas ?? [];
        if (metas.Count == 0)
        {
            return Error.Validacion("No hay metas para guardar.");
        }

        if (metas.Any(m => m.MetaCop < 0))
        {
            return Error.Validacion("Una meta no puede ser negativa.");
        }

        if (metas.Select(m => m.ComercialId).Distinct().Count() != metas.Count)
        {
            return Error.Validacion("Hay un comercial repetido.");
        }

        var actuales = (await metricas.MetasDelMesAsync(mes, ct)).ToDictionary(m => m.ComercialId);
        if (metas.FirstOrDefault(m => !actuales.ContainsKey(m.ComercialId)) is not null)
        {
            return Error.Validacion("Solo se fijan metas a comerciales habilitados (perfil con «crm:vender»).");
        }

        var ahora = reloj.AhoraUtc;
        foreach (var meta in metas)
        {
            var actual = actuales[meta.ComercialId];
            if (actual.MetaCop == meta.MetaCop)
            {
                continue;
            }

            var id = actual.MetaId ?? Guid.CreateVersion7();
            await metricas.GuardarMetaAsync(id, mes, meta.ComercialId, meta.MetaCop, ahora, ct);
            await auditoria.CambiosAsync(
                EntidadMeta, id,
                [
                    new CambioAuditado("Comercial", null, actual.ComercialNombre),
                    new CambioAuditado("Mes", null, mes.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture)),
                    new CambioAuditado("MetaCop", Formato(actual.MetaCop), Formato(meta.MetaCop)),
                ],
                ct);
        }

        return Resultado.Ok();
    }

    public async Task<DashboardCrm> DashboardAsync(DateOnly? mesPedido, AlcanceCrm alcance, CancellationToken ct = default)
    {
        var mes = PrimerDia(mesPedido ?? reloj.Hoy);
        var hasta = mes.AddMonths(1).AddDays(-1);
        var desdeUtc = mes.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) + DesfaseBogota;
        var hastaUtc = mes.AddMonths(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) + DesfaseBogota;

        var parametros = await metricas.ObtenerParametrosAsync(ct);
        var porComercial = await metricas.VentasPorComercialAsync(mes, hasta, ct);
        var metas = await metricas.MetasDelMesAsync(mes, ct);
        var resumen = await metricas.ResumenPeriodoAsync(mes, hasta, ct);
        var embudo = await metricas.EmbudoAsync(desdeUtc, hastaUtc, null, ct);

        var ventasCop = porComercial.Sum(v => v.VentasCop);
        var doaCop = porComercial.Sum(v => v.DoaCop);
        var fleteCop = porComercial.Sum(v => v.FleteCop);
        var calculo = CalcularComisiones(ventasCop, doaCop, fleteCop, parametros.PorcentajeComision, metas, porComercial);

        var avances = metas.Select(m => (m.ComercialId, m.ComercialNombre))
            .Union(porComercial.Select(v => (v.ComercialId, v.ComercialNombre)))
            .DistinctBy(c => c.ComercialId)
            .Select(c =>
            {
                var ventas = porComercial.FirstOrDefault(v => v.ComercialId == c.ComercialId);
                var meta = metas.FirstOrDefault(m => m.ComercialId == c.ComercialId)?.MetaCop ?? parametros.MetaComercialCop;
                return new AvanceComercial(
                    c.ComercialId, c.ComercialNombre, ventas?.Ventas ?? 0, ventas?.VentasCop ?? 0, meta,
                    Porcentaje(ventas?.VentasCop ?? 0, meta),
                    calculo.PorComercial.GetValueOrDefault(c.ComercialId));
            })
            .OrderByDescending(a => a.VentasCop)
            .ToList();

        // Alertas y tareas respetan el alcance: un comercial ve las suyas.
        var alertas = (await tracking.ListarAsync(alcance.FiltroComercial, null, ct))
            .Where(t => t.PorcentajeDoa > parametros.UmbralDoa
                        && (t.NotaCreditoNo is null || (t.FechaDespachoVuelo is { } f && f >= mes && f <= hasta)))
            .ToList();
        var vencidas = (await tareas.ListarAsync(reloj.Hoy, alcance.FiltroComercial, null, ct)).Where(t => t.Vencida).ToList();

        return new DashboardCrm(
            mes, parametros, ventasCop, doaCop, fleteCop, calculo.VentaNeta, Porcentaje(ventasCop, parametros.MetaEmpresaCop),
            calculo.Pool, resumen.Despachos, resumen.ClientesNuevos, avances, embudo, alertas, vencidas);
    }

    /// <summary>
    /// Venta neta, pool y reparto. Regla pura para poder probarla. El pool se reparte por igual
    /// entre los comerciales con meta en el mes; si nadie tiene meta, entre todos los comerciales
    /// habilitados (<paramref name="metas"/> los trae a todos, con o sin meta).
    /// </summary>
    public static (decimal VentaNeta, decimal Pool, IReadOnlyDictionary<Guid, decimal> PorComercial) CalcularComisiones(
        decimal ventasCop,
        decimal doaCop,
        decimal fleteCop,
        decimal porcentajeComision,
        IReadOnlyList<MetaMesFila> metas,
        IReadOnlyList<VentasComercialFila> porComercial)
    {
        var ventaNeta = Math.Max(0, ventasCop - doaCop - fleteCop);
        var pool = Math.Round(ventaNeta * porcentajeComision / 100, 0, MidpointRounding.AwayFromZero);

        var conMeta = metas.Where(m => m.MetaId is not null).Select(m => m.ComercialId).ToList();
        var beneficiarios = conMeta.Count > 0 ? conMeta : metas.Select(m => m.ComercialId).ToList();
        if (beneficiarios.Count == 0)
        {
            beneficiarios = [.. porComercial.Select(v => v.ComercialId)];
        }

        var parte = beneficiarios.Count == 0 ? 0 : Math.Round(pool / beneficiarios.Count, 0, MidpointRounding.AwayFromZero);
        return (ventaNeta, pool, beneficiarios.Distinct().ToDictionary(id => id, _ => parte));
    }

    private static decimal Porcentaje(decimal valor, decimal meta) =>
        meta <= 0 ? 0 : Math.Round(valor / meta * 100, 1, MidpointRounding.AwayFromZero);

    private static DateOnly PrimerDia(DateOnly fecha) => new(fecha.Year, fecha.Month, 1);

    private static string? Formato(decimal? valor) =>
        valor?.ToString("G29", System.Globalization.CultureInfo.InvariantCulture);
}
