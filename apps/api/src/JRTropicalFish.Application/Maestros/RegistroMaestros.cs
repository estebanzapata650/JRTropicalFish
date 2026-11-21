namespace JRTropicalFish.Application.Maestros;

/// <param name="Clave">Identificador en la URL (<c>/api/v1/maestros/{clave}</c>) y en la auditoría.</param>
/// <param name="Tabla">Tabla en Postgres. Debe estar también en la lista blanca de <c>fn_maestro_tabla</c>.</param>
public record DefinicionMaestro(string Clave, string Tabla, string Nombre, string NombreSingular, string Descripcion);

/// <summary>
/// Los maestros que existen. Es la <b>lista blanca</b> de tablas: la clave que llega en la URL se
/// busca aquí y nunca se usa como nombre de tabla directamente. Las funciones PL/pgSQL genéricas
/// (<c>fn_maestro_*</c>, <c>sp_maestro_*</c>) vuelven a validar la tabla contra su propia lista.
///
/// <para>
/// Agregar un maestro: entidad en <c>Domain/Maestros</c> (hereda <c>CatalogoSimple</c>), DbSet y
/// <c>ConfigurarCatalogo</c> en <c>AppDbContext</c>, migración (tabla + recrear <c>fn_maestro_tabla</c>
/// con la tabla nueva + sembrado si aplica) y una entrada aquí. El web no cambia.
/// </para>
/// </summary>
public static class RegistroMaestros
{
    public const string TiposIdentificacion = "tipos-identificacion";
    public const string Cargos = "cargos";
    public const string Areas = "areas";
    public const string TiposContrato = "tipos-contrato";

    public static readonly IReadOnlyList<DefinicionMaestro> Todos =
    [
        new(TiposIdentificacion, "tipos_identificacion", "Tipos de identificación", "tipo de identificación",
            "Documentos de identidad de las personas: cédula, pasaporte…"),
        new(Cargos, "cargos", "Cargos", "cargo", "Cargos que ocupan los empleados."),
        new(Areas, "areas", "Áreas", "área", "Áreas o dependencias de la empresa."),
        new(TiposContrato, "tipos_contrato", "Tipos de contrato", "tipo de contrato", "Modalidades de contrato laboral."),
    ];

    public static DefinicionMaestro? Buscar(string? clave) =>
        Todos.FirstOrDefault(m => string.Equals(m.Clave, clave, StringComparison.Ordinal));

    public static DefinicionMaestro Obtener(string clave) =>
        Buscar(clave) ?? throw new ArgumentException($"Maestro no registrado: {clave}", nameof(clave));
}
