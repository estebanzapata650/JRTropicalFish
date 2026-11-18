using JRTropicalFish.Infrastructure.Persistence.Sembrado;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Funciones y procedimientos del módulo de Seguridad: administración de usuarios y perfiles,
    /// bloqueo por intentos fallidos y revocación de sesiones (<c>sesion_invalida_antes</c>).
    ///
    /// <para>
    /// <c>fn_usuario_por_email</c> cambia su <c>RETURNS TABLE</c> (agrega intentos y bloqueo), y
    /// <c>CREATE OR REPLACE</c> no lo permite: se hace DROP + CREATE, y <c>Down</c> restaura la versión
    /// anterior. El orden de columnas de cada función debe coincidir con el record de C# que la lee
    /// (Dapper construye por posición).
    /// </para>
    /// </summary>
    public partial class FuncionesSeguridad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Actualiza la descripción de usuarios:eliminar ("Desactivar y reactivar usuarios").
            SembradoPermisos.Aplicar(migrationBuilder);

            CrearFuncionesAcceso(migrationBuilder);
            CrearFuncionesUsuarios(migrationBuilder);
            CrearFuncionesPerfiles(migrationBuilder);
        }

        private static void CrearFuncionesAcceso(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fn_usuario_por_email(text);");

            const string columnasAcceso =
                """
                RETURNS TABLE (id uuid, nombre text, email text, password_hash text, perfil_id uuid,
                               perfil_nombre text, activo boolean, intentos_fallidos integer,
                               bloqueado_hasta timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT u.id, u.nombre::text, u.email::text, u.password_hash, u.perfil_id,
                           p.nombre::text, u.activo, u.intentos_fallidos, u.bloqueado_hasta
                    FROM usuarios u
                    JOIN perfiles p ON p.id = u.perfil_id
                """;

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_usuario_por_email(p_email text)
                {columnasAcceso}
                    WHERE u.email = lower(trim(p_email));
                $$;
                """);

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_usuario_acceso_por_id(p_id uuid)
                {columnasAcceso}
                    WHERE u.id = p_id;
                $$;
                """);

            // El token se emite con segundos enteros (iat): por eso se trunca la marca de revocación.
            // Un token emitido en el mismo segundo que la revocación (el que se entrega al cambiar la
            // propia contraseña) sigue valiendo; uno anterior, no.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_usuario_sesion_valida(p_id uuid, p_emitido_en timestamptz)
                RETURNS boolean
                LANGUAGE sql STABLE AS $$
                    SELECT EXISTS (
                        SELECT 1 FROM usuarios
                        WHERE id = p_id AND activo
                          AND (sesion_invalida_antes IS NULL
                               OR p_emitido_en >= date_trunc('second', sesion_invalida_antes)));
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_usuario_registrar_acceso(p_id uuid, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE usuarios
                    SET ultimo_acceso_en = p_fecha, intentos_fallidos = 0, bloqueado_hasta = NULL
                    WHERE id = p_id;
                $$;
                """);

            // La decisión de bloquear la toma C# (ReglasBloqueo); aquí solo se aplica.
            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_usuario_registrar_fallo(p_id uuid, p_bloquear_hasta timestamptz)
                LANGUAGE sql AS $$
                    UPDATE usuarios
                    SET intentos_fallidos = CASE WHEN p_bloquear_hasta IS NULL THEN intentos_fallidos + 1 ELSE 0 END,
                        bloqueado_hasta = COALESCE(p_bloquear_hasta, bloqueado_hasta)
                    WHERE id = p_id;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_usuario_desbloquear(p_id uuid)
                LANGUAGE sql AS $$
                    UPDATE usuarios SET intentos_fallidos = 0, bloqueado_hasta = NULL WHERE id = p_id;
                $$;
                """);
        }

        private static void CrearFuncionesUsuarios(MigrationBuilder migrationBuilder)
        {
            const string columnasResumen =
                """
                RETURNS TABLE (id uuid, nombre text, email text, perfil_id uuid, perfil_nombre text,
                               activo boolean, bloqueado_hasta timestamptz, ultimo_acceso_en timestamptz,
                               creado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT u.id, u.nombre::text, u.email::text, u.perfil_id, p.nombre::text,
                           u.activo, u.bloqueado_hasta, u.ultimo_acceso_en, u.creado_en
                    FROM usuarios u
                    JOIN perfiles p ON p.id = u.perfil_id
                """;

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_usuarios_listar(p_busqueda text)
                {columnasResumen}
                    WHERE p_busqueda IS NULL OR p_busqueda = ''
                       OR u.nombre ILIKE '%' || p_busqueda || '%'
                       OR u.email ILIKE '%' || p_busqueda || '%'
                    ORDER BY u.activo DESC, u.nombre;
                $$;
                """);

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_usuario_por_id(p_id uuid)
                {columnasResumen}
                    WHERE u.id = p_id;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_usuario_email_existe(p_email text, p_excluir_id uuid)
                RETURNS boolean
                LANGUAGE sql STABLE AS $$
                    SELECT EXISTS (SELECT 1 FROM usuarios
                        WHERE email = lower(trim(p_email))
                          AND (p_excluir_id IS NULL OR id <> p_excluir_id));
                $$;
                """);

            // "Administrador" = usuario activo con usuarios:editar en un perfil activo.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_administradores_contar(p_excluir_usuario_id uuid, p_excluir_perfil_id uuid)
                RETURNS bigint
                LANGUAGE sql STABLE AS $$
                    SELECT count(DISTINCT u.id)
                    FROM usuarios u
                    JOIN perfiles pf ON pf.id = u.perfil_id AND pf.activo
                    JOIN perfil_permisos pp ON pp.perfil_id = pf.id
                    JOIN permisos pm ON pm.id = pp.permiso_id AND pm.codigo = 'usuarios:editar'
                    WHERE u.activo
                      AND (p_excluir_usuario_id IS NULL OR u.id <> p_excluir_usuario_id)
                      AND (p_excluir_perfil_id IS NULL OR pf.id <> p_excluir_perfil_id);
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_usuario_actualizar(
                    p_id uuid, p_nombre text, p_email text, p_perfil_id uuid,
                    p_invalidar_sesiones boolean, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE usuarios
                    SET nombre = trim(p_nombre), email = lower(trim(p_email)), perfil_id = p_perfil_id,
                        actualizado_en = p_fecha,
                        sesion_invalida_antes = CASE WHEN p_invalidar_sesiones THEN p_fecha ELSE sesion_invalida_antes END
                    WHERE id = p_id;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_usuario_cambiar_estado(p_id uuid, p_activo boolean, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE usuarios
                    SET activo = p_activo, actualizado_en = p_fecha,
                        sesion_invalida_antes = CASE WHEN NOT p_activo THEN p_fecha ELSE sesion_invalida_antes END
                    WHERE id = p_id;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_usuario_cambiar_password(p_id uuid, p_password_hash text, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE usuarios
                    SET password_hash = p_password_hash, sesion_invalida_antes = p_fecha,
                        intentos_fallidos = 0, bloqueado_hasta = NULL, actualizado_en = p_fecha
                    WHERE id = p_id;
                $$;
                """);
        }

        private static void CrearFuncionesPerfiles(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_permisos_listar()
                RETURNS TABLE (id uuid, codigo text, descripcion text)
                LANGUAGE sql STABLE AS $$
                    SELECT id, codigo::text, descripcion::text FROM permisos ORDER BY codigo;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_perfiles_listar(p_busqueda text)
                RETURNS TABLE (id uuid, nombre text, descripcion text, es_plantilla boolean, activo boolean,
                               total_permisos bigint, total_usuarios bigint)
                LANGUAGE sql STABLE AS $$
                    SELECT p.id, p.nombre::text, p.descripcion::text, p.es_plantilla, p.activo,
                           (SELECT count(*) FROM perfil_permisos pp WHERE pp.perfil_id = p.id),
                           (SELECT count(*) FROM usuarios u WHERE u.perfil_id = p.id)
                    FROM perfiles p
                    WHERE p_busqueda IS NULL OR p_busqueda = '' OR p.nombre ILIKE '%' || p_busqueda || '%'
                    ORDER BY p.es_plantilla DESC, p.nombre;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_perfil_por_id(p_id uuid)
                RETURNS TABLE (id uuid, nombre text, descripcion text, es_plantilla boolean, activo boolean)
                LANGUAGE sql STABLE AS $$
                    SELECT id, nombre::text, descripcion::text, es_plantilla, activo FROM perfiles WHERE id = p_id;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_perfil_permisos(p_perfil_id uuid)
                RETURNS TABLE (codigo text)
                LANGUAGE sql STABLE AS $$
                    SELECT pm.codigo::text
                    FROM perfil_permisos pp
                    JOIN permisos pm ON pm.id = pp.permiso_id
                    WHERE pp.perfil_id = p_perfil_id
                    ORDER BY pm.codigo;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_perfil_nombre_existe(p_nombre text, p_excluir_id uuid)
                RETURNS boolean
                LANGUAGE sql STABLE AS $$
                    SELECT EXISTS (SELECT 1 FROM perfiles
                        WHERE lower(nombre) = lower(trim(p_nombre))
                          AND (p_excluir_id IS NULL OR id <> p_excluir_id));
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_perfil_usuarios_contar(p_id uuid)
                RETURNS bigint
                LANGUAGE sql STABLE AS $$
                    SELECT count(*) FROM usuarios WHERE perfil_id = p_id;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_perfil_crear(
                    p_id uuid, p_nombre text, p_descripcion text, p_activo boolean, p_creado_en timestamptz)
                LANGUAGE sql AS $$
                    INSERT INTO perfiles (id, nombre, descripcion, es_plantilla, activo, creado_en)
                    VALUES (p_id, trim(p_nombre), p_descripcion, false, p_activo, p_creado_en);
                $$;
                """);

            // Si cambian permisos o estado, los usuarios del perfil deben volver a iniciar sesión.
            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_perfil_actualizar(
                    p_id uuid, p_nombre text, p_descripcion text, p_activo boolean,
                    p_invalidar_sesiones boolean, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE perfiles
                    SET nombre = trim(p_nombre), descripcion = p_descripcion, activo = p_activo, actualizado_en = p_fecha
                    WHERE id = p_id;

                    UPDATE usuarios SET sesion_invalida_antes = p_fecha
                    WHERE p_invalidar_sesiones AND perfil_id = p_id;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_perfil_eliminar(p_id uuid)
                LANGUAGE sql AS $$
                    DELETE FROM perfiles WHERE id = p_id AND NOT es_plantilla;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_perfil_reemplazar_permisos(p_perfil_id uuid, p_codigos text[])
                LANGUAGE plpgsql AS $$
                BEGIN
                    DELETE FROM perfil_permisos WHERE perfil_id = p_perfil_id;
                    IF p_codigos IS NOT NULL AND array_length(p_codigos, 1) > 0 THEN
                        INSERT INTO perfil_permisos (perfil_id, permiso_id)
                        SELECT p_perfil_id, pm.id FROM permisos pm WHERE pm.codigo = ANY(p_codigos);
                    END IF;
                END;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var objeto in new[]
                     {
                         "PROCEDURE sp_perfil_reemplazar_permisos(uuid, text[])",
                         "PROCEDURE sp_perfil_eliminar(uuid)",
                         "PROCEDURE sp_perfil_actualizar(uuid, text, text, boolean, boolean, timestamptz)",
                         "PROCEDURE sp_perfil_crear(uuid, text, text, boolean, timestamptz)",
                         "FUNCTION fn_perfil_usuarios_contar(uuid)",
                         "FUNCTION fn_perfil_nombre_existe(text, uuid)",
                         "FUNCTION fn_perfil_permisos(uuid)",
                         "FUNCTION fn_perfil_por_id(uuid)",
                         "FUNCTION fn_perfiles_listar(text)",
                         "FUNCTION fn_permisos_listar()",
                         "PROCEDURE sp_usuario_cambiar_password(uuid, text, timestamptz)",
                         "PROCEDURE sp_usuario_cambiar_estado(uuid, boolean, timestamptz)",
                         "PROCEDURE sp_usuario_actualizar(uuid, text, text, uuid, boolean, timestamptz)",
                         "FUNCTION fn_administradores_contar(uuid, uuid)",
                         "FUNCTION fn_usuario_email_existe(text, uuid)",
                         "FUNCTION fn_usuario_por_id(uuid)",
                         "FUNCTION fn_usuarios_listar(text)",
                         "PROCEDURE sp_usuario_desbloquear(uuid)",
                         "PROCEDURE sp_usuario_registrar_fallo(uuid, timestamptz)",
                         "PROCEDURE sp_usuario_registrar_acceso(uuid, timestamptz)",
                         "FUNCTION fn_usuario_sesion_valida(uuid, timestamptz)",
                         "FUNCTION fn_usuario_acceso_por_id(uuid)",
                         "FUNCTION fn_usuario_por_email(text)",
                     })
            {
                migrationBuilder.Sql($"DROP {objeto.Split(' ')[0]} IF EXISTS {objeto[(objeto.IndexOf(' ') + 1)..]};");
            }

            // Restaura la versión de SembradoSeguridad.
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
        }
    }
}
