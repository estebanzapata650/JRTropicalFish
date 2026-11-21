using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Domain.Crm;
using static JRTropicalFish.Application.Crm.ValidacionCrm;

namespace JRTropicalFish.Application.Crm;

/// <summary>
/// Pipeline comercial. La etapa solo cambia por <see cref="CambiarEtapaAsync"/> (adelante o Perdida)
/// y por <see cref="GanarAsync"/>, que cierra la oportunidad y crea la venta (y el tracking si es
/// exportación) en una sola transacción.
/// </summary>
public class OportunidadService(
    IOportunidadRepository oportunidades,
    IClienteRepository clientes,
    IComercialRepository comerciales,
    IConsecutivoRepository consecutivos,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    public const string Entidad = "Oportunidad";
    public const string Prefijo = "OPT";

    /// <summary>Etapas en las que se puede registrar una oportunidad nueva.</summary>
    private static readonly string[] EtapasIniciales =
        [EtapasPipeline.Prospeccion, EtapasPipeline.Stocklist, EtapasPipeline.PreInvoice, EtapasPipeline.AprobacionVuelo];

    public Task<IReadOnlyList<OportunidadFila>> ListarAsync(AlcanceCrm alcance, Guid? clienteId = null, CancellationToken ct = default) =>
        oportunidades.ListarAsync(alcance.FiltroComercial, clienteId, ct);

    public async Task<Resultado<OportunidadFila>> ObtenerAsync(Guid id, AlcanceCrm alcance, CancellationToken ct = default) =>
        await Visible(id, alcance, ct) is { } o ? o : NoExiste();

    public Task<IReadOnlyList<ComercialInfo>> ComercialesAsync(CancellationToken ct = default) => comerciales.ListarAsync(ct);

    public async Task<Resultado<Creado>> CrearAsync(GuardarOportunidadRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        var etapa = r.Etapa ?? EtapasPipeline.Prospeccion;
        if (!EtapasIniciales.Contains(etapa))
        {
            return Error.Validacion("Una oportunidad nueva arranca en una etapa activa (no ganada ni perdida).");
        }

        var datos = await ValidarAsync(r, alcance, null, ct);
        if (!datos.Exitoso)
        {
            return datos.Error!;
        }

        var (cliente, comercial, fechaContacto, notas) = datos.Valor;
        var ahora = reloj.AhoraUtc;
        var oportunidad = new Oportunidad
        {
            Codigo = await consecutivos.SiguienteAsync(Prefijo, ct),
            ClienteId = cliente.Id,
            TipoVenta = r.TipoVenta!,
            ComercialId = comercial.Id,
            FechaContactoInicial = fechaContacto,
            Etapa = etapa,
            Moneda = r.Moneda!,
            MontoEstimado = r.MontoEstimado,
            CajasEstimadas = r.CajasEstimadas,
            NotasSeguimiento = notas,
            EtapaCambiadaEn = ahora,
            CreadoEn = ahora,
        };

        await oportunidades.CrearAsync(oportunidad, ct);
        await auditoria.CreacionAsync(Entidad, oportunidad.Id, Auditable(oportunidad, cliente.NombreEmpresa, comercial.Nombre), ct: ct);
        return new Creado(oportunidad.Id, oportunidad.Codigo);
    }

    /// <summary>
    /// Edita los datos comerciales. La etapa no se toca aquí. Una oportunidad cerrada (ganada o
    /// perdida) solo admite cambiar las notas de seguimiento.
    /// </summary>
    public async Task<Resultado<Guid>> ActualizarAsync(
        Guid id, GuardarOportunidadRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await Visible(id, alcance, ct) is not { } existente)
        {
            return NoExiste();
        }

        Oportunidad oportunidad;
        if (EtapasPipeline.EsTerminal(existente.Etapa))
        {
            var notas = LimpiarTextoLargo(r.NotasSeguimiento);
            if (Opcional(notas, "Notas de seguimiento", 4000) is { } errorNotas)
            {
                return errorNotas;
            }

            oportunidad = DesdeFila(existente);
            oportunidad.NotasSeguimiento = notas;
        }
        else
        {
            var datos = await ValidarAsync(r, alcance, existente, ct);
            if (!datos.Exitoso)
            {
                return datos.Error!;
            }

            var (cliente, comercial, fechaContacto, notas) = datos.Valor;
            oportunidad = DesdeFila(existente);
            oportunidad.ClienteId = cliente.Id;
            oportunidad.TipoVenta = r.TipoVenta!;
            oportunidad.ComercialId = comercial.Id;
            oportunidad.FechaContactoInicial = fechaContacto;
            oportunidad.Moneda = r.Moneda!;
            oportunidad.MontoEstimado = r.MontoEstimado;
            oportunidad.CajasEstimadas = r.CajasEstimadas;
            oportunidad.NotasSeguimiento = notas;
        }

        oportunidad.ActualizadoEn = reloj.AhoraUtc;
        await oportunidades.ActualizarAsync(oportunidad, ct);

        var nombreCliente = oportunidad.ClienteId == existente.ClienteId
            ? existente.ClienteNombre
            : (await clientes.ObtenerAsync(oportunidad.ClienteId, ct))?.NombreEmpresa ?? "";
        var nombreComercial = oportunidad.ComercialId == existente.ComercialId
            ? existente.ComercialNombre
            : (await comerciales.ListarAsync(ct)).FirstOrDefault(c => c.Id == oportunidad.ComercialId)?.Nombre ?? "";
        await auditoria.CambiosAsync(
            Entidad, id,
            ComparadorAuditoria.Comparar(Auditable(existente), Auditable(oportunidad, nombreCliente, nombreComercial)),
            ct);
        return id;
    }

    public async Task<Resultado<Guid>> CambiarEtapaAsync(Guid id, CambiarEtapaRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await Visible(id, alcance, ct) is not { } existente)
        {
            return NoExiste();
        }

        if (r.EtapaActual is not null && r.EtapaActual != existente.Etapa)
        {
            return CambioConcurrente();
        }

        if (ReglasPipeline.ValidarCambio(existente.Etapa, r.Etapa, r.MotivoPerdida) is { } error)
        {
            return error;
        }

        var motivo = r.Etapa == EtapasPipeline.Perdida ? Limpiar(r.MotivoPerdida) : null;
        if (Opcional(motivo, "Motivo de pérdida", 500) is { } errorMotivo)
        {
            return errorMotivo;
        }

        if (!await oportunidades.CambiarEtapaAsync(id, existente.Etapa, r.Etapa!, motivo, reloj.AhoraUtc, ct))
        {
            return CambioConcurrente();
        }

        List<CambioAuditado> cambios = [new(nameof(OportunidadAuditable.Etapa), existente.Etapa, r.Etapa)];
        if (motivo is not null)
        {
            cambios.Add(new(nameof(OportunidadAuditable.MotivoPerdida), existente.MotivoPerdida, motivo));
        }

        await auditoria.CambiosAsync(Entidad, id, cambios, ct);
        return id;
    }

    /// <summary>
    /// Cierra la oportunidad como ganada, con la fecha de pago confirmado, y crea la venta. Si es
    /// exportación crea también el tracking del vuelo en fase Cuarentena. Todo o nada.
    /// </summary>
    public async Task<Resultado<OportunidadGanada>> GanarAsync(
        Guid id, GanarOportunidadRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await Visible(id, alcance, ct) is not { } o)
        {
            return NoExiste();
        }

        if (r.EtapaActual is not null && r.EtapaActual != o.Etapa)
        {
            return CambioConcurrente();
        }

        var fechaPago = r.FechaPagoConfirmado ?? reloj.Hoy;
        var estadoPago = r.EstadoPago ?? EstadosPago.Anticipo;
        var trm = o.Moneda == Monedas.Usd ? r.Trm : null;
        var error = Primero(
            ReglasPipeline.ValidarGanar(o.Etapa),
            fechaPago > reloj.Hoy ? Error.Validacion("La fecha de pago confirmado no puede ser futura.") : null,
            EnLista(r.OrigenCanal, OrigenesCanal.Todos, "Origen / canal"),
            EnLista(estadoPago, EstadosPago.Todos, "Estado de pago"),
            o.Moneda == Monedas.Usd ? ValidarTrm(trm) : null);
        if (error is not null)
        {
            return error;
        }

        var venta = new Creado(Guid.CreateVersion7(), await consecutivos.SiguienteAsync(VentaService.Prefijo, ct));
        Creado? tracking = o.TipoVenta == TiposVenta.Exportacion
            ? new Creado(Guid.CreateVersion7(), await consecutivos.SiguienteAsync(TrackingService.Prefijo, ct))
            : null;

        if (!await oportunidades.GanarAsync(id, o.Etapa, fechaPago, venta, r.OrigenCanal!, trm, estadoPago, tracking, reloj.AhoraUtc, ct))
        {
            return CambioConcurrente();
        }

        await auditoria.CambiosAsync(
            Entidad, id,
            [
                new(nameof(OportunidadAuditable.Etapa), o.Etapa, EtapasPipeline.Ganada),
                new(nameof(OportunidadAuditable.FechaPagoConfirmado), null, fechaPago.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)),
            ],
            ct);
        await auditoria.CreacionAsync(
            VentaService.Entidad, venta.Id,
            new VentaAuditable(
                o.ClienteNombre, o.ComercialNombre, r.OrigenCanal!, fechaPago, o.TipoVenta, o.Moneda, o.MontoEstimado, trm,
                o.CajasEstimadas, null, null, estadoPago, EstadosPreparacion.PendientePicking, null, null, null),
            [new CambioAuditado("Origen", null, $"Oportunidad {o.Codigo} ganada")],
            ct);
        if (tracking is not null)
        {
            await auditoria.CreacionAsync(
                TrackingService.Entidad, tracking.Id,
                new TrackingAuditable(
                    null, null, null, null, o.CajasEstimadas, FasesTracking.Cuarentena,
                    o.Moneda == Monedas.Usd ? o.MontoEstimado : 0, 0, 0, null),
                [new CambioAuditado("Origen", null, $"Venta {venta.Codigo}")],
                ct);
        }

        return new OportunidadGanada(venta.Id, venta.Codigo, tracking?.Id, tracking?.Codigo);
    }

    /// <summary>Borrado lógico. Una oportunidad ganada no se elimina: ya tiene una venta.</summary>
    public async Task<Resultado> EliminarAsync(Guid id, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await Visible(id, alcance, ct) is not { } existente)
        {
            return NoExiste();
        }

        if (existente.Etapa == EtapasPipeline.Ganada)
        {
            return Error.Conflicto("Una oportunidad ganada no se puede eliminar: ya tiene una venta.");
        }

        await oportunidades.EliminarAsync(id, reloj.AhoraUtc, ct);
        await auditoria.EliminacionAsync(Entidad, id, Auditable(existente), ct);
        return Resultado.Ok();
    }

    public async Task<Resultado<IReadOnlyList<RegistroAuditoriaLectura>>> HistorialAsync(
        Guid id, AlcanceCrm alcance, CancellationToken ct = default) =>
        await Visible(id, alcance, ct) is null ? NoExiste() : Resultado.Ok(await auditoria.HistorialAsync(Entidad, id, ct));

    /// <summary>TRM razonable para COP por USD; atrapa errores de digitación (ej. 4.1 en vez de 4100).</summary>
    public static Error? ValidarTrm(decimal? trm) =>
        trm is null or <= 0
            ? Error.Validacion("Las ventas en USD necesitan la TRM del día (pesos por dólar).")
            : trm is < 1000 or > 20000
                ? Error.Validacion("La TRM debe estar entre 1.000 y 20.000 pesos por dólar.")
                : null;

    private async Task<OportunidadFila?> Visible(Guid id, AlcanceCrm alcance, CancellationToken ct) =>
        await oportunidades.ObtenerAsync(id, ct) is { } o && alcance.PuedeVer(o.ComercialId) ? o : null;

    private async Task<Resultado<(ClienteFila Cliente, ComercialInfo Comercial, DateOnly FechaContacto, string? Notas)>> ValidarAsync(
        GuardarOportunidadRequest r, AlcanceCrm alcance, OportunidadFila? existente, CancellationToken ct)
    {
        var fechaContacto = r.FechaContactoInicial ?? existente?.FechaContactoInicial ?? reloj.Hoy;
        var notas = LimpiarTextoLargo(r.NotasSeguimiento);
        var error = Primero(
            EnLista(r.TipoVenta, TiposVenta.Todos, "Tipo de venta"),
            EnLista(r.Moneda, Monedas.Todos, "Moneda"),
            NoNegativo(r.MontoEstimado, "Monto estimado"),
            NoNegativo(r.CajasEstimadas, "Cajas estimadas"),
            fechaContacto > reloj.Hoy ? Error.Validacion("La fecha de contacto inicial no puede ser futura.") : null,
            Opcional(notas, "Notas de seguimiento", 4000));
        if (error is not null)
        {
            return error;
        }

        if (await clientes.ObtenerAsync(r.ClienteId, ct) is not { } cliente)
        {
            return Error.Validacion("Elige un cliente existente.");
        }

        var comercial = await ComercialesCrm.ResolverAsync(
            comerciales, alcance, r.ComercialId,
            existente is null ? null : (existente.ComercialId, existente.ComercialNombre), ct);
        if (!comercial.Exitoso)
        {
            return comercial.Error!;
        }

        return (cliente, comercial.Valor, fechaContacto, notas);
    }

    private static Oportunidad DesdeFila(OportunidadFila o) => new()
    {
        Id = o.Id,
        Codigo = o.Codigo,
        ClienteId = o.ClienteId,
        TipoVenta = o.TipoVenta,
        ComercialId = o.ComercialId,
        FechaContactoInicial = o.FechaContactoInicial,
        Etapa = o.Etapa,
        Moneda = o.Moneda,
        MontoEstimado = o.MontoEstimado,
        CajasEstimadas = o.CajasEstimadas,
        FechaPagoConfirmado = o.FechaPagoConfirmado,
        MotivoPerdida = o.MotivoPerdida,
        NotasSeguimiento = o.NotasSeguimiento,
        EtapaCambiadaEn = o.EtapaCambiadaEn,
        CreadoEn = o.CreadoEn,
    };

    private static OportunidadAuditable Auditable(Oportunidad o, string cliente, string comercial) =>
        new(cliente, o.TipoVenta, comercial, o.FechaContactoInicial, o.Etapa, o.Moneda, o.MontoEstimado,
            o.CajasEstimadas, o.FechaPagoConfirmado, o.MotivoPerdida, o.NotasSeguimiento);

    private static OportunidadAuditable Auditable(OportunidadFila o) =>
        new(o.ClienteNombre, o.TipoVenta, o.ComercialNombre, o.FechaContactoInicial, o.Etapa, o.Moneda, o.MontoEstimado,
            o.CajasEstimadas, o.FechaPagoConfirmado, o.MotivoPerdida, o.NotasSeguimiento);

    private static Error NoExiste() => Error.NoEncontrado("La oportunidad no existe.");

    private static Error CambioConcurrente() =>
        Error.Conflicto("Otra persona cambió esta oportunidad mientras tanto. Recarga la página y vuelve a intentarlo.");
}
