using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Esquema del CRM comercial: clientes, oportunidades, ventas, tracking de vuelos, tareas, metas,
    /// parámetros y consecutivos. Revisada a mano: solo crea tablas nuevas; FK con Restrict, enums como
    /// texto con CHECK, únicos parciales sin eliminados y columnas generadas (total_cop, porcentaje_doa).
    /// Funciones y sembrado en <c>FuncionesCrm</c>.
    /// </summary>
    public partial class Crm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre_empresa = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contacto_principal = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    telefono_whatsapp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    pais = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ciudad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    aeropuerto_iata = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    tipo_cliente = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    licencia_importacion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    notas = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    eliminado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clientes", x => x.id);
                    table.CheckConstraint("ck_clientes_estado", "estado IN ('activo', 'inactivo', 'prospecto')");
                    table.CheckConstraint("ck_clientes_licencia", "licencia_importacion IN ('activa', 'en_tramite', 'vencida', 'no_aplica')");
                    table.CheckConstraint("ck_clientes_tipo", "tipo_cliente IN ('b2b_internacional', 'local_colombia', 'ecommerce')");
                });

            migrationBuilder.CreateTable(
                name: "consecutivos",
                columns: table => new
                {
                    prefijo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ultimo = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consecutivos", x => x.prefijo);
                });

            migrationBuilder.CreateTable(
                name: "metas_comerciales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    mes = table.Column<DateOnly>(type: "date", nullable: false),
                    comercial_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meta_cop = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_metas_comerciales", x => x.id);
                    table.CheckConstraint("ck_metas_mes", "extract(day FROM mes) = 1");
                    table.CheckConstraint("ck_metas_valor", "meta_cop >= 0");
                    table.ForeignKey(
                        name: "fk_metas_comerciales_usuarios_comercial_id",
                        column: x => x.comercial_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parametros_comerciales",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    meta_empresa_cop = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    meta_comercial_cop = table.Column<decimal>(type: "numeric(16,2)", precision: 16, scale: 2, nullable: false),
                    punto_equilibrio_despachos = table.Column<int>(type: "integer", nullable: false),
                    porcentaje_comision = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    umbral_doa = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    meta_clientes_nuevos = table.Column<int>(type: "integer", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parametros_comerciales", x => x.id);
                    table.CheckConstraint("ck_parametros_fila_unica", "id = 1");
                });

            migrationBuilder.CreateTable(
                name: "oportunidades",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_venta = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    comercial_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_contacto_inicial = table.Column<DateOnly>(type: "date", nullable: false),
                    etapa = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    monto_estimado = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    cajas_estimadas = table.Column<int>(type: "integer", nullable: false),
                    fecha_pago_confirmado = table.Column<DateOnly>(type: "date", nullable: true),
                    motivo_perdida = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    notas_seguimiento = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    etapa_cambiada_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    eliminado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oportunidades", x => x.id);
                    table.CheckConstraint("ck_oportunidades_etapa", "etapa IN ('prospeccion', 'stocklist', 'pre_invoice', 'aprobacion_vuelo', 'ganada', 'perdida')");
                    table.CheckConstraint("ck_oportunidades_moneda", "moneda IN ('USD', 'COP')");
                    table.CheckConstraint("ck_oportunidades_monto", "monto_estimado >= 0 AND cajas_estimadas >= 0");
                    table.CheckConstraint("ck_oportunidades_motivo_perdida", "(etapa = 'perdida') = (motivo_perdida IS NOT NULL AND btrim(motivo_perdida) <> '')");
                    table.CheckConstraint("ck_oportunidades_pago_ganada", "etapa <> 'ganada' OR fecha_pago_confirmado IS NOT NULL");
                    table.CheckConstraint("ck_oportunidades_tipo_venta", "tipo_venta IN ('exportacion', 'nacional')");
                    table.ForeignKey(
                        name: "fk_oportunidades_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_oportunidades_usuarios_comercial_id",
                        column: x => x.comercial_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tareas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    oportunidad_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    tipo_accion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    prioridad = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    comercial_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_asignacion = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_vencimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    notas = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    completada_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    eliminado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tareas", x => x.id);
                    table.CheckConstraint("ck_tareas_estado", "estado IN ('pendiente', 'en_curso', 'esperando_cliente', 'completada')");
                    table.CheckConstraint("ck_tareas_fechas", "fecha_vencimiento >= fecha_asignacion");
                    table.CheckConstraint("ck_tareas_prioridad", "prioridad IN ('alta', 'media', 'baja')");
                    table.CheckConstraint("ck_tareas_tipo_accion", "tipo_accion IN ('prospeccion', 'stocklist_fotos', 'pre_invoice', 'cobro_cartera', 'post_venta_doa')");
                    table.ForeignKey(
                        name: "fk_tareas_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tareas_oportunidades_oportunidad_id",
                        column: x => x.oportunidad_id,
                        principalTable: "oportunidades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tareas_usuarios_comercial_id",
                        column: x => x.comercial_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ventas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    oportunidad_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comercial_id = table.Column<Guid>(type: "uuid", nullable: false),
                    origen_canal = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    fecha_pedido = table.Column<DateOnly>(type: "date", nullable: false),
                    tipo_venta = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    trm = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    total_cop = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, computedColumnSql: "round(total * coalesce(trm, 1), 2)", stored: true),
                    total_cajas = table.Column<int>(type: "integer", nullable: false),
                    transportadora_aerolinea = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    no_guia_awb = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    estado_pago = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    estado_preparacion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ciudad_entrega = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    direccion_entrega = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    notas = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ventas", x => x.id);
                    table.CheckConstraint("ck_ventas_estado_pago", "estado_pago IN ('pendiente', 'anticipo', 'pagado')");
                    table.CheckConstraint("ck_ventas_estado_preparacion", "estado_preparacion IN ('pendiente_picking', 'en_empaque', 'listo_despacho', 'entregado')");
                    table.CheckConstraint("ck_ventas_moneda", "moneda IN ('USD', 'COP')");
                    table.CheckConstraint("ck_ventas_montos", "total >= 0 AND total_cajas >= 0");
                    table.CheckConstraint("ck_ventas_origen", "origen_canal IN ('b2b_directo', 'web_ecommerce', 'whatsapp_directo', 'mostrador_bodega')");
                    table.CheckConstraint("ck_ventas_tipo_venta", "tipo_venta IN ('exportacion', 'nacional')");
                    table.CheckConstraint("ck_ventas_trm", "(moneda = 'USD') = (trm IS NOT NULL AND trm > 0)");
                    table.ForeignKey(
                        name: "fk_ventas_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ventas_oportunidades_oportunidad_id",
                        column: x => x.oportunidad_id,
                        principalTable: "oportunidades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ventas_usuarios_comercial_id",
                        column: x => x.comercial_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tracking_vuelos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    venta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    oportunidad_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    n_factura_invoice = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    no_awb = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    aerolinea = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    fecha_despacho_vuelo = table.Column<DateOnly>(type: "date", nullable: true),
                    cajas_reales = table.Column<int>(type: "integer", nullable: false),
                    fase = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    monto_fob_usd = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    flete_aereo_usd = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    doa_reportado_usd = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    porcentaje_doa = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: true, computedColumnSql: "CASE WHEN monto_fob_usd > 0 THEN round(doa_reportado_usd / monto_fob_usd * 100, 2) END", stored: true),
                    nota_credito_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tracking_vuelos", x => x.id);
                    table.CheckConstraint("ck_tracking_fase", "fase IN ('cuarentena', 'empaque_o2', 'inspeccion_ica', 'vuelo_transito', 'arribo', 'control_doa')");
                    table.CheckConstraint("ck_tracking_montos", "cajas_reales >= 0 AND monto_fob_usd >= 0 AND flete_aereo_usd >= 0 AND doa_reportado_usd >= 0");
                    table.ForeignKey(
                        name: "fk_tracking_vuelos_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tracking_vuelos_oportunidades_oportunidad_id",
                        column: x => x.oportunidad_id,
                        principalTable: "oportunidades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tracking_vuelos_ventas_venta_id",
                        column: x => x.venta_id,
                        principalTable: "ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_clientes_codigo",
                table: "clientes",
                column: "codigo",
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_clientes_nombre_empresa",
                table: "clientes",
                column: "nombre_empresa",
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_metas_comerciales_comercial_id",
                table: "metas_comerciales",
                column: "comercial_id");

            migrationBuilder.CreateIndex(
                name: "ix_metas_comerciales_mes_comercial_id",
                table: "metas_comerciales",
                columns: new[] { "mes", "comercial_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_oportunidades_cliente_id",
                table: "oportunidades",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_oportunidades_codigo",
                table: "oportunidades",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_oportunidades_comercial_id",
                table: "oportunidades",
                column: "comercial_id");

            migrationBuilder.CreateIndex(
                name: "ix_oportunidades_etapa_comercial_id",
                table: "oportunidades",
                columns: new[] { "etapa", "comercial_id" });

            migrationBuilder.CreateIndex(
                name: "ix_tareas_cliente_id",
                table: "tareas",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_tareas_codigo",
                table: "tareas",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tareas_comercial_id_estado",
                table: "tareas",
                columns: new[] { "comercial_id", "estado" });

            migrationBuilder.CreateIndex(
                name: "ix_tareas_oportunidad_id",
                table: "tareas",
                column: "oportunidad_id");

            migrationBuilder.CreateIndex(
                name: "ix_tracking_vuelos_cliente_id",
                table: "tracking_vuelos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_tracking_vuelos_codigo",
                table: "tracking_vuelos",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tracking_vuelos_oportunidad_id",
                table: "tracking_vuelos",
                column: "oportunidad_id");

            migrationBuilder.CreateIndex(
                name: "ix_tracking_vuelos_venta_id",
                table: "tracking_vuelos",
                column: "venta_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ventas_cliente_id",
                table: "ventas",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_codigo",
                table: "ventas",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ventas_comercial_id",
                table: "ventas",
                column: "comercial_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_fecha_pedido",
                table: "ventas",
                column: "fecha_pedido");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_oportunidad_id",
                table: "ventas",
                column: "oportunidad_id",
                unique: true,
                filter: "oportunidad_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consecutivos");

            migrationBuilder.DropTable(
                name: "metas_comerciales");

            migrationBuilder.DropTable(
                name: "parametros_comerciales");

            migrationBuilder.DropTable(
                name: "tareas");

            migrationBuilder.DropTable(
                name: "tracking_vuelos");

            migrationBuilder.DropTable(
                name: "ventas");

            migrationBuilder.DropTable(
                name: "oportunidades");

            migrationBuilder.DropTable(
                name: "clientes");
        }
    }
}
