using JRTropicalFish.Domain.Comun;

namespace JRTropicalFish.Domain.Seguridad;

public class Usuario : EntidadBase
{
    public required string Nombre { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public Guid PerfilId { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime? UltimoAccesoEn { get; set; }

    /// <summary>Intentos de ingreso fallidos consecutivos. Vuelve a 0 al ingresar o al bloquearse.</summary>
    public int IntentosFallidos { get; set; }

    /// <summary>Si tiene valor y es futuro, la cuenta no puede ingresar (protección contra fuerza bruta).</summary>
    public DateTime? BloqueadoHasta { get; set; }

    /// <summary>
    /// Todo token emitido antes de esta marca deja de ser válido. Se mueve al cambiar la contraseña,
    /// desactivar al usuario o cambiar sus permisos: así el cambio aplica de inmediato y no cuando
    /// el token expire.
    /// </summary>
    public DateTime? SesionInvalidaAntes { get; set; }
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
