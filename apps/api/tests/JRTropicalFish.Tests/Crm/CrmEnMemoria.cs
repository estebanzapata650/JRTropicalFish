using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Crm;
using JRTropicalFish.Domain.Auditoria;
using JRTropicalFish.Domain.Crm;

namespace JRTropicalFish.Tests.Crm;

/// <summary>
/// Repositorios del CRM en memoria con las mismas reglas que las funciones PL/pgSQL: consecutivos,
/// "comparar y escribir" en etapas/fases/estados, venta y tracking al ganar, columnas generadas
/// (<c>total_cop</c>, <c>porcentaje_doa</c>) y días restantes contra <see cref="Hoy"/>.
/// </summary>
public sealed class CrmEnMemoria :
    IConsecutivoRepository, IComercialRepository, IClienteRepository, IOportunidadRepository, IVentaRepository,
    ITrackingRepository, ITareaRepository, IAuditoriaRepository, IReloj, IUsuarioActual
{
    public DateTime Ahora { get; set; } = new(2026, 11, 20, 15, 0, 0, DateTimeKind.Utc);

    public Dictionary<string, int> Consecutivos { get; } = new() { ["CLI"] = 0, ["OPT"] = 100, ["VTA"] = 800, ["TRK"] = 500, ["TAR"] = 0 };
    public List<ComercialInfo> Comerciales { get; } = [];
    public Dictionary<Guid, Cliente> Clientes { get; } = [];
    public Dictionary<Guid, Oportunidad> Oportunidades { get; } = [];
    public Dictionary<Guid, Venta> Ventas { get; } = [];
    public Dictionary<Guid, TrackingVuelo> Tracking { get; } = [];
    public Dictionary<Guid, Tarea> Tareas { get; } = [];
    public List<RegistroAuditoria> Auditoria { get; } = [];

    public ComercialInfo Ana { get; }
    public ComercialInfo Beto { get; }
    public Cliente Cliente { get; }

    public CrmEnMemoria()
    {
        Ana = new ComercialInfo(Guid.CreateVersion7(), "Ana Vendedora", "ana@jrtf.local");
        Beto = new ComercialInfo(Guid.CreateVersion7(), "Beto Vendedor", "beto@jrtf.local");
        Comerciales.AddRange([Ana, Beto]);
        Cliente = AgregarCliente("Aquafarm International B.V.");
    }

    /// <summary>Ana como comercial que solo ve lo suyo.</summary>
    public AlcanceCrm ComoAna => new(Ana.Id, EsComercial: true, VerTodo: false);

    public AlcanceCrm ComoBeto => new(Beto.Id, EsComercial: true, VerTodo: false);

    /// <summary>Gerencia: ve todo, no es comercial.</summary>
    public AlcanceCrm ComoGerencia => new(Guid.CreateVersion7(), EsComercial: false, VerTodo: true);

    public Cliente AgregarCliente(string nombre)
    {
        var cliente = new Cliente
        {
            Codigo = $"CLI-{Clientes.Count + 1:000}", NombreEmpresa = nombre, Pais = "Países Bajos",
            TipoCliente = TiposCliente.B2BInternacional, LicenciaImportacion = LicenciasImportacion.Activa,
            Estado = EstadosCliente.Activo, CreadoEn = Ahora,
        };
        Clientes[cliente.Id] = cliente;
        return cliente;
    }

    private ServicioAuditoria Auditar() => new(this, this, this);

    public ClienteService ClienteService() => new(this, this, this, Auditar());

    public OportunidadService OportunidadService() => new(this, this, this, this, this, Auditar());

    public VentaService VentaService() => new(this, this, this, this, this, Auditar());

    public TrackingService TrackingService() => new(this, this, Auditar());

    public TareaService TareaService() => new(this, this, this, this, this, this, Auditar());

    public GuardarOportunidadRequest Oportunidad(string tipo = TiposVenta.Exportacion, string moneda = Monedas.Usd) =>
        new(Cliente.Id, tipo, null, null, null, moneda, 4850m, 12, null);

    // --- IReloj / IUsuarioActual ---
    public DateTime AhoraUtc => Ahora;
    public DateOnly Hoy => DateOnly.FromDateTime(Ahora);
    public Guid? UsuarioId => null;
    public string? Nombre => "Pruebas";

    // --- Auditoría ---
    public Task RegistrarAsync(RegistroAuditoria registro, CancellationToken ct = default)
    {
        Auditoria.Add(registro);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RegistroAuditoriaLectura>> ListarPorEntidadAsync(
        string entidad, Guid entidadId, int limite, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<RegistroAuditoriaLectura>>(
            [.. Auditoria.Where(r => r.Entidad == entidad && r.EntidadId == entidadId)
                .Select(r => new RegistroAuditoriaLectura(r.Id, r.Entidad, r.EntidadId, r.Accion.ToString(), r.CambiosJson,
                    r.UsuarioId, r.UsuarioNombre, r.FechaUtc))]);

    // --- Consecutivos / comerciales ---
    public Task<string> SiguienteAsync(string prefijo, CancellationToken ct = default)
    {
        Consecutivos[prefijo] = Consecutivos.GetValueOrDefault(prefijo) + 1;
        return Task.FromResult($"{prefijo}-{Consecutivos[prefijo]:000}");
    }

    public Task AjustarAsync(string prefijo, int numero, CancellationToken ct = default)
    {
        Consecutivos[prefijo] = Math.Max(Consecutivos.GetValueOrDefault(prefijo), numero);
        return Task.CompletedTask;
    }

    Task<IReadOnlyList<ComercialInfo>> IComercialRepository.ListarAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ComercialInfo>>(Comerciales);

    private string NombreComercial(Guid id) => Comerciales.FirstOrDefault(c => c.Id == id)?.Nombre ?? "?";

    // --- Clientes ---
    private static ClienteFila Fila(Cliente c) =>
        new(c.Id, c.Codigo, c.NombreEmpresa, c.ContactoPrincipal, c.Email, c.TelefonoWhatsapp, c.Pais, c.Ciudad,
            c.AeropuertoIata, c.TipoCliente, c.LicenciaImportacion, c.Estado, c.Notas, c.CreadoEn);

    Task<IReadOnlyList<ClienteFila>> IClienteRepository.ListarAsync(string? busqueda, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ClienteFila>>([.. Clientes.Values.Where(c => c.EliminadoEn is null).Select(Fila)]);

    Task<ClienteFila?> IClienteRepository.ObtenerAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Clientes.TryGetValue(id, out var c) && c.EliminadoEn is null ? Fila(c) : null);

    public Task<bool> ExisteCodigoAsync(string codigo, Guid? excluirId, CancellationToken ct = default) =>
        Task.FromResult(Clientes.Values.Any(c => c.EliminadoEn is null && c.Id != excluirId
                                                 && string.Equals(c.Codigo, codigo, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId, CancellationToken ct = default) =>
        Task.FromResult(Clientes.Values.Any(c => c.EliminadoEn is null && c.Id != excluirId
                                                 && string.Equals(c.NombreEmpresa, nombre, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> TieneMovimientosAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Oportunidades.Values.Any(o => o.ClienteId == id && o.EliminadoEn is null)
                        || Ventas.Values.Any(v => v.ClienteId == id)
                        || Tareas.Values.Any(t => t.ClienteId == id && t.EliminadoEn is null));

    public Task CrearAsync(Cliente cliente, CancellationToken ct = default)
    {
        cliente.Email = cliente.Email?.ToLowerInvariant();
        Clientes[cliente.Id] = cliente;
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(Cliente cliente, CancellationToken ct = default) => CrearAsync(cliente, ct);

    Task IClienteRepository.EliminarAsync(Guid id, DateTime fecha, CancellationToken ct)
    {
        Clientes[id].EliminadoEn = fecha;
        return Task.CompletedTask;
    }

    // --- Oportunidades ---
    private OportunidadFila Fila(Oportunidad o)
    {
        var c = Clientes[o.ClienteId];
        return new(o.Id, o.Codigo, o.ClienteId, c.Codigo, c.NombreEmpresa, o.TipoVenta, o.ComercialId, NombreComercial(o.ComercialId),
            o.FechaContactoInicial, o.Etapa, o.Moneda, o.MontoEstimado, o.CajasEstimadas, o.FechaPagoConfirmado,
            o.MotivoPerdida, o.NotasSeguimiento, o.EtapaCambiadaEn, Ventas.Values.FirstOrDefault(v => v.OportunidadId == o.Id)?.Id,
            o.CreadoEn);
    }

    Task<IReadOnlyList<OportunidadFila>> IOportunidadRepository.ListarAsync(Guid? comercialId, Guid? clienteId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<OportunidadFila>>(
            [.. Oportunidades.Values.Where(o => o.EliminadoEn is null && (comercialId is null || o.ComercialId == comercialId)
                                                && (clienteId is null || o.ClienteId == clienteId)).Select(Fila)]);

    Task<OportunidadFila?> IOportunidadRepository.ObtenerAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Oportunidades.TryGetValue(id, out var o) && o.EliminadoEn is null ? Fila(o) : null);

    public Task CrearAsync(Oportunidad oportunidad, CancellationToken ct = default)
    {
        Oportunidades[oportunidad.Id] = oportunidad;
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(Oportunidad oportunidad, CancellationToken ct = default)
    {
        // Como sp_oportunidad_actualizar: la etapa no se toca.
        oportunidad.Etapa = Oportunidades[oportunidad.Id].Etapa;
        Oportunidades[oportunidad.Id] = oportunidad;
        return Task.CompletedTask;
    }

    public Task<bool> CambiarEtapaAsync(
        Guid id, string etapaEsperada, string etapa, string? motivoPerdida, DateTime fecha, CancellationToken ct = default)
    {
        var o = Oportunidades[id];
        if (o.Etapa != etapaEsperada)
        {
            return Task.FromResult(false);
        }

        o.Etapa = etapa;
        o.MotivoPerdida = etapa == EtapasPipeline.Perdida ? motivoPerdida : null;
        o.EtapaCambiadaEn = fecha;
        return Task.FromResult(true);
    }

    public Task<bool> GanarAsync(
        Guid id, string etapaEsperada, DateOnly fechaPagoConfirmado, Creado venta, string origenCanal, decimal? trm,
        string estadoPago, Creado? tracking, DateTime fecha, CancellationToken ct = default)
    {
        var o = Oportunidades[id];
        if (o.Etapa != etapaEsperada)
        {
            return Task.FromResult(false);
        }

        o.Etapa = EtapasPipeline.Ganada;
        o.FechaPagoConfirmado = fechaPagoConfirmado;
        var trmVenta = o.Moneda == Monedas.Usd ? trm : null;
        Ventas[venta.Id] = new Venta
        {
            Id = venta.Id, Codigo = venta.Codigo, OportunidadId = o.Id, ClienteId = o.ClienteId, ComercialId = o.ComercialId,
            OrigenCanal = origenCanal, FechaPedido = fechaPagoConfirmado, TipoVenta = o.TipoVenta, Moneda = o.Moneda,
            Total = o.MontoEstimado, Trm = trmVenta, TotalCop = o.MontoEstimado * (trmVenta ?? 1), TotalCajas = o.CajasEstimadas,
            EstadoPago = estadoPago, EstadoPreparacion = EstadosPreparacion.PendientePicking, CreadoEn = fecha,
        };
        if (tracking is not null)
        {
            Tracking[tracking.Id] = new TrackingVuelo
            {
                Id = tracking.Id, Codigo = tracking.Codigo, VentaId = venta.Id, OportunidadId = o.Id, ClienteId = o.ClienteId,
                CajasReales = o.CajasEstimadas, Fase = FasesTracking.Cuarentena,
                MontoFobUsd = o.Moneda == Monedas.Usd ? o.MontoEstimado : 0, CreadoEn = fecha,
            };
        }

        return Task.FromResult(true);
    }

    Task IOportunidadRepository.EliminarAsync(Guid id, DateTime fecha, CancellationToken ct)
    {
        Oportunidades[id].EliminadoEn = fecha;
        return Task.CompletedTask;
    }

    // --- Ventas ---
    private VentaFila Fila(Venta v)
    {
        var c = Clientes[v.ClienteId];
        return new(v.Id, v.Codigo, v.OportunidadId, v.OportunidadId is { } oid ? Oportunidades[oid].Codigo : null, v.ClienteId,
            c.Codigo, c.NombreEmpresa, v.ComercialId, NombreComercial(v.ComercialId), v.OrigenCanal, v.FechaPedido, v.TipoVenta,
            v.Moneda, v.Total, v.Trm, v.Total * (v.Trm ?? 1), v.TotalCajas, v.TransportadoraAerolinea, v.NoGuiaAwb, v.EstadoPago,
            v.EstadoPreparacion, v.CiudadEntrega, v.DireccionEntrega, v.Notas,
            Tracking.Values.FirstOrDefault(t => t.VentaId == v.Id)?.Id, v.CreadoEn);
    }

    Task<IReadOnlyList<VentaFila>> IVentaRepository.ListarAsync(
        Guid? comercialId, Guid? clienteId, DateOnly? desde, DateOnly? hasta, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<VentaFila>>(
            [.. Ventas.Values.Where(v => (comercialId is null || v.ComercialId == comercialId)
                                         && (clienteId is null || v.ClienteId == clienteId)).Select(Fila)]);

    Task<VentaFila?> IVentaRepository.ObtenerAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Ventas.TryGetValue(id, out var v) ? Fila(v) : null);

    public Task CrearAsync(Venta venta, Creado? tracking, CancellationToken ct = default)
    {
        Ventas[venta.Id] = venta;
        if (tracking is not null)
        {
            Tracking[tracking.Id] = new TrackingVuelo
            {
                Id = tracking.Id, Codigo = tracking.Codigo, VentaId = venta.Id, ClienteId = venta.ClienteId,
                CajasReales = venta.TotalCajas, Fase = FasesTracking.Cuarentena,
                MontoFobUsd = venta.Moneda == Monedas.Usd ? venta.Total : 0, CreadoEn = venta.CreadoEn,
            };
        }

        return Task.CompletedTask;
    }

    public Task ActualizarAsync(Venta venta, CancellationToken ct = default)
    {
        Ventas[venta.Id] = venta;
        return Task.CompletedTask;
    }

    // --- Tracking ---
    private TrackingFila Fila(TrackingVuelo t)
    {
        var c = Clientes[t.ClienteId];
        var v = Ventas[t.VentaId];
        return new(t.Id, t.Codigo, t.VentaId, v.Codigo, t.OportunidadId, null, t.ClienteId, c.Codigo, c.NombreEmpresa,
            v.ComercialId, NombreComercial(v.ComercialId), t.NFacturaInvoice, t.NoAwb, t.Aerolinea, t.FechaDespachoVuelo,
            t.CajasReales, t.Fase, t.MontoFobUsd, t.FleteAereoUsd, t.DoaReportadoUsd,
            Application.Crm.TrackingService.PorcentajeDoa(t.MontoFobUsd, t.DoaReportadoUsd), t.NotaCreditoNo, t.CreadoEn);
    }

    Task<IReadOnlyList<TrackingFila>> ITrackingRepository.ListarAsync(Guid? comercialId, Guid? clienteId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<TrackingFila>>(
            [.. Tracking.Values.Select(Fila).Where(t => (comercialId is null || t.ComercialId == comercialId)
                                                        && (clienteId is null || t.ClienteId == clienteId))]);

    Task<TrackingFila?> ITrackingRepository.ObtenerAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Tracking.TryGetValue(id, out var t) ? Fila(t) : null);

    public Task ActualizarAsync(TrackingVuelo tracking, CancellationToken ct = default)
    {
        Tracking[tracking.Id] = tracking;
        return Task.CompletedTask;
    }

    public Task<bool> CambiarFaseAsync(Guid id, string faseEsperada, string fase, DateTime fecha, CancellationToken ct = default)
    {
        if (Tracking[id].Fase != faseEsperada)
        {
            return Task.FromResult(false);
        }

        Tracking[id].Fase = fase;
        return Task.FromResult(true);
    }

    // --- Tareas ---
    private TareaFila Fila(Tarea t, DateOnly hoy)
    {
        var c = Clientes[t.ClienteId];
        return new(t.Id, t.Codigo, t.ClienteId, c.Codigo, c.NombreEmpresa, t.OportunidadId,
            t.OportunidadId is { } oid ? Oportunidades[oid].Codigo : null, t.Accion, t.TipoAccion, t.Estado, t.Prioridad,
            t.ComercialId, NombreComercial(t.ComercialId), t.FechaAsignacion, t.FechaVencimiento,
            t.FechaVencimiento.DayNumber - hoy.DayNumber, t.Notas, t.CompletadaEn, t.CreadoEn);
    }

    Task<IReadOnlyList<TareaFila>> ITareaRepository.ListarAsync(DateOnly hoy, Guid? comercialId, Guid? clienteId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<TareaFila>>(
            [.. Tareas.Values.Where(t => t.EliminadoEn is null && (comercialId is null || t.ComercialId == comercialId)
                                         && (clienteId is null || t.ClienteId == clienteId)).Select(t => Fila(t, hoy))]);

    Task<TareaFila?> ITareaRepository.ObtenerAsync(Guid id, DateOnly hoy, CancellationToken ct) =>
        Task.FromResult(Tareas.TryGetValue(id, out var t) && t.EliminadoEn is null ? Fila(t, hoy) : null);

    public Task CrearAsync(Tarea tarea, CancellationToken ct = default)
    {
        Tareas[tarea.Id] = tarea;
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(Tarea tarea, CancellationToken ct = default) => CrearAsync(tarea, ct);

    public Task<bool> CambiarEstadoAsync(
        Guid id, string estadoEsperado, string estado, DateTime? completadaEn, DateTime fecha, CancellationToken ct = default)
    {
        var t = Tareas[id];
        if (t.Estado != estadoEsperado)
        {
            return Task.FromResult(false);
        }

        t.Estado = estado;
        t.CompletadaEn = completadaEn;
        return Task.FromResult(true);
    }

    Task ITareaRepository.EliminarAsync(Guid id, DateTime fecha, CancellationToken ct)
    {
        Tareas[id].EliminadoEn = fecha;
        return Task.CompletedTask;
    }
}
