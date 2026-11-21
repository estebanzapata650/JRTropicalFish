using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Esquema del catálogo: productos, grupos y cuencas (maestros), centros de acopio, listas de precios,
    /// matriz de precios, parámetros y proveedores. Revisada a mano: solo crea tablas; Cascade solo en las
    /// tablas puente y en precios_producto. Funciones y sembrado en <c>FuncionesCatalogos</c>.
    /// </summary>
    public partial class Catalogos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cuencas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    eliminado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cuencas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "grupos_producto",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    eliminado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_grupos_producto", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "listas_precios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    es_costo = table.Column<bool>(type: "boolean", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    lista_base_id = table.Column<Guid>(type: "uuid", nullable: true),
                    factor_salvaje = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    factor_criadero = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    factor_especial = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    aplica_impuestos = table.Column<bool>(type: "boolean", nullable: false),
                    redondeo = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_listas_precios", x => x.id);
                    table.CheckConstraint("ck_listas_base", "lista_base_id IS NULL OR lista_base_id <> id");
                    table.CheckConstraint("ck_listas_moneda", "moneda IN ('USD', 'COP')");
                    table.CheckConstraint("ck_listas_redondeo", "redondeo > 0");
                    table.ForeignKey(
                        name: "fk_listas_precios_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_listas_precios_listas_precios_lista_base_id",
                        column: x => x.lista_base_id,
                        principalTable: "listas_precios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parametros_precios",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    iva = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    pasarela = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    trm_referencia = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parametros_precios", x => x.id);
                    table.CheckConstraint("ck_parametros_precios_fila_unica", "id = 1");
                });

            migrationBuilder.CreateTable(
                name: "proveedores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tipo_identificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_identificacion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    telefono = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    contacto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    notas = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    eliminado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proveedores", x => x.id);
                    table.ForeignKey(
                        name: "fk_proveedores_tipos_identificacion_tipo_identificacion_id",
                        column: x => x.tipo_identificacion_id,
                        principalTable: "tipos_identificacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "centros_acopio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cuenca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    eliminado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_centros_acopio", x => x.id);
                    table.ForeignKey(
                        name: "fk_centros_acopio_cuencas_cuenca_id",
                        column: x => x.cuenca_id,
                        principalTable: "cuencas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "productos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    grupo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre_cientifico = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    nombre_comun = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    nombre_espanol = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    talla_min_cm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    talla_max_cm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    talla_etiqueta = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    bolsa_min = table.Column<int>(type: "integer", nullable: true),
                    bolsa_max = table.Column<int>(type: "integer", nullable: true),
                    disponibilidad = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notas = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    eliminado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_productos", x => x.id);
                    table.CheckConstraint("ck_productos_bolsa", "bolsa_min IS NULL OR bolsa_max IS NULL OR bolsa_min <= bolsa_max");
                    table.CheckConstraint("ck_productos_talla", "talla_min_cm IS NULL OR talla_max_cm IS NULL OR talla_min_cm <= talla_max_cm");
                    table.CheckConstraint("ck_productos_tipo", "tipo IN ('salvaje', 'criadero', 'especial')");
                    table.ForeignKey(
                        name: "fk_productos_grupos_producto_grupo_id",
                        column: x => x.grupo_id,
                        principalTable: "grupos_producto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proveedor_centros_acopio",
                columns: table => new
                {
                    proveedor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    centro_acopio_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proveedor_centros_acopio", x => new { x.proveedor_id, x.centro_acopio_id });
                    table.ForeignKey(
                        name: "fk_proveedor_centros_acopio_centros_acopio_centro_acopio_id",
                        column: x => x.centro_acopio_id,
                        principalTable: "centros_acopio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proveedor_centros_acopio_proveedores_proveedor_id",
                        column: x => x.proveedor_id,
                        principalTable: "proveedores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "precios_producto",
                columns: table => new
                {
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lista_precios_id = table.Column<Guid>(type: "uuid", nullable: false),
                    precio = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_precios_producto", x => new { x.producto_id, x.lista_precios_id });
                    table.CheckConstraint("ck_precios_positivo", "precio > 0");
                    table.ForeignKey(
                        name: "fk_precios_producto_listas_precios_lista_precios_id",
                        column: x => x.lista_precios_id,
                        principalTable: "listas_precios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_precios_producto_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "producto_centros_acopio",
                columns: table => new
                {
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    centro_acopio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_producto_centros_acopio", x => new { x.producto_id, x.centro_acopio_id });
                    table.CheckConstraint("ck_producto_centros_orden", "orden BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "fk_producto_centros_acopio_centros_acopio_centro_acopio_id",
                        column: x => x.centro_acopio_id,
                        principalTable: "centros_acopio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_producto_centros_acopio_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_centros_acopio_cuenca_id",
                table: "centros_acopio",
                column: "cuenca_id");

            migrationBuilder.CreateIndex(
                name: "ix_centros_acopio_nombre",
                table: "centros_acopio",
                column: "nombre",
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cuencas_codigo",
                table: "cuencas",
                column: "codigo",
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cuencas_nombre",
                table: "cuencas",
                column: "nombre",
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_grupos_producto_codigo",
                table: "grupos_producto",
                column: "codigo",
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_grupos_producto_nombre",
                table: "grupos_producto",
                column: "nombre",
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_listas_precios_cliente_id",
                table: "listas_precios",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_listas_precios_codigo",
                table: "listas_precios",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_listas_precios_lista_base_id",
                table: "listas_precios",
                column: "lista_base_id");

            migrationBuilder.CreateIndex(
                name: "ix_listas_precios_nombre",
                table: "listas_precios",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_precios_producto_lista_precios_id",
                table: "precios_producto",
                column: "lista_precios_id");

            migrationBuilder.CreateIndex(
                name: "ix_producto_centros_acopio_centro_acopio_id",
                table: "producto_centros_acopio",
                column: "centro_acopio_id");

            migrationBuilder.CreateIndex(
                name: "ix_productos_codigo",
                table: "productos",
                column: "codigo",
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_productos_grupo_id",
                table: "productos",
                column: "grupo_id");

            migrationBuilder.CreateIndex(
                name: "ix_proveedor_centros_acopio_centro_acopio_id",
                table: "proveedor_centros_acopio",
                column: "centro_acopio_id");

            migrationBuilder.CreateIndex(
                name: "ix_proveedores_tipo_identificacion_id_numero_identificacion",
                table: "proveedores",
                columns: new[] { "tipo_identificacion_id", "numero_identificacion" },
                unique: true,
                filter: "eliminado_en IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parametros_precios");

            migrationBuilder.DropTable(
                name: "precios_producto");

            migrationBuilder.DropTable(
                name: "producto_centros_acopio");

            migrationBuilder.DropTable(
                name: "proveedor_centros_acopio");

            migrationBuilder.DropTable(
                name: "listas_precios");

            migrationBuilder.DropTable(
                name: "productos");

            migrationBuilder.DropTable(
                name: "centros_acopio");

            migrationBuilder.DropTable(
                name: "proveedores");

            migrationBuilder.DropTable(
                name: "grupos_producto");

            migrationBuilder.DropTable(
                name: "cuencas");
        }
    }
}
