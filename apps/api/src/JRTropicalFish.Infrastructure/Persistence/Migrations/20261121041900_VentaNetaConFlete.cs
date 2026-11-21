using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// La venta neta descuenta también el flete aéreo (decisión del negocio, 2026-11-21): la función
    /// del dashboard devuelve <c>flete_cop</c>. Cambia el <c>RETURNS TABLE</c>, así que es
    /// <c>DROP</c> + <c>CREATE</c> (<c>CREATE OR REPLACE</c> no puede cambiar las columnas). El orden de
    /// columnas coincide con <c>VentasComercialFila</c>.
    /// </summary>
    public partial class VentaNetaConFlete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fn_crm_ventas_por_comercial(date, date);");
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_crm_ventas_por_comercial(p_desde date, p_hasta date)
                RETURNS TABLE (comercial_id uuid, comercial_nombre text, ventas integer, ventas_cop numeric,
                               doa_cop numeric, flete_cop numeric)
                LANGUAGE sql STABLE AS $$
                    SELECT v.comercial_id, u.nombre::text, count(*)::integer, sum(v.total_cop),
                           coalesce(sum(t.doa_reportado_usd * coalesce(v.trm, 1)), 0),
                           coalesce(sum(t.flete_aereo_usd * coalesce(v.trm, 1)), 0)
                    FROM ventas v
                    JOIN usuarios u ON u.id = v.comercial_id
                    LEFT JOIN tracking_vuelos t ON t.venta_id = v.id
                    WHERE v.fecha_pedido BETWEEN p_desde AND p_hasta
                    GROUP BY v.comercial_id, u.nombre
                    ORDER BY u.nombre;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fn_crm_ventas_por_comercial(date, date);");
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_crm_ventas_por_comercial(p_desde date, p_hasta date)
                RETURNS TABLE (comercial_id uuid, comercial_nombre text, ventas integer, ventas_cop numeric, doa_cop numeric)
                LANGUAGE sql STABLE AS $$
                    SELECT v.comercial_id, u.nombre::text, count(*)::integer, sum(v.total_cop),
                           coalesce(sum(t.doa_reportado_usd * coalesce(v.trm, 1)), 0)
                    FROM ventas v
                    JOIN usuarios u ON u.id = v.comercial_id
                    LEFT JOIN tracking_vuelos t ON t.venta_id = v.id
                    WHERE v.fecha_pedido BETWEEN p_desde AND p_hasta
                    GROUP BY v.comercial_id, u.nombre
                    ORDER BY u.nombre;
                $$;
                """);
        }
    }
}
