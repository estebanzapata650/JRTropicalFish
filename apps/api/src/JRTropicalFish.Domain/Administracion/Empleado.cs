using JRTropicalFish.Domain.Comun;

namespace JRTropicalFish.Domain.Administracion;

public class Empleado : EntidadBase
{
    // Identificación
    public Guid TipoIdentificacionId { get; set; }
    public required string NumeroIdentificacion { get; set; }
    public required string Nombres { get; set; }
    public required string Apellidos { get; set; }
    public string? Genero { get; set; }
    public DateOnly? FechaNacimiento { get; set; }

    // Contacto
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? Ciudad { get; set; }

    // Laboral
    public Guid CargoId { get; set; }
    public Guid? AreaId { get; set; }
    public Guid TipoContratoId { get; set; }
    public DateOnly FechaIngreso { get; set; }
    public DateOnly? FechaRetiro { get; set; }
    public decimal Salario { get; set; }
    public bool Activo { get; set; } = true;

    /// <summary>Clave del archivo de la foto en el almacén S3 (sale de <c>ClaveArchivo</c>).</summary>
    public string? FotoClave { get; set; }

    /// <summary>Borrado lógico: las consultas filtran por <c>eliminado_en IS NULL</c>.</summary>
    public DateTime? EliminadoEn { get; set; }
}

/// <summary>Valores admitidos para <see cref="Empleado.Genero"/>.</summary>
public static class Generos
{
    public static readonly IReadOnlyList<string> Todos = ["Femenino", "Masculino", "Otro"];
}
