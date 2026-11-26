using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Empaque: estándar por producto (tipo de bolsa, peces por bolsa, litros de agua, aditivos), costos de empaque
    /// (litros por tipo de bolsa, costo de los aditivos, heat pack y oxígeno) y DOA por forma de empaque.
    /// </summary>
    public partial class FuncionesEmpaque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                -- Estándar por producto (especie y talla). Sin estándar se muestra el producto con los peces por bolsa del
                -- catálogo. Los aditivos van como texto (ids separados por coma): Dapper no mapea uuid[].
                CREATE FUNCTION fn_estandares_empaque()
                RETURNS TABLE (producto_id uuid, producto_codigo text, nombre_cientifico text, nombre_espanol text, talla text,
                               grupo_nombre text, bolsa_min integer, bolsa_max integer, tiene_estandar boolean, tipo_bolsa_id uuid,
                               tipo_bolsa_nombre text, peces_min integer, peces_max integer, litros_agua numeric, notas text,
                               revisado boolean, aditivos_texto text, actualizado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT p.id, p.codigo::text, p.nombre_cientifico::text, p.nombre_espanol::text,
                           CASE WHEN p.talla_min_cm IS NULL OR p.talla_max_cm IS NULL THEN p.talla_etiqueta::text
                                WHEN p.talla_min_cm = p.talla_max_cm THEN replace(trim_scale(p.talla_min_cm)::text, '.', ',') || ' cm'
                                ELSE replace(trim_scale(p.talla_min_cm)::text, '.', ',') || ' – '
                                     || replace(trim_scale(p.talla_max_cm)::text, '.', ',') || ' cm'
                           END,
                           g.nombre::text, p.bolsa_min, p.bolsa_max, e.producto_id IS NOT NULL, e.tipo_bolsa_id, b.nombre::text,
                           e.peces_min, e.peces_max, e.litros_agua, e.notas::text, coalesce(e.revisado, false),
                           (SELECT string_agg(a.medicamento_id::text, ',') FROM estandar_empaque_aditivos a WHERE a.producto_id = p.id),
                           e.actualizado_en
                    FROM productos p
                    JOIN grupos_producto g ON g.id = p.grupo_id
                    LEFT JOIN estandares_empaque e ON e.producto_id = p.id
                    LEFT JOIN tipos_bolsa b ON b.id = e.tipo_bolsa_id
                    WHERE p.eliminado_en IS NULL AND p.activo
                    ORDER BY p.codigo;
                $$;

                CREATE PROCEDURE sp_estandar_empaque_guardar(p_producto_id uuid, p_tipo_bolsa_id uuid, p_peces_min integer,
                                                             p_peces_max integer, p_litros numeric, p_notas text, p_revisado boolean,
                                                             p_aditivos jsonb, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    INSERT INTO estandares_empaque (producto_id, tipo_bolsa_id, peces_min, peces_max, litros_agua, notas, revisado, actualizado_en)
                    VALUES (p_producto_id, p_tipo_bolsa_id, p_peces_min, p_peces_max, p_litros, p_notas, p_revisado, p_fecha)
                    ON CONFLICT (producto_id) DO UPDATE SET tipo_bolsa_id = excluded.tipo_bolsa_id, peces_min = excluded.peces_min,
                        peces_max = excluded.peces_max, litros_agua = excluded.litros_agua, notas = excluded.notas,
                        revisado = excluded.revisado, actualizado_en = excluded.actualizado_en;
                    DELETE FROM estandar_empaque_aditivos WHERE producto_id = p_producto_id;
                    INSERT INTO estandar_empaque_aditivos (producto_id, medicamento_id)
                    SELECT p_producto_id, x::uuid FROM jsonb_array_elements_text(p_aditivos) AS x;
                END;
                $$;

                CREATE PROCEDURE sp_estandar_empaque_eliminar(p_producto_id uuid)
                LANGUAGE sql AS $$
                    DELETE FROM estandares_empaque WHERE producto_id = p_producto_id;
                $$;

                CREATE FUNCTION fn_parametros_empaque()
                RETURNS TABLE (costo_heat_pack_cop numeric, costo_oxigeno_bolsa_cop numeric, actualizado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT costo_heat_pack_cop, costo_oxigeno_bolsa_cop, actualizado_en FROM parametros_empaque WHERE id = 1;
                $$;

                CREATE PROCEDURE sp_parametros_empaque_guardar(p_heat_pack numeric, p_oxigeno numeric, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    INSERT INTO parametros_empaque (id, costo_heat_pack_cop, costo_oxigeno_bolsa_cop, actualizado_en)
                    VALUES (1, p_heat_pack, p_oxigeno, p_fecha)
                    ON CONFLICT (id) DO UPDATE SET costo_heat_pack_cop = excluded.costo_heat_pack_cop,
                        costo_oxigeno_bolsa_cop = excluded.costo_oxigeno_bolsa_cop, actualizado_en = excluded.actualizado_en;
                $$;

                CREATE FUNCTION fn_empaque_bolsas()
                RETURNS TABLE (tipo_bolsa_id uuid, nombre text, tamano text, porcion numeric, costo_cop numeric, litros_agua numeric, activo boolean)
                LANGUAGE sql STABLE AS $$
                    SELECT b.id, b.nombre::text, b.tamano::text, b.porcion, b.costo_cop, e.litros_agua, b.activo
                    FROM tipos_bolsa b LEFT JOIN empaque_bolsas e ON e.tipo_bolsa_id = b.id
                    ORDER BY b.orden, b.nombre;
                $$;

                CREATE PROCEDURE sp_empaque_bolsa_guardar(p_tipo_bolsa_id uuid, p_litros numeric, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    INSERT INTO empaque_bolsas (tipo_bolsa_id, litros_agua, actualizado_en) VALUES (p_tipo_bolsa_id, p_litros, p_fecha)
                    ON CONFLICT (tipo_bolsa_id) DO UPDATE SET litros_agua = excluded.litros_agua, actualizado_en = excluded.actualizado_en;
                $$;

                -- Aditivos posibles: los medicamentos que se dosifican por volumen de agua.
                CREATE FUNCTION fn_empaque_aditivos()
                RETURNS TABLE (medicamento_id uuid, codigo text, nombre text, unidad text, dosis_cantidad numeric, dosis_base numeric,
                               costo_cop_unidad numeric, fuente text)
                LANGUAGE sql STABLE AS $$
                    SELECT m.id, m.codigo::text, m.nombre::text, m.unidad::text, m.dosis_cantidad, m.dosis_base, c.costo_cop_unidad, m.fuente::text
                    FROM medicamentos m LEFT JOIN empaque_costos_aditivo c ON c.medicamento_id = m.id
                    WHERE m.activo AND m.tipo_dosis = 'por_volumen'
                    ORDER BY m.nombre;
                $$;

                CREATE PROCEDURE sp_empaque_aditivo_costo(p_medicamento_id uuid, p_costo numeric, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    INSERT INTO empaque_costos_aditivo (medicamento_id, costo_cop_unidad, actualizado_en) VALUES (p_medicamento_id, p_costo, p_fecha)
                    ON CONFLICT (medicamento_id) DO UPDATE SET costo_cop_unidad = excluded.costo_cop_unidad, actualizado_en = excluded.actualizado_en;
                $$;

                -- DOA por forma de empaque: filas del empaque real de embarques facturados o cerrados (ya volaron), por
                -- producto × tipo de bolsa × peces por bolsa. DOA = DOA + ADD + FINAL, como la nota crédito.
                CREATE FUNCTION fn_doa_empaque(p_desde date, p_hasta date)
                RETURNS TABLE (producto_id uuid, producto_codigo text, nombre_cientifico text, nombre_espanol text, talla text,
                               tipo_bolsa_id uuid, tipo_bolsa_nombre text, peces_por_bolsa integer, embarques integer, bolsas integer,
                               peces integer, doa integer)
                LANGUAGE sql STABLE AS $$
                    SELECT p.id, p.codigo::text, p.nombre_cientifico::text, p.nombre_espanol::text,
                           CASE WHEN p.talla_min_cm IS NULL OR p.talla_max_cm IS NULL THEN p.talla_etiqueta::text
                                WHEN p.talla_min_cm = p.talla_max_cm THEN replace(trim_scale(p.talla_min_cm)::text, '.', ',') || ' cm'
                                ELSE replace(trim_scale(p.talla_min_cm)::text, '.', ',') || ' – '
                                     || replace(trim_scale(p.talla_max_cm)::text, '.', ',') || ' cm'
                           END,
                           x.tipo_bolsa_id, b.nombre::text, x.peces_por_bolsa, count(DISTINCT x.embarque_id)::integer,
                           sum(x.bolsas)::integer, sum(x.bolsas * x.peces_por_bolsa)::integer,
                           sum(x.doa + x.doa_adicional + x.doa_final)::integer
                    FROM embarque_empaque x
                    JOIN embarques e ON e.id = x.embarque_id
                    JOIN productos p ON p.id = x.producto_id
                    LEFT JOIN tipos_bolsa b ON b.id = x.tipo_bolsa_id
                    WHERE e.estado IN ('facturado', 'cerrado')
                      AND coalesce(e.factura_fecha, e.fecha_despacho) BETWEEN p_desde AND p_hasta
                    GROUP BY p.id, p.codigo, p.nombre_cientifico, p.nombre_espanol, p.talla_min_cm, p.talla_max_cm, p.talla_etiqueta,
                             x.tipo_bolsa_id, b.nombre, x.peces_por_bolsa
                    ORDER BY p.codigo, b.nombre, x.peces_por_bolsa;
                $$;

                -- Costo del heat pack del libro «Formatos Comerciales» (hoja Parametros). El oxígeno queda por definir.
                INSERT INTO parametros_empaque (id, costo_heat_pack_cop, costo_oxigeno_bolsa_cop, actualizado_en)
                VALUES (1, 1470, NULL, now())
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var objeto in new[]
                     {
                         "FUNCTION fn_doa_empaque", "PROCEDURE sp_empaque_aditivo_costo", "FUNCTION fn_empaque_aditivos", "PROCEDURE sp_empaque_bolsa_guardar", "FUNCTION fn_empaque_bolsas", "PROCEDURE sp_parametros_empaque_guardar", "FUNCTION fn_parametros_empaque", "PROCEDURE sp_estandar_empaque_eliminar", "PROCEDURE sp_estandar_empaque_guardar", "FUNCTION fn_estandares_empaque",
                     })
            {
                var (tipo, nombre) = (objeto.Split(' ')[0], objeto.Split(' ')[1]);
                migrationBuilder.Sql($"DROP {tipo} IF EXISTS {nombre};");
            }

            migrationBuilder.Sql("DELETE FROM parametros_empaque;");
        }
    }
}
