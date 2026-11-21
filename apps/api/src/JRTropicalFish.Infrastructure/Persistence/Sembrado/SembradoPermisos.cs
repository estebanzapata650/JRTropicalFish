using JRTropicalFish.Domain.Seguridad;
using Microsoft.EntityFrameworkCore.Migrations;

namespace JRTropicalFish.Infrastructure.Persistence.Sembrado;

/// <summary>
/// Siembra el catálogo de permisos y los perfiles plantilla a partir de <see cref="Permisos"/> y
/// <see cref="PerfilesPlantilla"/>, que son la única fuente de verdad.
///
/// <para>
/// Es <b>idempotente</b>: cada migración que agregue permisos de un módulo nuevo vuelve a llamar a
/// <see cref="Aplicar"/>. Inserta los permisos que falten, actualiza descripciones, borra los
/// retirados y asigna permisos a los perfiles plantilla sin deshacer ajustes que el negocio haya
/// hecho a mano.
/// </para>
///
/// <para>
/// Cuando un perfil <b>recibe o pierde</b> permisos, se revocan las sesiones de sus usuarios
/// (<c>usuarios.sesion_invalida_antes</c>): el token trae los permisos de cuando se emitió, y sin esto
/// las pantallas nuevas no aparecerían hasta que expire. Así el usuario vuelve al login y entra con
/// los permisos al día. Si el perfil no cambió, nadie pierde la sesión.
/// </para>
/// </summary>
public static class SembradoPermisos
{
    public static void Aplicar(MigrationBuilder migrationBuilder)
    {
        SembrarPermisos(migrationBuilder);
        SembrarPerfilesPlantilla(migrationBuilder);
    }

    private static void SembrarPermisos(MigrationBuilder migrationBuilder)
    {
        var filas = Permisos.Todos.Select(p => $"(gen_random_uuid(), '{p.Key}', '{p.Value.Replace("'", "''")}')");

        migrationBuilder.Sql(
            $"""
            INSERT INTO permisos (id, codigo, descripcion) VALUES
                {string.Join(",\n    ", filas)}
            ON CONFLICT (codigo) DO UPDATE SET descripcion = EXCLUDED.descripcion;
            """);

        if (Permisos.Retirados.Count > 0)
        {
            var retirados = string.Join(", ", Permisos.Retirados.Select(c => $"'{c}'"));
            // Quitar un permiso cambia lo que pueden hacer sus usuarios: se revocan sus sesiones.
            migrationBuilder.Sql(
                $"""
                WITH quitados AS (
                    DELETE FROM perfil_permisos
                    WHERE permiso_id IN (SELECT id FROM permisos WHERE codigo IN ({retirados}))
                    RETURNING perfil_id)
                {ActualizarSesiones("quitados")}
                DELETE FROM permisos WHERE codigo IN ({retirados});
                """);
        }
    }

    /// <summary>
    /// Crea los perfiles plantilla. Los permisos nuevos se asignan solo si el perfil aún no los
    /// tiene (<c>ON CONFLICT DO NOTHING</c>); nunca se quitan permisos que el negocio haya agregado.
    /// El Administrador recibe siempre todos.
    /// </summary>
    private static void SembrarPerfilesPlantilla(MigrationBuilder migrationBuilder)
    {
        foreach (var (nombre, permisos) in PerfilesPlantilla.Definicion)
        {
            migrationBuilder.Sql(
                $"""
                INSERT INTO perfiles (id, nombre, descripcion, es_plantilla, activo, creado_en)
                VALUES (gen_random_uuid(), '{nombre}', 'Perfil base del sistema', true, true, now())
                ON CONFLICT (nombre) DO UPDATE SET es_plantilla = true;
                """);

            if (permisos.Count == 0)
            {
                continue;
            }

            // Si el perfil recibió permisos nuevos, sus usuarios deben volver a iniciar sesión: el token
            // trae los permisos de cuando se emitió y, sin esto, las pantallas nuevas no aparecerían
            // (sin aviso) hasta que el token expire.
            var codigos = string.Join(", ", permisos.Select(p => $"'{p}'"));
            migrationBuilder.Sql(
                $"""
                WITH nuevos AS (
                    INSERT INTO perfil_permisos (perfil_id, permiso_id)
                    SELECT pf.id, pm.id
                    FROM perfiles pf
                    CROSS JOIN permisos pm
                    WHERE pf.nombre = '{nombre}' AND pm.codigo IN ({codigos})
                    ON CONFLICT DO NOTHING
                    RETURNING perfil_id)
                {ActualizarSesiones("nuevos")}
                """);
        }
    }

    /// <summary>
    /// Revoca las sesiones de los usuarios de los perfiles que devuelve la CTE <paramref name="cte"/>
    /// (mismo mecanismo que editar permisos desde la pantalla de Perfiles; ver <c>ValidacionSesion</c>).
    /// </summary>
    private static string ActualizarSesiones(string cte) =>
        $"""
        UPDATE usuarios SET sesion_invalida_antes = now()
        WHERE perfil_id IN (SELECT DISTINCT perfil_id FROM {cte});
        """;
}
