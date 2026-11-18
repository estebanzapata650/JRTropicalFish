namespace JRTropicalFish.Domain.Auditoria;

public enum AccionAuditoria
{
    Crear,
    Actualizar,
    Eliminar,
}

/// <summary>
/// Control de cambios transversal. Un registro por operación, con el detalle campo a campo en
/// <see cref="CambiosJson"/>. En inventario es además la traza que explica un descuadre.
/// </summary>
public class RegistroAuditoria
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Nombre de la entidad afectada, ej. "Especie".</summary>
    public required string Entidad { get; set; }

    public required Guid EntidadId { get; set; }

    public AccionAuditoria Accion { get; set; }

    /// <summary>Cambios campo a campo: [{ campo, anterior, nuevo }]. En Crear/Eliminar va el estado completo.</summary>
    public required string CambiosJson { get; set; }

    public Guid? UsuarioId { get; set; }

    /// <summary>Se guarda el nombre además del id, para que el historial siga siendo legible
    /// aunque el usuario se elimine o cambie de nombre después.</summary>
    public string? UsuarioNombre { get; set; }

    public DateTime FechaUtc { get; set; }
}
