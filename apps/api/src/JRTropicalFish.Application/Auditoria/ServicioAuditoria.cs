using System.Text.Json;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Domain.Auditoria;

namespace JRTropicalFish.Application.Auditoria;

/// <summary>
/// Punto único para escribir el historial de cambios. Arma el registro con el usuario actual y la
/// hora, y serializa los cambios campo a campo. Un servicio de módulo solo decide <i>qué</i> auditar.
/// </summary>
public class ServicioAuditoria(IAuditoriaRepository repositorio, IUsuarioActual usuarioActual, IReloj reloj)
{
    public const int LimiteHistorial = 100;

    /// <summary>
    /// Sin escapar tildes ni eñes: el historial se lee también desde SQL y "Contraseña" no se
    /// entiende. Es seguro porque el JSON va a una columna jsonb y el web lo pinta con React, que escapa.
    /// </summary>
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <param name="adicionales">Campos que no están en <paramref name="estado"/> (ej. la lista de permisos).</param>
    public Task CreacionAsync<T>(
        string entidad, Guid id, T estado, IEnumerable<CambioAuditado>? adicionales = null, CancellationToken ct = default) =>
        RegistrarAsync(
            entidad, id, AccionAuditoria.Crear,
            [.. ComparadorAuditoria.Instantanea(estado), .. adicionales ?? []], ct);

    public Task EliminacionAsync<T>(string entidad, Guid id, T estado, CancellationToken ct = default) =>
        RegistrarAsync(entidad, id, AccionAuditoria.Eliminar, ComparadorAuditoria.Instantanea(estado, comoValorNuevo: false), ct);

    /// <summary>Registra los cambios si hay alguno; si la lista está vacía no escribe nada.</summary>
    public Task CambiosAsync(string entidad, Guid id, IReadOnlyList<CambioAuditado> cambios, CancellationToken ct = default) =>
        cambios.Count == 0 ? Task.CompletedTask : RegistrarAsync(entidad, id, AccionAuditoria.Actualizar, cambios, ct);

    public Task<IReadOnlyList<RegistroAuditoriaLectura>> HistorialAsync(string entidad, Guid id, CancellationToken ct = default) =>
        repositorio.ListarPorEntidadAsync(entidad, id, LimiteHistorial, ct);

    private Task RegistrarAsync(
        string entidad, Guid id, AccionAuditoria accion, IReadOnlyList<CambioAuditado> cambios, CancellationToken ct) =>
        repositorio.RegistrarAsync(
            new RegistroAuditoria
            {
                Entidad = entidad,
                EntidadId = id,
                Accion = accion,
                CambiosJson = JsonSerializer.Serialize(cambios, OpcionesJson),
                UsuarioId = usuarioActual.UsuarioId,
                UsuarioNombre = usuarioActual.Nombre,
                FechaUtc = reloj.AhoraUtc,
            },
            ct);
}
