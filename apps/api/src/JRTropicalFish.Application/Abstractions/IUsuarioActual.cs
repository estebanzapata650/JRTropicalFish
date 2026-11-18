namespace JRTropicalFish.Application.Abstractions;

/// <summary>Identidad del usuario que ejecuta la petición actual — se usa para la auditoría.</summary>
public interface IUsuarioActual
{
    Guid? UsuarioId { get; }
    string? Nombre { get; }
}
