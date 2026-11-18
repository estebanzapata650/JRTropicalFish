namespace JRTropicalFish.Domain.Comun;

/// <summary>
/// Forma común de los catálogos administrables desde «Maestros»: código + nombre, con estado
/// activo, auditoría y borrado lógico. Ejemplos previstos: familias de peces, tallas, tipos de
/// documento de importación, unidades de empaque.
/// </summary>
public abstract class CatalogoSimple : EntidadBase
{
    public required string Codigo { get; set; }

    public required string Nombre { get; set; }

    public bool Activo { get; set; } = true;

    /// <summary>Borrado lógico: las consultas filtran por <c>eliminado_en IS NULL</c>.</summary>
    public DateTime? EliminadoEn { get; set; }
}
