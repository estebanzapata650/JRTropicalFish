using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Esquema de inventario, compras y pedidos de cliente (migrado de la app «BASES_TROPICAL FISH»).
    /// Revisada a mano: solo crea tablas; Cascade solo de las líneas a su encabezado; existencias con
    /// único NULLS NOT DISTINCT (acuario nulo = por ubicar) y CHECK cantidad >= 0; costo_total generado.
    /// Funciones en <c>FuncionesInventario</c>.
    /// </summary>
    public partial class Inventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "acuarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sector = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    nivel = table.Column<int>(type: "integer", nullable: false),
                    posicion = table.Column<int>(type: "integer", nullable: false),
                    capacidad_litros = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_acuarios", x => x.id);
                    table.CheckConstraint("ck_acuarios_capacidad", "capacidad_litros IS NULL OR capacidad_litros > 0");
                });

            migrationBuilder.CreateTable(
                name: "lotes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proveedor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    centro_acopio_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recepcion_linea_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_ingreso = table.Column<DateOnly>(type: "date", nullable: false),
                    costo_unitario = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    recibidos = table.Column<int>(type: "integer", nullable: false),
                    muertos_llegada = table.Column<int>(type: "integer", nullable: false),
                    origen = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lotes", x => x.id);
                    table.CheckConstraint("ck_lotes_cantidades", "recibidos >= 0 AND muertos_llegada >= 0 AND muertos_llegada <= recibidos");
                    table.CheckConstraint("ck_lotes_origen", "origen IN ('recepcion', 'saldo_inicial')");
                    table.ForeignKey(
                        name: "fk_lotes_centros_acopio_centro_acopio_id",
                        column: x => x.centro_acopio_id,
                        principalTable: "centros_acopio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lotes_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lotes_proveedores_proveedor_id",
                        column: x => x.proveedor_id,
                        principalTable: "proveedores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ofertas_proveedor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    proveedor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_estimada_llegada = table.Column<DateOnly>(type: "date", nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    observaciones = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ofertas_proveedor", x => x.id);
                    table.CheckConstraint("ck_ofertas_estado", "estado IN ('abierta', 'cerrada')");
                    table.ForeignKey(
                        name: "fk_ofertas_proveedor_proveedores_proveedor_id",
                        column: x => x.proveedor_id,
                        principalTable: "proveedores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pedidos_cliente",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comercial_id = table.Column<Guid>(type: "uuid", nullable: false),
                    venta_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    ciudad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    observaciones = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pedidos_cliente", x => x.id);
                    table.CheckConstraint("ck_pedidos_cliente_estado", "estado IN ('borrador', 'confirmado', 'en_preparacion', 'despachado', 'cancelado')");
                    table.ForeignKey(
                        name: "fk_pedidos_cliente_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pedidos_cliente_usuarios_comercial_id",
                        column: x => x.comercial_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pedidos_cliente_ventas_venta_id",
                        column: x => x.venta_id,
                        principalTable: "ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "existencias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lote_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    condicion = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    cantidad = table.Column<int>(type: "integer", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_existencias", x => x.id);
                    table.CheckConstraint("ck_existencias_condicion", "condicion IN ('sano', 'enfermo')");
                    table.CheckConstraint("ck_existencias_no_negativa", "cantidad >= 0");
                    table.ForeignKey(
                        name: "fk_existencias_acuarios_acuario_id",
                        column: x => x.acuario_id,
                        principalTable: "acuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_existencias_lotes_lote_id",
                        column: x => x.lote_id,
                        principalTable: "lotes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ofertas_proveedor_lineas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    oferta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cantidad = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ofertas_proveedor_lineas", x => x.id);
                    table.CheckConstraint("ck_ofertas_lineas_cantidad", "cantidad > 0");
                    table.ForeignKey(
                        name: "fk_ofertas_proveedor_lineas_ofertas_proveedor_oferta_id",
                        column: x => x.oferta_id,
                        principalTable: "ofertas_proveedor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ofertas_proveedor_lineas_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pedidos_proveedor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    proveedor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    oferta_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_estimada_llegada = table.Column<DateOnly>(type: "date", nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    observaciones = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pedidos_proveedor", x => x.id);
                    table.CheckConstraint("ck_pedidos_proveedor_estado", "estado IN ('borrador', 'enviado', 'en_transito', 'recibido_parcial', 'recibido', 'cancelado')");
                    table.ForeignKey(
                        name: "fk_pedidos_proveedor_ofertas_proveedor_oferta_id",
                        column: x => x.oferta_id,
                        principalTable: "ofertas_proveedor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pedidos_proveedor_proveedores_proveedor_id",
                        column: x => x.proveedor_id,
                        principalTable: "proveedores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pedidos_cliente_lineas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pedido_id = table.Column<Guid>(type: "uuid", nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cantidad = table.Column<int>(type: "integer", nullable: false),
                    precio_unitario = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    observacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pedidos_cliente_lineas", x => x.id);
                    table.CheckConstraint("ck_pedidos_cliente_lineas_cantidad", "cantidad > 0");
                    table.CheckConstraint("ck_pedidos_cliente_lineas_precio", "precio_unitario IS NULL OR precio_unitario >= 0");
                    table.ForeignKey(
                        name: "fk_pedidos_cliente_lineas_pedidos_cliente_pedido_id",
                        column: x => x.pedido_id,
                        principalTable: "pedidos_cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pedidos_cliente_lineas_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pedidos_proveedor_lineas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pedido_id = table.Column<Guid>(type: "uuid", nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cantidad = table.Column<int>(type: "integer", nullable: false),
                    costo_unitario = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    observacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pedidos_proveedor_lineas", x => x.id);
                    table.CheckConstraint("ck_pedidos_proveedor_lineas_cantidad", "cantidad > 0");
                    table.CheckConstraint("ck_pedidos_proveedor_lineas_costo", "costo_unitario IS NULL OR costo_unitario >= 0");
                    table.ForeignKey(
                        name: "fk_pedidos_proveedor_lineas_pedidos_proveedor_pedido_id",
                        column: x => x.pedido_id,
                        principalTable: "pedidos_proveedor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pedidos_proveedor_lineas_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recepciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    pedido_proveedor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    observaciones = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recepciones", x => x.id);
                    table.ForeignKey(
                        name: "fk_recepciones_pedidos_proveedor_pedido_proveedor_id",
                        column: x => x.pedido_proveedor_id,
                        principalTable: "pedidos_proveedor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recepciones_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recepciones_lineas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recepcion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pedido_linea_id = table.Column<Guid>(type: "uuid", nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recibida = table.Column<int>(type: "integer", nullable: false),
                    enferma = table.Column<int>(type: "integer", nullable: false),
                    muerta = table.Column<int>(type: "integer", nullable: false),
                    costo_unitario = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    costo_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, computedColumnSql: "recibida * costo_unitario", stored: true),
                    lote_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recepciones_lineas", x => x.id);
                    table.CheckConstraint("ck_recepciones_lineas_cantidades", "recibida > 0 AND enferma >= 0 AND muerta >= 0 AND enferma + muerta <= recibida AND costo_unitario >= 0");
                    table.ForeignKey(
                        name: "fk_recepciones_lineas_lotes_lote_id",
                        column: x => x.lote_id,
                        principalTable: "lotes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recepciones_lineas_pedidos_proveedor_lineas_pedido_linea_id",
                        column: x => x.pedido_linea_id,
                        principalTable: "pedidos_proveedor_lineas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recepciones_lineas_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recepciones_lineas_recepciones_recepcion_id",
                        column: x => x.recepcion_id,
                        principalTable: "recepciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimientos_inventario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    lote_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acuario_origen_id = table.Column<Guid>(type: "uuid", nullable: true),
                    condicion_origen = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    acuario_destino_id = table.Column<Guid>(type: "uuid", nullable: true),
                    condicion_destino = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    cantidad = table.Column<int>(type: "integer", nullable: false),
                    pedido_cliente_linea_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recepcion_linea_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nota = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    usuario_nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_movimientos_inventario", x => x.id);
                    table.CheckConstraint("ck_movimientos_cantidad", "cantidad > 0");
                    table.CheckConstraint("ck_movimientos_condiciones", "(condicion_origen IS NULL OR condicion_origen IN ('sano', 'enfermo')) AND (condicion_destino IS NULL OR condicion_destino IN ('sano', 'enfermo'))");
                    table.CheckConstraint("ck_movimientos_lado", "condicion_origen IS NOT NULL OR condicion_destino IS NOT NULL");
                    table.CheckConstraint("ck_movimientos_tipo", "tipo IN ('entrada', 'saldo_inicial', 'ubicacion', 'traslado', 'enfermo', 'aliviado', 'mortalidad', 'despacho', 'ajuste')");
                    table.ForeignKey(
                        name: "fk_movimientos_inventario_acuarios_acuario_destino_id",
                        column: x => x.acuario_destino_id,
                        principalTable: "acuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_movimientos_inventario_acuarios_acuario_origen_id",
                        column: x => x.acuario_origen_id,
                        principalTable: "acuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_movimientos_inventario_lotes_lote_id",
                        column: x => x.lote_id,
                        principalTable: "lotes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_movimientos_inventario_pedidos_cliente_lineas_pedido_client",
                        column: x => x.pedido_cliente_linea_id,
                        principalTable: "pedidos_cliente_lineas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_movimientos_inventario_recepciones_lineas_recepcion_linea_id",
                        column: x => x.recepcion_linea_id,
                        principalTable: "recepciones_lineas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_movimientos_inventario_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_acuarios_codigo",
                table: "acuarios",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_existencias_acuario_id",
                table: "existencias",
                column: "acuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_existencias_lote_id_acuario_id_condicion",
                table: "existencias",
                columns: new[] { "lote_id", "acuario_id", "condicion" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_lotes_centro_acopio_id",
                table: "lotes",
                column: "centro_acopio_id");

            migrationBuilder.CreateIndex(
                name: "ix_lotes_codigo",
                table: "lotes",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lotes_producto_id",
                table: "lotes",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "ix_lotes_proveedor_id",
                table: "lotes",
                column: "proveedor_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_inventario_acuario_destino_id",
                table: "movimientos_inventario",
                column: "acuario_destino_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_inventario_acuario_origen_id",
                table: "movimientos_inventario",
                column: "acuario_origen_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_inventario_lote_id_fecha",
                table: "movimientos_inventario",
                columns: new[] { "lote_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_inventario_pedido_cliente_linea_id",
                table: "movimientos_inventario",
                column: "pedido_cliente_linea_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_inventario_recepcion_linea_id",
                table: "movimientos_inventario",
                column: "recepcion_linea_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_inventario_tipo_fecha",
                table: "movimientos_inventario",
                columns: new[] { "tipo", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_inventario_usuario_id",
                table: "movimientos_inventario",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_ofertas_proveedor_codigo",
                table: "ofertas_proveedor",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ofertas_proveedor_proveedor_id",
                table: "ofertas_proveedor",
                column: "proveedor_id");

            migrationBuilder.CreateIndex(
                name: "ix_ofertas_proveedor_lineas_oferta_id_producto_id",
                table: "ofertas_proveedor_lineas",
                columns: new[] { "oferta_id", "producto_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ofertas_proveedor_lineas_producto_id",
                table: "ofertas_proveedor_lineas",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_cliente_cliente_id",
                table: "pedidos_cliente",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_cliente_codigo",
                table: "pedidos_cliente",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_cliente_comercial_id",
                table: "pedidos_cliente",
                column: "comercial_id");

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_cliente_estado_comercial_id",
                table: "pedidos_cliente",
                columns: new[] { "estado", "comercial_id" });

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_cliente_venta_id",
                table: "pedidos_cliente",
                column: "venta_id",
                unique: true,
                filter: "venta_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_cliente_lineas_pedido_id_producto_id",
                table: "pedidos_cliente_lineas",
                columns: new[] { "pedido_id", "producto_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_cliente_lineas_producto_id",
                table: "pedidos_cliente_lineas",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_proveedor_codigo",
                table: "pedidos_proveedor",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_proveedor_estado",
                table: "pedidos_proveedor",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_proveedor_oferta_id",
                table: "pedidos_proveedor",
                column: "oferta_id");

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_proveedor_proveedor_id",
                table: "pedidos_proveedor",
                column: "proveedor_id");

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_proveedor_lineas_pedido_id_producto_id",
                table: "pedidos_proveedor_lineas",
                columns: new[] { "pedido_id", "producto_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_proveedor_lineas_producto_id",
                table: "pedidos_proveedor_lineas",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "ix_recepciones_codigo",
                table: "recepciones",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_recepciones_pedido_proveedor_id",
                table: "recepciones",
                column: "pedido_proveedor_id");

            migrationBuilder.CreateIndex(
                name: "ix_recepciones_usuario_id",
                table: "recepciones",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_recepciones_lineas_lote_id",
                table: "recepciones_lineas",
                column: "lote_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_recepciones_lineas_pedido_linea_id",
                table: "recepciones_lineas",
                column: "pedido_linea_id");

            migrationBuilder.CreateIndex(
                name: "ix_recepciones_lineas_producto_id",
                table: "recepciones_lineas",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "ix_recepciones_lineas_recepcion_id",
                table: "recepciones_lineas",
                column: "recepcion_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "existencias");

            migrationBuilder.DropTable(
                name: "movimientos_inventario");

            migrationBuilder.DropTable(
                name: "ofertas_proveedor_lineas");

            migrationBuilder.DropTable(
                name: "acuarios");

            migrationBuilder.DropTable(
                name: "pedidos_cliente_lineas");

            migrationBuilder.DropTable(
                name: "recepciones_lineas");

            migrationBuilder.DropTable(
                name: "pedidos_cliente");

            migrationBuilder.DropTable(
                name: "lotes");

            migrationBuilder.DropTable(
                name: "pedidos_proveedor_lineas");

            migrationBuilder.DropTable(
                name: "recepciones");

            migrationBuilder.DropTable(
                name: "pedidos_proveedor");

            migrationBuilder.DropTable(
                name: "ofertas_proveedor");
        }
    }
}
