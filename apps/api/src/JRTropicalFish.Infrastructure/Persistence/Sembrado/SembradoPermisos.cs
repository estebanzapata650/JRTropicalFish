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
            migrationBuilder.Sql(
                $"""
                DELETE FROM perfil_permisos WHERE permiso_id IN (SELECT id FROM permisos WHERE codigo IN ({retirados}));
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

            var codigos = string.Join(", ", permisos.Select(p => $"'{p}'"));
            migrationBuilder.Sql(
                $"""
                INSERT INTO perfil_permisos (perfil_id, permiso_id)
                SELECT pf.id, pm.id
                FROM perfiles pf
                CROSS JOIN permisos pm
                WHERE pf.nombre = '{nombre}' AND pm.codigo IN ({codigos})
                ON CONFLICT DO NOTHING;
                """);
        }
    }
}
