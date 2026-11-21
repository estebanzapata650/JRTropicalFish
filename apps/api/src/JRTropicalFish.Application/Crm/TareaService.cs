using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Domain.Crm;
using static JRTropicalFish.Application.Crm.ValidacionCrm;

namespace JRTropicalFish.Application.Crm;

/// <summary>
/// Checklist operativo diario de los comerciales (tablero Kanban). Los días restantes se calculan
/// contra el "hoy" de negocio (<see cref="IReloj.Hoy"/>); una tarea vencida y no completada va en rojo.
/// </summary>
public class TareaService(
    ITareaRepository tareas,
    IClienteRepository clientes,
    IOportunidadRepository oportunidades,
    IComercialRepository comerciales,
    IConsecutivoRepository consecutivos,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    public const string Entidad = "Tarea";
    public const string Prefijo = "TAR";

    public Task<IReadOnlyList<TareaFila>> ListarAsync(AlcanceCrm alcance, Guid? clienteId = null, CancellationToken ct = default) =>
        tareas.ListarAsync(reloj.Hoy, alcance.FiltroComercial, clienteId, ct);

    public async Task<Resultado<TareaFila>> ObtenerAsync(Guid id, AlcanceCrm alcance, CancellationToken ct = default) =>
        await Visible(id, alcance, ct) is { } t ? t : NoExiste();

    public async Task<Resultado<Creado>> CrearAsync(GuardarTareaRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        var datos = await ValidarAsync(r, alcance, null, ct);
        if (!datos.Exitoso)
        {
            return datos.Error!;
        }

        var (tarea, auditable) = datos.Valor;
        tarea.Codigo = await consecutivos.SiguienteAsync(Prefijo, ct);
        tarea.CreadoEn = reloj.AhoraUtc;
        await tareas.CrearAsync(tarea, ct);
        await auditoria.CreacionAsync(Entidad, tarea.Id, auditable, ct: ct);
        return new Creado(tarea.Id, tarea.Codigo);
    }

    public async Task<Resultado<Guid>> ActualizarAsync(Guid id, GuardarTareaRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await Visible(id, alcance, ct) is not { } existente)
        {
            return NoExiste();
        }

        var datos = await ValidarAsync(r, alcance, existente, ct);
        if (!datos.Exitoso)
        {
            return datos.Error!;
        }

        var (tarea, auditable) = datos.Valor;
        tarea.Id = id;
        tarea.Codigo = existente.Codigo;
        tarea.CreadoEn = existente.CreadoEn;
        tarea.ActualizadoEn = reloj.AhoraUtc;
        await tareas.ActualizarAsync(tarea, ct);
        await auditoria.CambiosAsync(Entidad, id, ComparadorAuditoria.Comparar(Auditable(existente), auditable), ct);
        return id;
    }

    /// <summary>Mover la tarjeta en el tablero. Cualquier estado a cualquier estado (se puede reabrir).</summary>
    public async Task<Resultado<Guid>> CambiarEstadoAsync(
        Guid id, CambiarEstadoTareaRequest r, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await Visible(id, alcance, ct) is not { } existente)
        {
            return NoExiste();
        }

        if (EnLista(r.Estado, EstadosTarea.Todos, "Estado") is { } error)
        {
            return error;
        }

        var esperado = r.EstadoActual ?? existente.Estado;
        if (r.Estado == existente.Estado && esperado == existente.Estado)
        {
            return id;
        }

        var completadaEn = CompletadaEn(r.Estado!, existente);
        if (esperado != existente.Estado
            || !await tareas.CambiarEstadoAsync(id, esperado, r.Estado!, completadaEn, reloj.AhoraUtc, ct))
        {
            return Error.Conflicto("Otra persona movió esta tarea mientras tanto. Recarga la página.");
        }

        await auditoria.CambiosAsync(Entidad, id, [new(nameof(TareaAuditable.Estado), existente.Estado, r.Estado)], ct);
        return id;
    }

    public async Task<Resultado> EliminarAsync(Guid id, AlcanceCrm alcance, CancellationToken ct = default)
    {
        if (await Visible(id, alcance, ct) is not { } existente)
        {
            return NoExiste();
        }

        await tareas.EliminarAsync(id, reloj.AhoraUtc, ct);
        await auditoria.EliminacionAsync(Entidad, id, Auditable(existente), ct);
        return Resultado.Ok();
    }

    public async Task<Resultado<IReadOnlyList<RegistroAuditoriaLectura>>> HistorialAsync(
        Guid id, AlcanceCrm alcance, CancellationToken ct = default) =>
        await Visible(id, alcance, ct) is null ? NoExiste() : Resultado.Ok(await auditoria.HistorialAsync(Entidad, id, ct));

    /// <summary>Se marca al completar y se conserva mientras siga completada; al reabrir se borra.</summary>
    private DateTime? CompletadaEn(string estado, TareaFila? existente) =>
        estado != EstadosTarea.Completada
            ? null
            : existente is { Estado: EstadosTarea.Completada, CompletadaEn: { } antes } ? antes : reloj.AhoraUtc;

    private async Task<Resultado<(Tarea Tarea, TareaAuditable Auditable)>> ValidarAsync(
        GuardarTareaRequest r, AlcanceCrm alcance, TareaFila? existente, CancellationToken ct)
    {
        var accion = Limpiar(r.Accion);
        var notas = LimpiarTextoLargo(r.Notas);
        var estado = r.Estado ?? existente?.Estado ?? EstadosTarea.Pendiente;
        var asignacion = r.FechaAsignacion ?? existente?.FechaAsignacion ?? reloj.Hoy;
        var error = Primero(
            Requerido(accion, "Acción comercial", 300, 3),
            EnLista(r.TipoAccion, TiposAccion.Todos, "Tipo de acción"),
            EnLista(estado, EstadosTarea.Todos, "Estado"),
            EnLista(r.Prioridad, Prioridades.Todos, "Prioridad"),
            r.FechaVencimiento is null ? Error.Validacion("Indica la fecha de vencimiento.") : null,
            r.FechaVencimiento < asignacion
                ? Error.Validacion("La fecha de vencimiento no puede ser anterior a la de asignación.")
                : null,
            Opcional(notas, "Notas", 4000));
        if (error is not null)
        {
            return error;
        }

        if (await clientes.ObtenerAsync(r.ClienteId, ct) is not { } cliente)
        {
            return Error.Validacion("Elige un cliente existente.");
        }

        string? oportunidadCodigo = null;
        if (r.OportunidadId is { } oportunidadId)
        {
            var oportunidad = await oportunidades.ObtenerAsync(oportunidadId, ct);
            if (oportunidad is null || !alcance.PuedeVer(oportunidad.ComercialId))
            {
                return Error.Validacion("La oportunidad elegida no existe.");
            }

            if (oportunidad.ClienteId != cliente.Id)
            {
                return Error.Validacion("La oportunidad elegida es de otro cliente.");
            }

            oportunidadCodigo = oportunidad.Codigo;
        }

        var comercial = await ComercialesCrm.ResolverAsync(
            comerciales, alcance, r.ComercialId,
            existente is null ? null : (existente.ComercialId, existente.ComercialNombre), ct);
        if (!comercial.Exitoso)
        {
            return comercial.Error!;
        }

        var tarea = new Tarea
        {
            Codigo = "",
            ClienteId = cliente.Id,
            OportunidadId = r.OportunidadId,
            Accion = accion!,
            TipoAccion = r.TipoAccion!,
            Estado = estado,
            Prioridad = r.Prioridad!,
            ComercialId = comercial.Valor.Id,
            FechaAsignacion = asignacion,
            FechaVencimiento = r.FechaVencimiento!.Value,
            Notas = notas,
            CompletadaEn = CompletadaEn(estado, existente),
        };
        var auditable = new TareaAuditable(
            cliente.NombreEmpresa, oportunidadCodigo, tarea.Accion, tarea.TipoAccion, tarea.Estado, tarea.Prioridad,
            comercial.Valor.Nombre, tarea.FechaAsignacion, tarea.FechaVencimiento, tarea.Notas);
        return (tarea, auditable);
    }

    private async Task<TareaFila?> Visible(Guid id, AlcanceCrm alcance, CancellationToken ct) =>
        await tareas.ObtenerAsync(id, reloj.Hoy, ct) is { } t && alcance.PuedeVer(t.ComercialId) ? t : null;

    private static TareaAuditable Auditable(TareaFila t) =>
        new(t.ClienteNombre, t.OportunidadCodigo, t.Accion, t.TipoAccion, t.Estado, t.Prioridad, t.ComercialNombre,
            t.FechaAsignacion, t.FechaVencimiento, t.Notas);

    private static Error NoExiste() => Error.NoEncontrado("La tarea no existe.");
}
