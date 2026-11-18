using JRTropicalFish.Infrastructure.Persistence.Sembrado;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Siembra permisos y perfiles plantilla, y crea las funciones/procedimientos PL/pgSQL de
    /// seguridad y auditoría que usan los repositorios Dapper.
    ///
    /// Convención: <c>fn_*</c> para lecturas (<c>SELECT * FROM fn_x(...)</c>), <c>sp_*</c> para
    /// escrituras (<c>CALL sp_x(...)</c>). Columnas varchar se devuelven con <c>::text</c> porque las
    /// funciones SQL exigen que el tipo coincida con el de <c>RETURNS TABLE</c>. Cada objeto creado
    /// en <c>Up</c> tiene su DROP en <c>Down</c>.
    /// </summary>
    public partial class SembradoSeguridad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SembradoPermisos.Aplicar(migrationBuilder);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_usuario_por_email(p_email text)
                RETURNS TABLE (id uuid, nombre text, email text, password_hash text,
                               perfil_id uuid, perfil_nombre text, activo boolean)
                LANGUAGE sql STABLE AS $$
                    SELECT u.id, u.nombre::text, u.email::text, u.password_hash,
                           u.perfil_id, p.nombre::text, u.activo
                    FROM usuarios u
                    JOIN perfiles p ON p.id = u.perfil_id
                    WHERE lower(u.email) = lower(p_email);
                $$;
                """);

            // Un perfil inactivo no concede permisos aunque el usuario siga activo.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_usuario_permisos(p_usuario_id uuid)
                RETURNS TABLE (codigo text)
                LANGUAGE sql STABLE AS $$
                    SELECT pm.codigo::text
                    FROM usuarios u
                    JOIN perfiles pf ON pf.id = u.perfil_id AND pf.activo
                    JOIN perfil_permisos pp ON pp.perfil_id = pf.id
                    JOIN permisos pm ON pm.id = pp.permiso_id
                    WHERE u.id = p_usuario_id
                    ORDER BY pm.codigo;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_perfil_id_por_nombre(p_nombre text)
                RETURNS uuid
                LANGUAGE sql STABLE AS $$
                    SELECT id FROM perfiles WHERE nombre = p_nombre;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_usuario_crear(
                    p_id uuid, p_nombre text, p_email text, p_password_hash text,
                    p_perfil_id uuid, p_activo boolean, p_creado_en timestamptz)
                LANGUAGE sql AS $$
                    INSERT INTO usuarios (id, nombre, email, password_hash, perfil_id, activo, creado_en)
                    VALUES (p_id, p_nombre, lower(p_email), p_password_hash, p_perfil_id, p_activo, p_creado_en);
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_auditoria_registrar(
                    p_id uuid, p_entidad text, p_entidad_id uuid, p_accion text, p_cambios jsonb,
                    p_usuario_id uuid, p_usuario_nombre text, p_fecha_utc timestamptz)
                LANGUAGE sql AS $$
                    INSERT INTO auditoria (id, entidad, entidad_id, accion, cambios_json,
                                           usuario_id, usuario_nombre, fecha_utc)
                    VALUES (p_id, p_entidad, p_entidad_id, p_accion, p_cambios,
                            p_usuario_id, p_usuario_nombre, p_fecha_utc);
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_auditoria_por_entidad(p_entidad text, p_entidad_id uuid, p_limite int)
                RETURNS TABLE (id uuid, entidad text, entidad_id uuid, accion text, cambios_json text,
                               usuario_id uuid, usuario_nombre text, fecha_utc timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT a.id, a.entidad::text, a.entidad_id, a.accion::text, a.cambios_json::text,
                           a.usuario_id, a.usuario_nombre::text, a.fecha_utc
                    FROM auditoria a
                    WHERE a.entidad = p_entidad AND a.entidad_id = p_entidad_id
                    ORDER BY a.fecha_utc DESC
                    LIMIT p_limite;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fn_auditoria_por_entidad(text, uuid, int);");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_auditoria_registrar(uuid, text, uuid, text, jsonb, uuid, text, timestamptz);");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_usuario_crear(uuid, text, text, text, uuid, boolean, timestamptz);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fn_perfil_id_por_nombre(text);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fn_usuario_permisos(uuid);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fn_usuario_por_email(text);");

            migrationBuilder.Sql("DELETE FROM perfil_permisos;");
            migrationBuilder.Sql(
                """
                DELETE FROM perfiles p
                WHERE p.es_plantilla AND NOT EXISTS (SELECT 1 FROM usuarios u WHERE u.perfil_id = p.id);
                """);
            migrationBuilder.Sql("DELETE FROM permisos;");
        }
    }
}
