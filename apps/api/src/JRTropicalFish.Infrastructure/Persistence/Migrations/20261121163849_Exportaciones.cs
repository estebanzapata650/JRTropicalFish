using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Exportaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cargos_exportacion",
                columns: table => new
                {
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    precio_usd = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cargos_exportacion", x => x.codigo);
                    table.CheckConstraint("ck_cargos_exportacion_codigo", "codigo IN ('zoosanitario', 'origen', 'cites', 'airport_fee', 'heat_pack')");
                    table.CheckConstraint("ck_cargos_exportacion_precio", "precio_usd >= 0");
                });

            migrationBuilder.CreateTable(
                name: "clientes_exportacion",
                columns: table => new
                {
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direccion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    eori = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    formato_factura = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    lista_precios_id = table.Column<Guid>(type: "uuid", nullable: true),
                    descuento_peces = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clientes_exportacion", x => x.cliente_id);
                    table.CheckConstraint("ck_clientes_exportacion_descuento", "descuento_peces BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_clientes_exportacion_formato", "formato_factura IN ('jrch', 'v2')");
                    table.ForeignKey(
                        name: "fk_clientes_exportacion_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_clientes_exportacion_listas_precios_lista_precios_id",
                        column: x => x.lista_precios_id,
                        principalTable: "listas_precios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "datos_empresa",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    razon_social = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    nit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    direccion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    ciudad_pais = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    telefono = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    sitio_web = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    banco = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    swift = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    cuenta_bancaria = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    beneficiario = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    direccion_banco = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    firmante_nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    firmante_cargo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    firmante_telefono = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    logo_clave = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_datos_empresa", x => x.id);
                    table.CheckConstraint("ck_datos_empresa_fila_unica", "id = 1");
                });

            migrationBuilder.CreateTable(
                name: "embarques",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    venta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pedido_cliente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comercial_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    lista_precios_id = table.Column<Guid>(type: "uuid", nullable: true),
                    descuento_peces = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    aerolinea = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ruta = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    awb = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    destino = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    salida_vuelo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    llegada_vuelo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fecha_despacho = table.Column<DateOnly>(type: "date", nullable: true),
                    preinvoice_numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    preinvoice_fecha = table.Column<DateOnly>(type: "date", nullable: true),
                    formato_factura = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    factura_numero = table.Column<int>(type: "integer", nullable: true),
                    factura_fecha = table.Column<DateOnly>(type: "date", nullable: true),
                    nota_credito_fecha = table.Column<DateOnly>(type: "date", nullable: true),
                    awb_valor = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    anticipo_recibido = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    tarifas = table.Column<string>(type: "jsonb", nullable: false),
                    cargos = table.Column<string>(type: "jsonb", nullable: false),
                    cajas_estimadas = table.Column<string>(type: "jsonb", nullable: false),
                    cajas_reales = table.Column<string>(type: "jsonb", nullable: false),
                    notas = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_embarques", x => x.id);
                    table.CheckConstraint("ck_embarques_estado", "estado IN ('borrador', 'preinvoice_enviada', 'confirmado', 'empacado', 'facturado', 'cerrado', 'cancelado')");
                    table.CheckConstraint("ck_embarques_formato", "formato_factura IN ('jrch', 'v2')");
                    table.CheckConstraint("ck_embarques_valores", "descuento_peces BETWEEN 0 AND 100 AND anticipo_recibido >= 0 AND (awb_valor IS NULL OR awb_valor >= 0)");
                    table.ForeignKey(
                        name: "fk_embarques_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_embarques_listas_precios_lista_precios_id",
                        column: x => x.lista_precios_id,
                        principalTable: "listas_precios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_embarques_pedidos_cliente_pedido_cliente_id",
                        column: x => x.pedido_cliente_id,
                        principalTable: "pedidos_cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_embarques_usuarios_comercial_id",
                        column: x => x.comercial_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_embarques_ventas_venta_id",
                        column: x => x.venta_id,
                        principalTable: "ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tarifas_flete",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    tarifa_kg_jr = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    tarifa_kg_cliente = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    fuel_kg = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    jp_fee_kg = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    vsc_kg = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    vsc_minimo = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    dbc_porcentaje = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    dbc_minimo = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    df_porcentaje = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    df_minimo = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    due_agent_jr = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    due_agent_cliente = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    due_carrier = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tarifas_flete", x => x.id);
                    table.CheckConstraint("ck_tarifas_flete_fila_unica", "id = 1");
                });

            migrationBuilder.CreateTable(
                name: "tipos_bolsa",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    tamano = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    porcion = table.Column<decimal>(type: "numeric(8,6)", precision: 8, scale: 6, nullable: false),
                    costo_cop = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tipos_bolsa", x => x.id);
                    table.CheckConstraint("ck_tipos_bolsa_valores", "porcion > 0 AND porcion <= 1 AND costo_cop >= 0");
                });

            migrationBuilder.CreateTable(
                name: "tipos_caja",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    descripcion_factura = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    costo_cop = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    precio_usd = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    kg_por_caja = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    pisos = table.Column<int>(type: "integer", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tipos_caja", x => x.id);
                    table.CheckConstraint("ck_tipos_caja_pisos", "pisos BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_tipos_caja_valores", "costo_cop >= 0 AND precio_usd >= 0 AND kg_por_caja >= 0");
                });

            migrationBuilder.CreateTable(
                name: "embarque_documentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    embarque_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    numero = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    archivo_clave = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    generado_por = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_embarque_documentos", x => x.id);
                    table.CheckConstraint("ck_embarque_documentos_tipo", "tipo IN ('preinvoice', 'flete', 'orden_cargue', 'empaque', 'factura', 'nota_credito', 'balance')");
                    table.ForeignKey(
                        name: "fk_embarque_documentos_embarques_embarque_id",
                        column: x => x.embarque_id,
                        principalTable: "embarques",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "embarque_envios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    embarque_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_documento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    para = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    asunto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    proveedor = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    exitoso = table.Column<bool>(type: "boolean", nullable: false),
                    llego_al_destinatario = table.Column<bool>(type: "boolean", nullable: false),
                    mensaje_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    enviado_por = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_embarque_envios", x => x.id);
                    table.CheckConstraint("ck_embarque_envios_tipo", "tipo_documento IN ('preinvoice', 'flete', 'orden_cargue', 'empaque', 'factura', 'nota_credito', 'balance')");
                    table.ForeignKey(
                        name: "fk_embarque_envios_embarques_embarque_id",
                        column: x => x.embarque_id,
                        principalTable: "embarques",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "embarque_lineas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    embarque_id = table.Column<Guid>(type: "uuid", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cantidad = table.Column<int>(type: "integer", nullable: false),
                    precio = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    fraccion_caja = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    empaque = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    nota = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_embarque_lineas", x => x.id);
                    table.CheckConstraint("ck_embarque_lineas_valores", "cantidad > 0 AND precio >= 0 AND fraccion_caja >= 0");
                    table.ForeignKey(
                        name: "fk_embarque_lineas_embarques_embarque_id",
                        column: x => x.embarque_id,
                        principalTable: "embarques",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_embarque_lineas_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "embarque_empaque",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    embarque_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_caja = table.Column<int>(type: "integer", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre_factura = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    tipo_bolsa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bolsas = table.Column<int>(type: "integer", nullable: false),
                    peces_por_bolsa = table.Column<int>(type: "integer", nullable: false),
                    precio = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    doa = table.Column<int>(type: "integer", nullable: false),
                    doa_adicional = table.Column<int>(type: "integer", nullable: false),
                    doa_final = table.Column<int>(type: "integer", nullable: false),
                    nota = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_embarque_empaque", x => x.id);
                    table.CheckConstraint("ck_embarque_empaque_doa", "doa >= 0 AND doa_adicional >= 0 AND doa_final >= 0 AND doa + doa_adicional + doa_final <= bolsas * peces_por_bolsa");
                    table.CheckConstraint("ck_embarque_empaque_valores", "numero_caja > 0 AND bolsas > 0 AND peces_por_bolsa > 0 AND precio >= 0");
                    table.ForeignKey(
                        name: "fk_embarque_empaque_embarques_embarque_id",
                        column: x => x.embarque_id,
                        principalTable: "embarques",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_embarque_empaque_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_embarque_empaque_tipos_bolsa_tipo_bolsa_id",
                        column: x => x.tipo_bolsa_id,
                        principalTable: "tipos_bolsa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_clientes_exportacion_lista_precios_id",
                table: "clientes_exportacion",
                column: "lista_precios_id");

            migrationBuilder.CreateIndex(
                name: "ix_embarque_documentos_embarque_id_tipo",
                table: "embarque_documentos",
                columns: new[] { "embarque_id", "tipo" });

            migrationBuilder.CreateIndex(
                name: "ix_embarque_empaque_embarque_id_numero_caja_orden",
                table: "embarque_empaque",
                columns: new[] { "embarque_id", "numero_caja", "orden" });

            migrationBuilder.CreateIndex(
                name: "ix_embarque_empaque_producto_id",
                table: "embarque_empaque",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "ix_embarque_empaque_tipo_bolsa_id",
                table: "embarque_empaque",
                column: "tipo_bolsa_id");

            migrationBuilder.CreateIndex(
                name: "ix_embarque_envios_embarque_id_creado_en",
                table: "embarque_envios",
                columns: new[] { "embarque_id", "creado_en" });

            migrationBuilder.CreateIndex(
                name: "ix_embarque_lineas_embarque_id_orden",
                table: "embarque_lineas",
                columns: new[] { "embarque_id", "orden" });

            migrationBuilder.CreateIndex(
                name: "ix_embarque_lineas_producto_id",
                table: "embarque_lineas",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "ix_embarques_cliente_id",
                table: "embarques",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_embarques_codigo",
                table: "embarques",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_embarques_comercial_id",
                table: "embarques",
                column: "comercial_id");

            migrationBuilder.CreateIndex(
                name: "ix_embarques_estado_comercial_id",
                table: "embarques",
                columns: new[] { "estado", "comercial_id" });

            migrationBuilder.CreateIndex(
                name: "ix_embarques_factura_numero",
                table: "embarques",
                column: "factura_numero",
                unique: true,
                filter: "factura_numero IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_embarques_lista_precios_id",
                table: "embarques",
                column: "lista_precios_id");

            migrationBuilder.CreateIndex(
                name: "ix_embarques_pedido_cliente_id",
                table: "embarques",
                column: "pedido_cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_embarques_venta_id",
                table: "embarques",
                column: "venta_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tipos_bolsa_nombre",
                table: "tipos_bolsa",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tipos_caja_nombre",
                table: "tipos_caja",
                column: "nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cargos_exportacion");

            migrationBuilder.DropTable(
                name: "clientes_exportacion");

            migrationBuilder.DropTable(
                name: "datos_empresa");

            migrationBuilder.DropTable(
                name: "embarque_documentos");

            migrationBuilder.DropTable(
                name: "embarque_empaque");

            migrationBuilder.DropTable(
                name: "embarque_envios");

            migrationBuilder.DropTable(
                name: "embarque_lineas");

            migrationBuilder.DropTable(
                name: "tarifas_flete");

            migrationBuilder.DropTable(
                name: "tipos_caja");

            migrationBuilder.DropTable(
                name: "tipos_bolsa");

            migrationBuilder.DropTable(
                name: "embarques");
        }
    }
}
