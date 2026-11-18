using JRTropicalFish.Domain.Comun;

namespace JRTropicalFish.Domain.Seguridad;

public class Usuario : EntidadBase
{
    public required string Nombre { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public Guid PerfilId { get; set; }

    public bool Activo { get; set; } = true;
}

public class Perfil : EntidadBase
{
    public required string Nombre { get; set; }

    public string? Descripcion { get; set; }

    /// <summary>Perfil sembrado por migración: se puede editar, pero no eliminar.</summary>
    public bool EsPlantilla { get; set; }

    public bool Activo { get; set; } = true;
}

public class Permiso
{
    public Guid Id { get; set; }

    /// <summary>Formato <c>modulo:accion</c>, ej. <c>inventario:ajustar</c>.</summary>
    public required string Codigo { get; set; }

    public string? Descripcion { get; set; }
}
