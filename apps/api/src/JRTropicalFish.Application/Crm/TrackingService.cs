using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Domain.Crm;
using static JRTropicalFish.Application.Crm.ValidacionCrm;

namespace JRTropicalFish.Application.Crm;

/// <summary>
/// Torre de control de vuelos: fases logísticas de cada despacho de exportación y control DOA. El
/// <c>% DOA</c> lo calcula Postgres (columna generada), así que nunca queda desalineado con el FOB.
/// Los registros nacen con la venta; aquí solo se editan.
/// </summary>
public class TrackingService(ITrackingRepository tracking, IReloj reloj, ServicioAuditoria auditoria)
{
    public const string Entidad = "TrackingVuelo";
    public const string Prefijo = "TRK";

    public Task<IReadOnlyList<TrackingFila>> ListarAsync(AlcanceCrm alcance, Guid? clienteId = null, CancellationToken ct = default) =>
        tracking.ListarAsync(alcance.FiltroComercial, clienteId, ct);

    public async Task<Resultado<TrackingFila>> ObtenerAsync(Guid id, AlcanceCrm alcance, CancellationToken ct = default) =>
        await Visible(id, alcance, ct) is { } t ? t : NoExiste();

    public async Task<Resultado<Guid>> ActualizarAsync(Guid id, GuardarTrackingRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await Visible(id, alcance, ct) is not { } existente)
        {
            return NoExiste();
        }

        var factura = Limpiar(r.NFacturaInvoice);
        var awb = Limpiar(r.NoAwb);
        var aerolinea = Limpiar(r.Aerolinea);
        var notaCredito = Limpiar(r.NotaCreditoNo);
        var error = Primero(
            EnLista(r.Fase, FasesTracking.Todos, "Fase"),
            NoNegativo(r.CajasReales, "Cajas reales"),
            NoNegativo(r.MontoFobUsd, "Monto FOB"),
            NoNegativo(r.FleteAereoUsd, "Flete aéreo"),
            NoNegativo(r.DoaReportadoUsd, "DOA reportado"),
            r.DoaReportadoUsd > 0 && r.MontoFobUsd == 0
                ? Error.Validacion("Para registrar DOA, indica primero el monto FOB de los peces.")
                : null,
            r.FechaDespachoVuelo is { Year: < 2000 } ? Error.Validacion("La fecha de despacho no es válida.") : null,
            Opcional(factura, "N.º de factura / invoice", 50),
            Opcional(awb, "Guía AWB", 50),
            Opcional(aerolinea, "Aerolínea", 150),
            Opcional(notaCredito, "N.º de nota crédito", 50));
        if (error is not null)
        {
            return error;
        }

        var nuevo = new TrackingVuelo
        {
            Id = id,
            Codigo = existente.Codigo,
            VentaId = existente.VentaId,
            OportunidadId = existente.OportunidadId,
            ClienteId = existente.ClienteId,
            NFacturaInvoice = factura,
            NoAwb = awb,
            Aerolinea = aerolinea,
            FechaDespachoVuelo = r.FechaDespachoVuelo,
            CajasReales = r.CajasReales,
            Fase = r.Fase!,
            MontoFobUsd = r.MontoFobUsd,
            FleteAereoUsd = r.FleteAereoUsd,
            DoaReportadoUsd = r.DoaReportadoUsd,
            NotaCreditoNo = notaCredito,
            ActualizadoEn = reloj.AhoraUtc,
        };

        await tracking.ActualizarAsync(nuevo, ct);
        await auditoria.CambiosAsync(Entidad, id, ComparadorAuditoria.Comparar(Auditable(existente), Auditable(nuevo)), ct);
        return id;
    }

    /// <summary>Cambio rápido de fase (botón «Avanzar» de la torre de control). Admite corregir hacia atrás.</summary>
    public async Task<Resultado<Guid>> CambiarFaseAsync(Guid id, CambiarFaseRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await Visible(id, alcance, ct) is not { } existente)
        {
            return NoExiste();
        }

        if (EnLista(r.Fase, FasesTracking.Todos, "Fase") is { } error)
        {
            return error;
        }

        var esperada = r.FaseActual ?? existente.Fase;
        if (esperada != existente.Fase || !await tracking.CambiarFaseAsync(id, esperada, r.Fase!, reloj.AhoraUtc, ct))
        {
            return Error.Conflicto("Otra persona cambió la fase de este despacho. Recarga la página.");
        }

        await auditoria.CambiosAsync(Entidad, id, [new(nameof(TrackingAuditable.Fase), existente.Fase, r.Fase)], ct);
        return id;
    }

    public async Task<Resultado<IReadOnlyList<RegistroAuditoriaLectura>>> HistorialAsync(
        Guid id, AlcanceCrm alcance, CancellationToken ct = default) =>
        await Visible(id, alcance, ct) is null ? NoExiste() : Resultado.Ok(await auditoria.HistorialAsync(Entidad, id, ct));

    /// <summary>Misma fórmula que la columna generada, para mostrarla antes de guardar y en pruebas.</summary>
    public static decimal? PorcentajeDoa(decimal fobUsd, decimal doaUsd) =>
        fobUsd > 0 ? Math.Round(doaUsd / fobUsd * 100, 2, MidpointRounding.AwayFromZero) : null;

    private async Task<TrackingFila?> Visible(Guid id, AlcanceCrm alcance, CancellationToken ct) =>
        await tracking.ObtenerAsync(id, ct) is { } t && alcance.PuedeVer(t.ComercialId) ? t : null;

    private static TrackingAuditable Auditable(TrackingFila t) =>
        new(t.NFacturaInvoice, t.NoAwb, t.Aerolinea, t.FechaDespachoVuelo, t.CajasReales, t.Fase, t.MontoFobUsd,
            t.FleteAereoUsd, t.DoaReportadoUsd, t.NotaCreditoNo);

    private static TrackingAuditable Auditable(TrackingVuelo t) =>
        new(t.NFacturaInvoice, t.NoAwb, t.Aerolinea, t.FechaDespachoVuelo, t.CajasReales, t.Fase, t.MontoFobUsd,
            t.FleteAereoUsd, t.DoaReportadoUsd, t.NotaCreditoNo);

    private static Error NoExiste() => Error.NoEncontrado("El despacho no existe.");
}
