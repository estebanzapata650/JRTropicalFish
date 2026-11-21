namespace JRTropicalFish.Domain.Seguridad;

/// <summary>
/// Catálogo maestro de permisos, en formato <c>modulo:accion</c>. Es la única fuente de verdad:
/// se siembra en la base por migración, se usa en <c>[RequierePermiso]</c> y el sitio web tiene un
/// espejo en <c>apps/web/src/lib/permisos.ts</c>.
///
/// <para>
/// Al agregar un módulo: (1) constantes aquí, (2) entrada en <see cref="Todos"/>, (3) asignación en
/// <see cref="PerfilesPlantilla"/>, (4) migración nueva cuyo <c>Up</c> llame a
/// <c>SembradoPermisos.Aplicar(migrationBuilder)</c>, (5) espejo en el web. Ver CLAUDE.md, «Agregar un permiso».
/// </para>
/// </summary>
public static class Permisos
{
    public const string UsuariosLeer = "usuarios:leer";
    public const string UsuariosCrear = "usuarios:crear";
    public const string UsuariosEditar = "usuarios:editar";
    public const string UsuariosEliminar = "usuarios:eliminar";

    public const string PerfilesLeer = "perfiles:leer";
    public const string PerfilesCrear = "perfiles:crear";
    public const string PerfilesEditar = "perfiles:editar";
    public const string PerfilesEliminar = "perfiles:eliminar";

    public const string MaestrosLeer = "maestros:leer";
    public const string MaestrosEditar = "maestros:editar";

    public const string EmpleadosLeer = "empleados:leer";
    public const string EmpleadosCrear = "empleados:crear";
    public const string EmpleadosEditar = "empleados:editar";
    public const string EmpleadosEliminar = "empleados:eliminar";

    /// <summary>Ver y editar el salario. Sin este permiso el salario no viaja en ninguna respuesta.</summary>
    public const string EmpleadosVerSalario = "empleados:ver-salario";

    // Módulos de negocio (Catálogos, Inventario, Importaciones, Exportaciones): sus permisos se
    // agregan con cada módulo. El mapa previsto está en docs/dominio/modulos.md.

    /// <summary>Todos los permisos vigentes, con su descripción para la pantalla de perfiles.</summary>
    public static readonly IReadOnlyDictionary<string, string> Todos = new Dictionary<string, string>
    {
        [UsuariosLeer] = "Consultar usuarios",
        [UsuariosCrear] = "Crear usuarios",
        [UsuariosEditar] = "Editar usuarios",
        [UsuariosEliminar] = "Desactivar y reactivar usuarios",
        [PerfilesLeer] = "Consultar perfiles",
        [PerfilesCrear] = "Crear perfiles",
        [PerfilesEditar] = "Editar perfiles",
        [PerfilesEliminar] = "Eliminar perfiles",
        [MaestrosLeer] = "Consultar maestros",
        [MaestrosEditar] = "Crear, editar y eliminar valores de maestros",
        [EmpleadosLeer] = "Consultar empleados",
        [EmpleadosCrear] = "Crear empleados",
        [EmpleadosEditar] = "Editar empleados",
        [EmpleadosEliminar] = "Desactivar y eliminar empleados",
        [EmpleadosVerSalario] = "Ver y editar el salario de los empleados",
    };

    /// <summary>
    /// Códigos que existieron y ya no: el sembrado los borra de <c>permisos</c> y
    /// <c>perfil_permisos</c>. Necesario porque el sembrado es de solo inserción.
    /// </summary>
    public static readonly IReadOnlyList<string> Retirados = [];
}

/// <summary>Perfiles sembrados por migración. Editables, pero no eliminables.</summary>
public static class PerfilesPlantilla
{
    public const string Administrador = "Administrador";
    public const string Operativo = "Operativo";
    public const string Consulta = "Consulta";

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Definicion =
        new Dictionary<string, IReadOnlyList<string>>
        {
            [Administrador] = [.. Permisos.Todos.Keys],
            // Operativo y Consulta reciben los permisos de negocio a medida que nacen los módulos.
            [Operativo] = [],
            [Consulta] = [Permisos.UsuariosLeer, Permisos.PerfilesLeer, Permisos.MaestrosLeer, Permisos.EmpleadosLeer],
        };
}
