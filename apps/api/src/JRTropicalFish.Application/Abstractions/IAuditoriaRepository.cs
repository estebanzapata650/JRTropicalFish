using JRTropicalFish.Domain.Auditoria;

namespace JRTropicalFish.Application.Abstractions;

public record CambioAuditado(string Campo, string? Anterior, string? Nuevo);

public record RegistroAuditoriaLectura(
    Guid Id,
    string Entidad,
    Guid EntidadId,
    string Accion,
    string CambiosJson,
    Guid? UsuarioId,
    string? UsuarioNombre,
    DateTime FechaUtc);

public interface IAuditoriaRepository
{
    Task RegistrarAsync(RegistroAuditoria registro, CancellationToken ct = default);

    Task<IReadOnlyList<RegistroAuditoriaLectura>> ListarPorEntidadAsync(
        string entidad, Guid entidadId, int limite, CancellationToken ct = default);
}
