using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Domain.Crm;
using static JRTropicalFish.Application.Crm.ValidacionCrm;

namespace JRTropicalFish.Application.Crm;

/// <summary>
/// Ventas unificadas (nacional y exportación). Nacen al ganar una oportunidad o como venta directa
/// (mostrador, web). Una venta de exportación siempre tiene su tracking de vuelo.
///
/// <para>
/// Campos según el tipo (el web los muestra u oculta con la misma regla): exportación lleva guía AWB
/// y no dirección de entrega; nacional lleva dirección y no guía. El servicio limpia lo que no aplica.
/// </para>
/// </summary>
public class VentaService(
    IVentaRepository ventas,
    IClienteRepository clientes,
    IComercialRepository comerciales,
    IConsecutivoRepository consecutivos,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    public const string Entidad = "Venta";
    public const string Prefijo = "VTA";

    public Task<IReadOnlyList<VentaFila>> ListarAsync(
        AlcanceCrm alcance, Guid? clienteId = null, DateOnly? desde = null, DateOnly? hasta = null, CancellationToken ct = default) =>
        ventas.ListarAsync(alcance.FiltroComercial, clienteId, desde, hasta, ct);

    public async Task<Resultado<VentaFila>> ObtenerAsync(Guid id, AlcanceCrm alcance, CancellationToken ct = default) =>
        await Visible(id, alcance, ct) is { } v ? v : NoExiste();

    /// <summary>Venta directa, sin oportunidad. Si es exportación nace también su tracking en Cuarentena.</summary>
    public async Task<Resultado<Creado>> CrearAsync(GuardarVentaRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (EnLista(r.TipoVenta, TiposVenta.Todos, "Tipo de venta") is { } errorTipo)
        {
            return errorTipo;
        }

        if (r.ClienteId is null || await clientes.ObtenerAsync(r.ClienteId.Value, ct) is not { } cliente)
        {
            return Error.Validacion("Elige un cliente existente.");
        }

        var comercial = await ComercialesCrm.ResolverAsync(comerciales, alcance, r.ComercialId, null, ct);
        if (!comercial.Exitoso)
        {
            return comercial.Error!;
        }

        var venta = new Venta
        {
            Codigo = "",
            ClienteId = cliente.Id,
            ComercialId = comercial.Valor.Id,
            TipoVenta = r.TipoVenta!,
            OrigenCanal = "",
            Moneda = "",
            EstadoPago = "",
            EstadoPreparacion = "",
        };
        if (Aplicar(venta, r) is { } error)
        {
            return error;
        }

        venta.Codigo = await consecutivos.SiguienteAsync(Prefijo, ct);
        venta.CreadoEn = reloj.AhoraUtc;
        Creado? tracking = venta.TipoVenta == TiposVenta.Exportacion
            ? new Creado(Guid.CreateVersion7(), await consecutivos.SiguienteAsync(TrackingService.Prefijo, ct))
            : null;

        await ventas.CrearAsync(venta, tracking, ct);
        await auditoria.CreacionAsync(Entidad, venta.Id, Auditable(venta, cliente.NombreEmpresa, comercial.Valor.Nombre), ct: ct);
        if (tracking is not null)
        {
            await auditoria.CreacionAsync(
                TrackingService.Entidad, tracking.Id,
                new TrackingAuditable(
                    null, venta.NoGuiaAwb, venta.TransportadoraAerolinea, null, venta.TotalCajas, FasesTracking.Cuarentena,
                    venta.Moneda == Monedas.Usd ? venta.Total : 0, 0, 0, null),
                [new CambioAuditado("Origen", null, $"Venta {venta.Codigo}")],
                ct);
        }

        return new Creado(venta.Id, venta.Codigo);
    }

    /// <summary>Cliente, comercial y tipo de venta no cambian: de ellos dependen la oportunidad y el tracking.</summary>
    public async Task<Resultado<Guid>> ActualizarAsync(Guid id, GuardarVentaRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await Visible(id, alcance, ct) is not { } existente)
        {
            return NoExiste();
        }

        var venta = DesdeFila(existente);
        if (Aplicar(venta, r) is { } error)
        {
            return error;
        }

        venta.ActualizadoEn = reloj.AhoraUtc;
        await ventas.ActualizarAsync(venta, ct);
        await auditoria.CambiosAsync(
            Entidad, id,
            ComparadorAuditoria.Comparar(
                Auditable(DesdeFila(existente), existente.ClienteNombre, existente.ComercialNombre),
                Auditable(venta, existente.ClienteNombre, existente.ComercialNombre)),
            ct);
        return id;
    }

    public async Task<Resultado<IReadOnlyList<RegistroAuditoriaLectura>>> HistorialAsync(
        Guid id, AlcanceCrm alcance, CancellationToken ct = default) =>
        await Visible(id, alcance, ct) is null ? NoExiste() : Resultado.Ok(await auditoria.HistorialAsync(Entidad, id, ct));

    /// <summary>Valida y copia los campos editables según el tipo de venta (que ya está en <paramref name="v"/>).</summary>
    private Error? Aplicar(Venta v, GuardarVentaRequest r)
    {
        var exportacion = v.TipoVenta == TiposVenta.Exportacion;
        var transportadora = Limpiar(r.TransportadoraAerolinea);
        var awb = exportacion ? Limpiar(r.NoGuiaAwb) : null;
        var ciudad = Limpiar(r.CiudadEntrega);
        var direccion = exportacion ? null : Limpiar(r.DireccionEntrega);
        var notas = LimpiarTextoLargo(r.Notas);
        var fechaPedido = r.FechaPedido ?? reloj.Hoy;
        var estadoPago = r.EstadoPago ?? EstadosPago.Pendiente;
        var estadoPreparacion = r.EstadoPreparacion ?? EstadosPreparacion.PendientePicking;
        var trm = r.Moneda == Monedas.Usd ? r.Trm : null;

        var error = Primero(
            EnLista(r.OrigenCanal, OrigenesCanal.Todos, "Origen / canal"),
            EnLista(r.Moneda, Monedas.Todos, "Moneda"),
            EnLista(estadoPago, EstadosPago.Todos, "Estado de pago"),
            EnLista(estadoPreparacion, EstadosPreparacion.Todos, "Estado de preparación"),
            NoNegativo(r.Total, "Total"),
            NoNegativo(r.TotalCajas, "Total de cajas"),
            r.Moneda == Monedas.Usd ? OportunidadService.ValidarTrm(trm) : null,
            fechaPedido.Year < 2000 ? Error.Validacion("La fecha del pedido no es válida.") : null,
            Opcional(transportadora, "Transportadora / aerolínea", 150),
            Opcional(awb, "Guía AWB", 50),
            Opcional(ciudad, "Ciudad de entrega", 100),
            Opcional(direccion, "Dirección de entrega", 250),
            Opcional(notas, "Notas", 2000));
        if (error is not null)
        {
            return error;
        }

        v.OrigenCanal = r.OrigenCanal!;
        v.FechaPedido = fechaPedido;
        v.Moneda = r.Moneda!;
        v.Total = r.Total;
        v.Trm = trm;
        v.TotalCajas = r.TotalCajas;
        v.TransportadoraAerolinea = transportadora;
        v.NoGuiaAwb = awb;
        v.EstadoPago = estadoPago;
        v.EstadoPreparacion = estadoPreparacion;
        v.CiudadEntrega = ciudad;
        v.DireccionEntrega = direccion;
        v.Notas = notas;
        return null;
    }

    private async Task<VentaFila?> Visible(Guid id, AlcanceCrm alcance, CancellationToken ct) =>
        await ventas.ObtenerAsync(id, ct) is { } v && alcance.PuedeVer(v.ComercialId) ? v : null;

    private static Venta DesdeFila(VentaFila v) => new()
    {
        Id = v.Id,
        Codigo = v.Codigo,
        OportunidadId = v.OportunidadId,
        ClienteId = v.ClienteId,
        ComercialId = v.ComercialId,
        OrigenCanal = v.OrigenCanal,
        FechaPedido = v.FechaPedido,
        TipoVenta = v.TipoVenta,
        Moneda = v.Moneda,
        Total = v.Total,
        Trm = v.Trm,
        TotalCop = v.TotalCop,
        TotalCajas = v.TotalCajas,
        TransportadoraAerolinea = v.TransportadoraAerolinea,
        NoGuiaAwb = v.NoGuiaAwb,
        EstadoPago = v.EstadoPago,
        EstadoPreparacion = v.EstadoPreparacion,
        CiudadEntrega = v.CiudadEntrega,
        DireccionEntrega = v.DireccionEntrega,
        Notas = v.Notas,
        CreadoEn = v.CreadoEn,
    };

    private static VentaAuditable Auditable(Venta v, string cliente, string comercial) =>
        new(cliente, comercial, v.OrigenCanal, v.FechaPedido, v.TipoVenta, v.Moneda, v.Total, v.Trm, v.TotalCajas,
            v.TransportadoraAerolinea, v.NoGuiaAwb, v.EstadoPago, v.EstadoPreparacion, v.CiudadEntrega, v.DireccionEntrega,
            v.Notas);

    private static Error NoExiste() => Error.NoEncontrado("La venta no existe.");
}
