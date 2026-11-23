using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Funciones AUNAP: catálogo de especies autorizadas (Resolución 1924 de 2015), enlace de cada producto con su
    /// especie AUNAP y permiso AUNAP por embarque. Los documentos para autoridades se arman en C# con estos datos y
    /// siempre desde lo real (líneas y empaque del embarque).
    ///
    /// <para>El orden de columnas de cada RETURNS TABLE = el record en <c>Application/Exportaciones/AunapModels.cs</c>.</para>
    /// </summary>
    public partial class FuncionesAunap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_especies_aunap_listar()
                RETURNS TABLE (id uuid, codigo text, nombre_cientifico text, nombre_comun text, amazonia boolean, orinoquia boolean,
                               magdalena boolean, pacifico boolean, caribe boolean, orden text, familia text, activo boolean, productos integer)
                LANGUAGE sql STABLE AS $$
                    SELECT e.id, e.codigo::text, e.nombre_cientifico::text, e.nombre_comun::text, e.amazonia, e.orinoquia, e.magdalena,
                           e.pacifico, e.caribe, e.orden::text, e.familia::text, e.activo,
                           (SELECT count(*) FROM productos p WHERE p.especie_aunap_id = e.id AND p.eliminado_en IS NULL)::integer
                    FROM especies_aunap e
                    ORDER BY e.nombre_cientifico;
                $$;

                -- Carga o actualiza el listado por código (el de la resolución). Devuelve cuántas son nuevas y cuántas cambiaron.
                CREATE FUNCTION fn_especies_aunap_importar(p_filas jsonb, p_fecha timestamptz)
                RETURNS TABLE (nuevas integer, actualizadas integer)
                LANGUAGE plpgsql AS $$
                DECLARE
                    v_nuevas integer := 0;
                    v_actualizadas integer := 0;
                    f jsonb;
                BEGIN
                    FOR f IN SELECT * FROM jsonb_array_elements(p_filas) LOOP
                        IF EXISTS (SELECT 1 FROM especies_aunap WHERE codigo = f->>'codigo') THEN
                            UPDATE especies_aunap SET nombre_cientifico = f->>'nombreCientifico', nombre_comun = f->>'nombreComun',
                                   amazonia = (f->>'amazonia')::boolean, orinoquia = (f->>'orinoquia')::boolean,
                                   magdalena = (f->>'magdalena')::boolean, pacifico = (f->>'pacifico')::boolean,
                                   caribe = (f->>'caribe')::boolean, orden = f->>'orden', familia = f->>'familia', activo = true,
                                   actualizado_en = p_fecha
                            WHERE codigo = f->>'codigo'
                              AND (nombre_cientifico, coalesce(nombre_comun, ''), amazonia, orinoquia, magdalena, pacifico, caribe,
                                   coalesce(orden, ''), coalesce(familia, ''), activo)
                                  IS DISTINCT FROM
                                  (f->>'nombreCientifico', coalesce(f->>'nombreComun', ''), (f->>'amazonia')::boolean,
                                   (f->>'orinoquia')::boolean, (f->>'magdalena')::boolean, (f->>'pacifico')::boolean,
                                   (f->>'caribe')::boolean, coalesce(f->>'orden', ''), coalesce(f->>'familia', ''), true);
                            IF FOUND THEN v_actualizadas := v_actualizadas + 1; END IF;
                        ELSE
                            INSERT INTO especies_aunap (id, codigo, nombre_cientifico, nombre_comun, amazonia, orinoquia, magdalena, pacifico,
                                                        caribe, orden, familia, activo, creado_en)
                            VALUES ((f->>'id')::uuid, f->>'codigo', f->>'nombreCientifico', f->>'nombreComun', (f->>'amazonia')::boolean,
                                    (f->>'orinoquia')::boolean, (f->>'magdalena')::boolean, (f->>'pacifico')::boolean,
                                    (f->>'caribe')::boolean, f->>'orden', f->>'familia', true, p_fecha);
                            v_nuevas := v_nuevas + 1;
                        END IF;
                    END LOOP;
                    RETURN QUERY SELECT v_nuevas, v_actualizadas;
                END;
                $$;

                CREATE FUNCTION fn_productos_aunap()
                RETURNS TABLE (producto_id uuid, producto_codigo text, nombre_cientifico text, nombre_comun text, grupo_nombre text,
                               activo boolean, especie_aunap_id uuid, aunap_codigo text, aunap_nombre_cientifico text,
                               aunap_nombre_comun text, aunap_familia text)
                LANGUAGE sql STABLE AS $$
                    SELECT p.id, p.codigo::text, p.nombre_cientifico::text, coalesce(p.nombre_espanol, p.nombre_comun)::text, g.nombre::text,
                           p.activo, e.id, e.codigo::text, e.nombre_cientifico::text, e.nombre_comun::text, e.familia::text
                    FROM productos p
                    LEFT JOIN grupos_producto g ON g.id = p.grupo_id
                    LEFT JOIN especies_aunap e ON e.id = p.especie_aunap_id
                    WHERE p.eliminado_en IS NULL
                    ORDER BY p.codigo;
                $$;

                CREATE PROCEDURE sp_producto_aunap(p_producto_id uuid, p_especie_aunap_id uuid, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE productos SET especie_aunap_id = p_especie_aunap_id, actualizado_en = p_fecha WHERE id = p_producto_id;
                $$;

                -- Enlace automático solo cuando el nombre científico es idéntico (sin mayúsculas ni espacios de más) y hay una sola
                -- especie con ese nombre. Los sinónimos taxonómicos se enlazan a mano.
                CREATE FUNCTION fn_productos_aunap_enlazar(p_fecha timestamptz)
                RETURNS integer
                LANGUAGE plpgsql AS $$
                DECLARE
                    v_total integer;
                BEGIN
                    WITH unicas AS (
                        SELECT lower(regexp_replace(trim(nombre_cientifico), '\s+', ' ', 'g')) AS nombre, min(id::text)::uuid AS id
                        FROM especies_aunap WHERE activo
                        GROUP BY 1 HAVING count(*) = 1)
                    UPDATE productos p SET especie_aunap_id = u.id, actualizado_en = p_fecha
                    FROM unicas u
                    WHERE p.especie_aunap_id IS NULL AND p.eliminado_en IS NULL
                      AND lower(regexp_replace(trim(p.nombre_cientifico), '\s+', ' ', 'g')) = u.nombre;
                    GET DIAGNOSTICS v_total = ROW_COUNT;
                    RETURN v_total;
                END;
                $$;

                CREATE FUNCTION fn_permiso_aunap(p_embarque_id uuid)
                RETURNS TABLE (numero text, fecha date, notas text, actualizado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT numero::text, fecha, notas::text, actualizado_en FROM embarque_permisos_aunap WHERE embarque_id = p_embarque_id;
                $$;

                CREATE FUNCTION fn_permiso_aunap_lineas(p_embarque_id uuid)
                RETURNS TABLE (especie_aunap_id uuid, cantidad_autorizada integer)
                LANGUAGE sql STABLE AS $$
                    SELECT especie_aunap_id, cantidad_autorizada FROM embarque_permiso_aunap_lineas WHERE embarque_id = p_embarque_id;
                $$;

                CREATE PROCEDURE sp_permiso_aunap_guardar(p_embarque_id uuid, p_numero text, p_fecha date, p_notas text, p_lineas jsonb,
                                                          p_actualizado timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    INSERT INTO embarque_permisos_aunap (embarque_id, numero, fecha, notas, actualizado_en)
                    VALUES (p_embarque_id, p_numero, p_fecha, p_notas, p_actualizado)
                    ON CONFLICT (embarque_id) DO UPDATE SET numero = excluded.numero, fecha = excluded.fecha, notas = excluded.notas,
                        actualizado_en = excluded.actualizado_en;

                    DELETE FROM embarque_permiso_aunap_lineas WHERE embarque_id = p_embarque_id;
                    INSERT INTO embarque_permiso_aunap_lineas (embarque_id, especie_aunap_id, cantidad_autorizada)
                    SELECT p_embarque_id, (x->>'especie')::uuid, (x->>'cantidad')::integer
                    FROM jsonb_array_elements(p_lineas) AS x;
                END;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var objeto in new[]
                     {
                         "PROCEDURE sp_permiso_aunap_guardar", "FUNCTION fn_permiso_aunap_lineas", "FUNCTION fn_permiso_aunap",
                         "FUNCTION fn_productos_aunap_enlazar", "PROCEDURE sp_producto_aunap", "FUNCTION fn_productos_aunap",
                         "FUNCTION fn_especies_aunap_importar", "FUNCTION fn_especies_aunap_listar",
                     })
            {
                var (tipo, nombre) = (objeto.Split(' ')[0], objeto.Split(' ')[1]);
                migrationBuilder.Sql($"DROP {tipo} IF EXISTS {nombre};");
            }
        }
    }
}
