using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Pedidos de cliente: la línea trae la talla del producto del catálogo y el pedido suelto (sin venta) toma la
    /// moneda del país del cliente (Colombia = COP, los demás = USD), para no mostrar en pesos un precio STANDARD.
    /// </summary>
    public partial class PedidoTallaCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            CrearPedidos(migrationBuilder, "coalesce(v.moneda::text, CASE WHEN c.pais = 'Colombia' THEN 'COP' ELSE 'USD' END)");
            migrationBuilder.Sql(
                """
                DROP FUNCTION fn_pedido_cliente_lineas(uuid);
                CREATE FUNCTION fn_pedido_cliente_lineas(p_pedido_id uuid)
                RETURNS TABLE (id uuid, pedido_id uuid, producto_id uuid, producto_codigo text, nombre_cientifico text,
                               nombre_espanol text, cantidad integer, precio_unitario numeric, observacion text, despachada integer,
                               talla text)
                LANGUAGE sql STABLE AS $$
                    SELECT l.id, l.pedido_id, l.producto_id, p.codigo::text, p.nombre_cientifico::text, p.nombre_espanol::text,
                           l.cantidad, l.precio_unitario, l.observacion::text,
                           coalesce((SELECT sum(cantidad) FROM movimientos_inventario WHERE pedido_cliente_linea_id = l.id
                                     AND tipo = 'despacho'), 0)::integer,
                           CASE WHEN p.talla_min_cm IS NULL OR p.talla_max_cm IS NULL THEN p.talla_etiqueta::text
                                WHEN p.talla_min_cm = p.talla_max_cm
                                    THEN replace(trim_scale(p.talla_min_cm)::text, '.', ',') || ' cm'
                                ELSE replace(trim_scale(p.talla_min_cm)::text, '.', ',') || ' – '
                                     || replace(trim_scale(p.talla_max_cm)::text, '.', ',') || ' cm'
                           END
                    FROM pedidos_cliente_lineas l
                    JOIN productos p ON p.id = l.producto_id
                    WHERE l.pedido_id = p_pedido_id
                    ORDER BY p.codigo;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            CrearPedidos(migrationBuilder, "v.moneda::text");
            migrationBuilder.Sql(
                """
                DROP FUNCTION fn_pedido_cliente_lineas(uuid);
                CREATE FUNCTION fn_pedido_cliente_lineas(p_pedido_id uuid)
                RETURNS TABLE (id uuid, pedido_id uuid, producto_id uuid, producto_codigo text, nombre_cientifico text,
                               nombre_espanol text, cantidad integer, precio_unitario numeric, observacion text, despachada integer)
                LANGUAGE sql STABLE AS $$
                    SELECT l.id, l.pedido_id, l.producto_id, p.codigo::text, p.nombre_cientifico::text, p.nombre_espanol::text,
                           l.cantidad, l.precio_unitario, l.observacion::text,
                           coalesce((SELECT sum(cantidad) FROM movimientos_inventario WHERE pedido_cliente_linea_id = l.id
                                     AND tipo = 'despacho'), 0)::integer
                    FROM pedidos_cliente_lineas l
                    JOIN productos p ON p.id = l.producto_id
                    WHERE l.pedido_id = p_pedido_id
                    ORDER BY p.codigo;
                $$;
                """);
        }

        private static void CrearPedidos(MigrationBuilder migrationBuilder, string moneda)
        {
            var columnas =
                $"""
                RETURNS TABLE (id uuid, codigo text, cliente_id uuid, cliente_nombre text, comercial_id uuid, comercial_nombre text,
                               venta_id uuid, venta_codigo text, moneda text, fecha date, ciudad text, estado text,
                               observaciones text, unidades integer, despachadas integer, total numeric, creado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT pc.id, pc.codigo::text, pc.cliente_id, c.nombre_empresa::text, pc.comercial_id, u.nombre::text,
                           pc.venta_id, v.codigo::text, {moneda}, pc.fecha, pc.ciudad::text, pc.estado::text,
                           pc.observaciones::text,
                           coalesce((SELECT sum(cantidad) FROM pedidos_cliente_lineas WHERE pedido_id = pc.id), 0)::integer,
                           coalesce((SELECT sum(m.cantidad) FROM movimientos_inventario m
                                     JOIN pedidos_cliente_lineas l ON l.id = m.pedido_cliente_linea_id
                                     WHERE l.pedido_id = pc.id AND m.tipo = 'despacho'), 0)::integer,
                           (SELECT sum(cantidad * precio_unitario) FROM pedidos_cliente_lineas WHERE pedido_id = pc.id),
                           pc.creado_en
                    FROM pedidos_cliente pc
                    JOIN clientes c ON c.id = pc.cliente_id
                    JOIN usuarios u ON u.id = pc.comercial_id
                    LEFT JOIN ventas v ON v.id = pc.venta_id
                """;

            migrationBuilder.Sql(
                $"""
                CREATE OR REPLACE FUNCTION fn_pedidos_cliente_listar(p_comercial_id uuid)
                {columnas}
                    WHERE p_comercial_id IS NULL OR pc.comercial_id = p_comercial_id
                    ORDER BY pc.fecha DESC, pc.codigo DESC;
                $$;

                CREATE OR REPLACE FUNCTION fn_pedido_cliente_por_id(p_id uuid)
                {columnas}
                    WHERE pc.id = p_id;
                $$;

                CREATE OR REPLACE FUNCTION fn_pedido_cliente_por_venta(p_venta_id uuid)
                {columnas}
                    WHERE pc.venta_id = p_venta_id;
                $$;
                """);
        }
    }
}
