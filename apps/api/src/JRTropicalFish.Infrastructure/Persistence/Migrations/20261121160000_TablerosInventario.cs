using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Funciones de los tableros de inventario (bodega, proveedores, clientes y trazabilidad de lotes).
    /// Solo lectura: no cambian el esquema. Reemplazan los dos resúmenes del primer dashboard
    /// (<c>fn_inventario_despachos_por_producto</c> y <c>fn_inventario_compras_por_proveedor</c>; este
    /// último contaba doble lo pedido cuando una línea tenía varias recepciones).
    ///
    /// <para>El orden de columnas de cada RETURNS TABLE = el record en <c>Application/Inventario/TablerosModels.cs</c>.</para>
    /// </summary>
    public partial class TablerosInventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                -- Tiempos por lote a partir del kardex: cuánto estuvo en aclimatación (stock sin acuario) y en cuarentena
                -- (stock enfermo). Se suma el tiempo en que el saldo de ese estado fue > 0; si aún lo es, cuenta hasta p_ahora.
                -- Para lotes del saldo inicial, la aclimatación arranca en la fecha de ingreso del Excel (no en la migración).
                CREATE FUNCTION fn_lotes_tiempos(p_lote_id uuid, p_ahora timestamptz)
                RETURNS TABLE (lote_id uuid, dias_aclimatacion numeric, aclimatacion_en_curso boolean, dias_cuarentena numeric,
                               cuarentena_en_curso boolean, episodios_cuarentena integer, despachos integer, clientes integer,
                               primer_despacho timestamptz, ultimo_despacho timestamptz)
                LANGUAGE sql STABLE AS $$
                    WITH ev AS (
                        SELECT m.lote_id, m.id,
                               CASE WHEN m.tipo = 'saldo_inicial' AND m.acuario_destino_id IS NULL
                                    THEN least(m.fecha, l.fecha_ingreso::timestamp AT TIME ZONE 'America/Bogota') ELSE m.fecha END AS t,
                               (CASE WHEN m.condicion_destino IS NOT NULL AND m.acuario_destino_id IS NULL THEN m.cantidad ELSE 0 END)
                                 - (CASE WHEN m.condicion_origen IS NOT NULL AND m.acuario_origen_id IS NULL THEN m.cantidad ELSE 0 END) AS d_acl,
                               (CASE WHEN m.condicion_destino = 'enfermo' THEN m.cantidad ELSE 0 END)
                                 - (CASE WHEN m.condicion_origen = 'enfermo' THEN m.cantidad ELSE 0 END) AS d_enf
                        FROM movimientos_inventario m
                        JOIN lotes l ON l.id = m.lote_id
                        WHERE p_lote_id IS NULL OR m.lote_id = p_lote_id),
                    serie AS (
                        SELECT lote_id, id, t,
                               sum(d_acl) OVER w AS acl,
                               sum(d_enf) OVER w AS enf,
                               lead(t) OVER w AS t_sig
                        FROM ev
                        WINDOW w AS (PARTITION BY lote_id ORDER BY t, id ROWS UNBOUNDED PRECEDING)),
                    serie_previa AS (
                        SELECT s.*, lag(s.enf) OVER (PARTITION BY s.lote_id ORDER BY s.t, s.id) AS enf_previo FROM serie s),
                    tiempos AS (
                        SELECT lote_id,
                               sum(extract(epoch FROM coalesce(t_sig, p_ahora) - t)) FILTER (WHERE acl > 0) / 86400 AS dias_acl,
                               bool_or(t_sig IS NULL AND acl > 0) AS acl_curso,
                               sum(extract(epoch FROM coalesce(t_sig, p_ahora) - t)) FILTER (WHERE enf > 0) / 86400 AS dias_enf,
                               bool_or(t_sig IS NULL AND enf > 0) AS enf_curso,
                               count(*) FILTER (WHERE enf > 0 AND coalesce(enf_previo, 0) <= 0) AS episodios
                        FROM serie_previa
                        GROUP BY lote_id),
                    despachos AS (
                        SELECT m.lote_id, count(*) AS n, count(DISTINCT pc.cliente_id) AS clientes, min(m.fecha) AS primero, max(m.fecha) AS ultimo
                        FROM movimientos_inventario m
                        JOIN pedidos_cliente_lineas pcl ON pcl.id = m.pedido_cliente_linea_id
                        JOIN pedidos_cliente pc ON pc.id = pcl.pedido_id
                        WHERE m.tipo = 'despacho' AND (p_lote_id IS NULL OR m.lote_id = p_lote_id)
                        GROUP BY m.lote_id)
                    SELECT l.id, round(coalesce(t.dias_acl, 0)::numeric, 1), coalesce(t.acl_curso, false),
                           round(coalesce(t.dias_enf, 0)::numeric, 1), coalesce(t.enf_curso, false), coalesce(t.episodios, 0)::integer,
                           coalesce(d.n, 0)::integer, coalesce(d.clientes, 0)::integer, d.primero, d.ultimo
                    FROM lotes l
                    LEFT JOIN tiempos t ON t.lote_id = l.id
                    LEFT JOIN despachos d ON d.lote_id = l.id
                    WHERE p_lote_id IS NULL OR l.id = p_lote_id;
                $$;
                """);

            migrationBuilder.Sql(
                """
                -- Despachos (salidas a clientes) con su lote, cliente y pedido. Filtros opcionales por periodo y lote.
                CREATE FUNCTION fn_despachos_listar(p_desde timestamptz, p_hasta timestamptz, p_lote_id uuid)
                RETURNS TABLE (fecha timestamptz, lote_id uuid, lote_codigo text, producto_id uuid, producto_codigo text, nombre_cientifico text,
                               cliente_id uuid, cliente_nombre text, comercial_id uuid, pedido_id uuid, pedido_codigo text,
                               acuario_codigo text, cantidad integer)
                LANGUAGE sql STABLE AS $$
                    SELECT m.fecha, l.id, l.codigo::text, p.id, p.codigo::text, p.nombre_cientifico::text,
                           c.id, c.nombre_empresa::text, pc.comercial_id, pc.id, pc.codigo::text, a.codigo::text, m.cantidad
                    FROM movimientos_inventario m
                    JOIN lotes l ON l.id = m.lote_id
                    JOIN productos p ON p.id = l.producto_id
                    JOIN pedidos_cliente_lineas pcl ON pcl.id = m.pedido_cliente_linea_id
                    JOIN pedidos_cliente pc ON pc.id = pcl.pedido_id
                    JOIN clientes c ON c.id = pc.cliente_id
                    LEFT JOIN acuarios a ON a.id = m.acuario_origen_id
                    WHERE m.tipo = 'despacho'
                      AND (p_desde IS NULL OR m.fecha >= p_desde)
                      AND (p_hasta IS NULL OR m.fecha < p_hasta)
                      AND (p_lote_id IS NULL OR m.lote_id = p_lote_id)
                    ORDER BY m.fecha DESC;
                $$;
                """);

            migrationBuilder.Sql(
                """
                -- Líneas de pedidos a proveedor que siguen en camino (pedido enviado, en tránsito o recibido parcial).
                CREATE FUNCTION fn_compras_lineas_pendientes()
                RETURNS TABLE (pedido_id uuid, pedido_codigo text, proveedor_id uuid, proveedor_nombre text, fecha date,
                               fecha_estimada_llegada date, estado text, producto_id uuid, producto_codigo text, nombre_cientifico text,
                               centro_nombre text, cantidad integer, recibida integer)
                LANGUAGE sql STABLE AS $$
                    WITH centros AS (
                        SELECT DISTINCT ON (pca.producto_id) pca.producto_id, c.nombre
                        FROM producto_centros_acopio pca JOIN centros_acopio c ON c.id = pca.centro_acopio_id
                        ORDER BY pca.producto_id, pca.orden),
                    lineas AS (
                        SELECT pl.*, coalesce((SELECT sum(rl.recibida) FROM recepciones_lineas rl WHERE rl.pedido_linea_id = pl.id), 0)::integer AS recibida
                        FROM pedidos_proveedor_lineas pl)
                    SELECT pp.id, pp.codigo::text, pv.id, pv.nombre::text, pp.fecha, pp.fecha_estimada_llegada, pp.estado::text,
                           p.id, p.codigo::text, p.nombre_cientifico::text, ce.nombre::text, li.cantidad, li.recibida
                    FROM lineas li
                    JOIN pedidos_proveedor pp ON pp.id = li.pedido_id
                    JOIN proveedores pv ON pv.id = pp.proveedor_id
                    JOIN productos p ON p.id = li.producto_id
                    LEFT JOIN centros ce ON ce.producto_id = p.id
                    WHERE pp.estado IN ('enviado', 'en_transito', 'recibido_parcial') AND li.cantidad > li.recibida
                    ORDER BY coalesce(pp.fecha_estimada_llegada, pp.fecha), pp.codigo, p.codigo;
                $$;
                """);

            migrationBuilder.Sql(
                """
                -- Cumplimiento por pedido a proveedor del periodo (fecha del pedido). "En full": lo recibido hasta lo
                -- pedido, por línea; recibir de más no compensa lo que faltó en otra especie.
                CREATE FUNCTION fn_compras_cumplimiento(p_desde date, p_hasta date)
                RETURNS TABLE (pedido_id uuid, pedido_codigo text, proveedor_id uuid, proveedor_nombre text, fecha date, estado text,
                               pedidas integer, recibidas integer, recibidas_en_full integer)
                LANGUAGE sql STABLE AS $$
                    WITH lineas AS (
                        SELECT pl.pedido_id, pl.cantidad,
                               coalesce((SELECT sum(rl.recibida) FROM recepciones_lineas rl WHERE rl.pedido_linea_id = pl.id), 0) AS recibida
                        FROM pedidos_proveedor_lineas pl)
                    SELECT pp.id, pp.codigo::text, pv.id, pv.nombre::text, pp.fecha, pp.estado::text,
                           sum(li.cantidad)::integer, sum(li.recibida)::integer, sum(least(li.recibida, li.cantidad))::integer
                    FROM pedidos_proveedor pp
                    JOIN proveedores pv ON pv.id = pp.proveedor_id
                    JOIN lineas li ON li.pedido_id = pp.id
                    WHERE pp.fecha BETWEEN p_desde AND p_hasta AND pp.estado NOT IN ('borrador', 'cancelado')
                    GROUP BY pp.id, pv.id
                    ORDER BY pp.fecha DESC, pp.codigo DESC;
                $$;
                """);

            migrationBuilder.Sql(
                """
                -- Líneas de pedidos de cliente confirmados o en preparación que aún tienen pendiente por despachar,
                -- con el stock sano ubicado de la especie (¿se puede despachar ya?).
                CREATE FUNCTION fn_pedidos_cliente_lineas_pendientes()
                RETURNS TABLE (pedido_id uuid, pedido_codigo text, cliente_id uuid, cliente_nombre text, comercial_id uuid, fecha date,
                               estado text, producto_id uuid, producto_codigo text, nombre_cientifico text, cantidad integer, despachada integer,
                               stock_sano integer)
                LANGUAGE sql STABLE AS $$
                    WITH lineas AS (
                        SELECT l.*, coalesce((SELECT sum(m.cantidad) FROM movimientos_inventario m
                                              WHERE m.pedido_cliente_linea_id = l.id AND m.tipo = 'despacho'), 0)::integer AS despachada
                        FROM pedidos_cliente_lineas l)
                    SELECT pc.id, pc.codigo::text, c.id, c.nombre_empresa::text, pc.comercial_id, pc.fecha, pc.estado::text,
                           p.id, p.codigo::text, p.nombre_cientifico::text, li.cantidad, li.despachada,
                           coalesce((SELECT sum(e.cantidad) FROM existencias e JOIN lotes lo ON lo.id = e.lote_id
                                     WHERE lo.producto_id = p.id AND e.condicion = 'sano' AND e.acuario_id IS NOT NULL), 0)::integer
                    FROM lineas li
                    JOIN pedidos_cliente pc ON pc.id = li.pedido_id
                    JOIN clientes c ON c.id = pc.cliente_id
                    JOIN productos p ON p.id = li.producto_id
                    WHERE pc.estado IN ('confirmado', 'en_preparacion') AND li.cantidad > li.despachada
                    ORDER BY pc.fecha, pc.codigo, p.codigo;
                $$;
                """);

            migrationBuilder.Sql(
                """
                DROP FUNCTION fn_inventario_despachos_por_producto(timestamptz, timestamptz);
                DROP FUNCTION fn_inventario_compras_por_proveedor(date, date);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var funcion in new[]
                     {
                         "fn_lotes_tiempos", "fn_despachos_listar", "fn_compras_lineas_pendientes", "fn_compras_cumplimiento",
                         "fn_pedidos_cliente_lineas_pendientes",
                     })
            {
                migrationBuilder.Sql($"DROP FUNCTION IF EXISTS {funcion};");
            }

            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_inventario_despachos_por_producto(p_desde timestamptz, p_hasta timestamptz)
                RETURNS TABLE (producto_id uuid, producto_codigo text, nombre_cientifico text, cantidad integer)
                LANGUAGE sql STABLE AS $$
                    SELECT p.id, p.codigo::text, p.nombre_cientifico::text, sum(m.cantidad)::integer
                    FROM movimientos_inventario m
                    JOIN lotes l ON l.id = m.lote_id
                    JOIN productos p ON p.id = l.producto_id
                    WHERE m.tipo = 'despacho' AND m.fecha >= p_desde AND m.fecha < p_hasta
                    GROUP BY p.id
                    ORDER BY 4 DESC
                    LIMIT 10;
                $$;

                CREATE FUNCTION fn_inventario_compras_por_proveedor(p_desde date, p_hasta date)
                RETURNS TABLE (proveedor_id uuid, proveedor_nombre text, recepciones integer, unidades integer, costo_total numeric,
                               pedidas integer)
                LANGUAGE sql STABLE AS $$
                    SELECT pv.id, pv.nombre::text, count(DISTINCT r.id)::integer, sum(rl.recibida)::integer, sum(rl.costo_total),
                           sum(pl.cantidad)::integer
                    FROM recepciones r
                    JOIN recepciones_lineas rl ON rl.recepcion_id = r.id
                    JOIN pedidos_proveedor_lineas pl ON pl.id = rl.pedido_linea_id
                    JOIN pedidos_proveedor pp ON pp.id = r.pedido_proveedor_id
                    JOIN proveedores pv ON pv.id = pp.proveedor_id
                    WHERE r.fecha BETWEEN p_desde AND p_hasta
                    GROUP BY pv.id
                    ORDER BY 4 DESC;
                $$;
                """);
        }
    }
}
