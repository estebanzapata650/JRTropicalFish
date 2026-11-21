using JRTropicalFish.Infrastructure.Persistence.Sembrado;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Funciones y procedimientos de Maestros y Empleados, sembrado de los maestros base y de los
    /// permisos nuevos.
    ///
    /// <para>
    /// <b>Maestros</b>: una tabla por maestro, pero un solo juego de funciones genéricas con SQL
    /// dinámico. La tabla llega como parámetro, se valida contra la lista blanca de
    /// <c>fn_maestro_tabla</c> y se cita con <c>format('%I')</c>. Al agregar un maestro, la migración
    /// que crea su tabla debe recrear <c>fn_maestro_tabla</c> incluyéndola (misma firma:
    /// <c>CREATE OR REPLACE</c> basta).
    /// </para>
    ///
    /// <para>
    /// El orden de columnas de cada función debe coincidir con el record de C# que la lee (Dapper
    /// construye por posición): <c>ValorMaestro</c> y <c>EmpleadoFila</c>.
    /// </para>
    /// </summary>
    public partial class FuncionesAdministracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SembradoPermisos.Aplicar(migrationBuilder);

            CrearFuncionesMaestros(migrationBuilder);
            SembrarMaestros(migrationBuilder);
            CrearFuncionesEmpleados(migrationBuilder);
        }

        private static void CrearFuncionesMaestros(MigrationBuilder migrationBuilder)
        {
            // Lista blanca: la única puerta para que un nombre de tabla llegue a un SQL dinámico.
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION fn_maestro_tabla(p_tabla text)
                RETURNS text
                LANGUAGE plpgsql IMMUTABLE AS $$
                BEGIN
                    IF p_tabla IS NULL OR p_tabla NOT IN ('tipos_identificacion', 'cargos', 'areas', 'tipos_contrato') THEN
                        RAISE EXCEPTION 'Maestro no permitido: %', p_tabla;
                    END IF;
                    RETURN p_tabla;
                END;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_maestro_listar(p_tabla text, p_solo_activos boolean)
                RETURNS TABLE (id uuid, codigo text, nombre text, activo boolean, creado_en timestamptz)
                LANGUAGE plpgsql STABLE AS $$
                BEGIN
                    RETURN QUERY EXECUTE format(
                        'SELECT id, codigo::text, nombre::text, activo, creado_en FROM %I
                         WHERE eliminado_en IS NULL AND (NOT $1 OR activo)
                         ORDER BY activo DESC, nombre',
                        fn_maestro_tabla(p_tabla))
                    USING p_solo_activos;
                END;
                $$;
                """);

            // Devuelve también inactivos (para validar y para mostrar lo que un empleado ya tiene).
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_maestro_por_id(p_tabla text, p_id uuid)
                RETURNS TABLE (id uuid, codigo text, nombre text, activo boolean, creado_en timestamptz)
                LANGUAGE plpgsql STABLE AS $$
                BEGIN
                    RETURN QUERY EXECUTE format(
                        'SELECT id, codigo::text, nombre::text, activo, creado_en FROM %I
                         WHERE id = $1 AND eliminado_en IS NULL',
                        fn_maestro_tabla(p_tabla))
                    USING p_id;
                END;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_maestro_valor_existe(p_tabla text, p_campo text, p_valor text, p_excluir_id uuid)
                RETURNS boolean
                LANGUAGE plpgsql STABLE AS $$
                DECLARE
                    v_existe boolean;
                BEGIN
                    IF p_campo NOT IN ('codigo', 'nombre') THEN
                        RAISE EXCEPTION 'Campo no permitido: %', p_campo;
                    END IF;
                    EXECUTE format(
                        'SELECT EXISTS (SELECT 1 FROM %I
                                        WHERE lower(%I) = lower(trim($1)) AND eliminado_en IS NULL
                                          AND ($2 IS NULL OR id <> $2))',
                        fn_maestro_tabla(p_tabla), p_campo)
                    INTO v_existe
                    USING p_valor, p_excluir_id;
                    RETURN v_existe;
                END;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_maestro_crear(p_tabla text, p_id uuid, p_codigo text, p_nombre text,
                                                  p_activo boolean, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    EXECUTE format(
                        'INSERT INTO %I (id, codigo, nombre, activo, creado_en) VALUES ($1, upper(trim($2)), trim($3), $4, $5)',
                        fn_maestro_tabla(p_tabla))
                    USING p_id, p_codigo, p_nombre, p_activo, p_fecha;
                END;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_maestro_actualizar(p_tabla text, p_id uuid, p_codigo text, p_nombre text,
                                                       p_activo boolean, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    EXECUTE format(
                        'UPDATE %I SET codigo = upper(trim($2)), nombre = trim($3), activo = $4, actualizado_en = $5
                         WHERE id = $1 AND eliminado_en IS NULL',
                        fn_maestro_tabla(p_tabla))
                    USING p_id, p_codigo, p_nombre, p_activo, p_fecha;
                END;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_maestro_eliminar(p_tabla text, p_id uuid, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    EXECUTE format(
                        'UPDATE %I SET eliminado_en = $2, actualizado_en = $2 WHERE id = $1 AND eliminado_en IS NULL',
                        fn_maestro_tabla(p_tabla))
                    USING p_id, p_fecha;
                END;
                $$;
                """);
        }

        /// <summary>Valores base. Idempotente: no toca lo que el negocio haya editado.</summary>
        private static void SembrarMaestros(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO tipos_identificacion (id, codigo, nombre, activo, creado_en) VALUES
                    (gen_random_uuid(), 'CC', 'Cédula de ciudadanía', true, now()),
                    (gen_random_uuid(), 'CE', 'Cédula de extranjería', true, now()),
                    (gen_random_uuid(), 'TI', 'Tarjeta de identidad', true, now()),
                    (gen_random_uuid(), 'PA', 'Pasaporte', true, now()),
                    (gen_random_uuid(), 'PPT', 'Permiso por protección temporal', true, now())
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO tipos_contrato (id, codigo, nombre, activo, creado_en) VALUES
                    (gen_random_uuid(), 'INDEFINIDO', 'Término indefinido', true, now()),
                    (gen_random_uuid(), 'FIJO', 'Término fijo', true, now()),
                    (gen_random_uuid(), 'OBRA', 'Obra o labor', true, now()),
                    (gen_random_uuid(), 'SERVICIOS', 'Prestación de servicios', true, now()),
                    (gen_random_uuid(), 'APRENDIZAJE', 'Aprendizaje', true, now())
                ON CONFLICT DO NOTHING;
                """);
        }

        private static void CrearFuncionesEmpleados(MigrationBuilder migrationBuilder)
        {
            // Los maestros se unen sin filtrar eliminado_en: un empleado conserva el nombre de un
            // valor que se borró después de asignárselo.
            const string columnas =
                """
                RETURNS TABLE (id uuid, tipo_identificacion_id uuid, tipo_identificacion_codigo text,
                               numero_identificacion text, nombres text, apellidos text, genero text,
                               fecha_nacimiento date, telefono text, email text, direccion text, ciudad text,
                               cargo_id uuid, cargo_nombre text, area_id uuid, area_nombre text,
                               tipo_contrato_id uuid, tipo_contrato_nombre text, fecha_ingreso date,
                               fecha_retiro date, salario numeric, activo boolean, foto_clave text,
                               creado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT e.id, e.tipo_identificacion_id, ti.codigo::text, e.numero_identificacion::text,
                           e.nombres::text, e.apellidos::text, e.genero::text, e.fecha_nacimiento,
                           e.telefono::text, e.email::text, e.direccion::text, e.ciudad::text,
                           e.cargo_id, c.nombre::text, e.area_id, a.nombre::text,
                           e.tipo_contrato_id, tc.nombre::text, e.fecha_ingreso,
                           e.fecha_retiro, e.salario, e.activo, e.foto_clave::text, e.creado_en
                    FROM empleados e
                    JOIN tipos_identificacion ti ON ti.id = e.tipo_identificacion_id
                    JOIN cargos c ON c.id = e.cargo_id
                    LEFT JOIN areas a ON a.id = e.area_id
                    JOIN tipos_contrato tc ON tc.id = e.tipo_contrato_id
                """;

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_empleados_listar(p_busqueda text)
                {columnas}
                    WHERE e.eliminado_en IS NULL
                      AND (p_busqueda IS NULL OR p_busqueda = ''
                           OR (e.nombres || ' ' || e.apellidos) ILIKE '%' || p_busqueda || '%'
                           OR e.numero_identificacion ILIKE '%' || p_busqueda || '%'
                           OR e.email ILIKE '%' || p_busqueda || '%'
                           OR c.nombre ILIKE '%' || p_busqueda || '%')
                    ORDER BY e.activo DESC, e.nombres, e.apellidos;
                $$;
                """);

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_empleado_por_id(p_id uuid)
                {columnas}
                    WHERE e.id = p_id AND e.eliminado_en IS NULL;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_empleado_identificacion_existe(p_tipo_id uuid, p_numero text, p_excluir_id uuid)
                RETURNS boolean
                LANGUAGE sql STABLE AS $$
                    SELECT EXISTS (SELECT 1 FROM empleados
                        WHERE tipo_identificacion_id = p_tipo_id
                          AND numero_identificacion = upper(trim(p_numero))
                          AND eliminado_en IS NULL
                          AND (p_excluir_id IS NULL OR id <> p_excluir_id));
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_empleado_crear(
                    p_id uuid, p_tipo_identificacion_id uuid, p_numero_identificacion text, p_nombres text,
                    p_apellidos text, p_genero text, p_fecha_nacimiento date, p_telefono text, p_email text,
                    p_direccion text, p_ciudad text, p_cargo_id uuid, p_area_id uuid, p_tipo_contrato_id uuid,
                    p_fecha_ingreso date, p_fecha_retiro date, p_salario numeric, p_activo boolean,
                    p_creado_en timestamptz)
                LANGUAGE sql AS $$
                    INSERT INTO empleados (
                        id, tipo_identificacion_id, numero_identificacion, nombres, apellidos, genero,
                        fecha_nacimiento, telefono, email, direccion, ciudad, cargo_id, area_id,
                        tipo_contrato_id, fecha_ingreso, fecha_retiro, salario, activo, creado_en)
                    VALUES (
                        p_id, p_tipo_identificacion_id, upper(trim(p_numero_identificacion)), p_nombres, p_apellidos,
                        p_genero, p_fecha_nacimiento, p_telefono, lower(p_email), p_direccion, p_ciudad, p_cargo_id,
                        p_area_id, p_tipo_contrato_id, p_fecha_ingreso, p_fecha_retiro, p_salario, p_activo, p_creado_en);
                $$;
                """);

            // p_actualizar_salario = false: quien edita no tiene empleados:ver-salario; se conserva el guardado.
            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_empleado_actualizar(
                    p_id uuid, p_tipo_identificacion_id uuid, p_numero_identificacion text, p_nombres text,
                    p_apellidos text, p_genero text, p_fecha_nacimiento date, p_telefono text, p_email text,
                    p_direccion text, p_ciudad text, p_cargo_id uuid, p_area_id uuid, p_tipo_contrato_id uuid,
                    p_fecha_ingreso date, p_fecha_retiro date, p_actualizar_salario boolean, p_salario numeric,
                    p_actualizado_en timestamptz)
                LANGUAGE sql AS $$
                    UPDATE empleados SET
                        tipo_identificacion_id = p_tipo_identificacion_id,
                        numero_identificacion = upper(trim(p_numero_identificacion)),
                        nombres = p_nombres,
                        apellidos = p_apellidos,
                        genero = p_genero,
                        fecha_nacimiento = p_fecha_nacimiento,
                        telefono = p_telefono,
                        email = lower(p_email),
                        direccion = p_direccion,
                        ciudad = p_ciudad,
                        cargo_id = p_cargo_id,
                        area_id = p_area_id,
                        tipo_contrato_id = p_tipo_contrato_id,
                        fecha_ingreso = p_fecha_ingreso,
                        fecha_retiro = p_fecha_retiro,
                        salario = CASE WHEN p_actualizar_salario THEN p_salario ELSE salario END,
                        actualizado_en = p_actualizado_en
                    WHERE id = p_id AND eliminado_en IS NULL;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_empleado_cambiar_estado(p_id uuid, p_activo boolean, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE empleados SET activo = p_activo, actualizado_en = p_fecha
                    WHERE id = p_id AND eliminado_en IS NULL;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_empleado_cambiar_foto(p_id uuid, p_foto_clave text, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE empleados SET foto_clave = p_foto_clave, actualizado_en = p_fecha
                    WHERE id = p_id AND eliminado_en IS NULL;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_empleado_eliminar(p_id uuid, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE empleados SET eliminado_en = p_fecha, actualizado_en = p_fecha
                    WHERE id = p_id AND eliminado_en IS NULL;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var objeto in new[]
                     {
                         "PROCEDURE sp_empleado_eliminar(uuid, timestamptz)",
                         "PROCEDURE sp_empleado_cambiar_foto(uuid, text, timestamptz)",
                         "PROCEDURE sp_empleado_cambiar_estado(uuid, boolean, timestamptz)",
                         "PROCEDURE sp_empleado_actualizar(uuid, uuid, text, text, text, text, date, text, text, text, text, uuid, uuid, uuid, date, date, boolean, numeric, timestamptz)",
                         "PROCEDURE sp_empleado_crear(uuid, uuid, text, text, text, text, date, text, text, text, text, uuid, uuid, uuid, date, date, numeric, boolean, timestamptz)",
                         "FUNCTION fn_empleado_identificacion_existe(uuid, text, uuid)",
                         "FUNCTION fn_empleado_por_id(uuid)",
                         "FUNCTION fn_empleados_listar(text)",
                         "PROCEDURE sp_maestro_eliminar(text, uuid, timestamptz)",
                         "PROCEDURE sp_maestro_actualizar(text, uuid, text, text, boolean, timestamptz)",
                         "PROCEDURE sp_maestro_crear(text, uuid, text, text, boolean, timestamptz)",
                         "FUNCTION fn_maestro_valor_existe(text, text, text, uuid)",
                         "FUNCTION fn_maestro_por_id(text, uuid)",
                         "FUNCTION fn_maestro_listar(text, boolean)",
                         "FUNCTION fn_maestro_tabla(text)",
                     })
            {
                migrationBuilder.Sql($"DROP {objeto.Split(' ')[0]} IF EXISTS {objeto[(objeto.IndexOf(' ') + 1)..]};");
            }

            // Los permisos sembrados se quedan: el Down de Administracion borra las tablas, y los
            // permisos huérfanos no conceden nada.
        }
    }
}
