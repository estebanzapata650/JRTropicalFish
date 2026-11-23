using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Aunap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_embarque_envios_tipo",
                table: "embarque_envios");

            migrationBuilder.DropCheckConstraint(
                name: "ck_embarque_documentos_tipo",
                table: "embarque_documentos");

            migrationBuilder.AddColumn<Guid>(
                name: "especie_aunap_id",
                table: "productos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "embarque_permisos_aunap",
                columns: table => new
                {
                    embarque_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    fecha = table.Column<DateOnly>(type: "date", nullable: true),
                    notas = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_embarque_permisos_aunap", x => x.embarque_id);
                    table.ForeignKey(
                        name: "fk_embarque_permisos_aunap_embarques_embarque_id",
                        column: x => x.embarque_id,
                        principalTable: "embarques",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "especies_aunap",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre_cientifico = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    nombre_comun = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    amazonia = table.Column<bool>(type: "boolean", nullable: false),
                    orinoquia = table.Column<bool>(type: "boolean", nullable: false),
                    magdalena = table.Column<bool>(type: "boolean", nullable: false),
                    pacifico = table.Column<bool>(type: "boolean", nullable: false),
                    caribe = table.Column<bool>(type: "boolean", nullable: false),
                    orden = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    familia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_especies_aunap", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "embarque_permiso_aunap_lineas",
                columns: table => new
                {
                    embarque_id = table.Column<Guid>(type: "uuid", nullable: false),
                    especie_aunap_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cantidad_autorizada = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_embarque_permiso_aunap_lineas", x => new { x.embarque_id, x.especie_aunap_id });
                    table.CheckConstraint("ck_permiso_aunap_lineas_cantidad", "cantidad_autorizada >= 0");
                    table.ForeignKey(
                        name: "fk_embarque_permiso_aunap_lineas_embarque_permisos_aunap_embar",
                        column: x => x.embarque_id,
                        principalTable: "embarque_permisos_aunap",
                        principalColumn: "embarque_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_embarque_permiso_aunap_lineas_especies_aunap_especie_aunap_",
                        column: x => x.especie_aunap_id,
                        principalTable: "especies_aunap",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_productos_especie_aunap_id",
                table: "productos",
                column: "especie_aunap_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_embarque_envios_tipo",
                table: "embarque_envios",
                sql: "tipo_documento IN ('preinvoice', 'flete', 'orden_cargue', 'empaque', 'factura', 'nota_credito', 'balance', 'solicitud_aunap', 'packing_aduana', 'factura_aduana')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_embarque_documentos_tipo",
                table: "embarque_documentos",
                sql: "tipo IN ('preinvoice', 'flete', 'orden_cargue', 'empaque', 'factura', 'nota_credito', 'balance', 'solicitud_aunap', 'packing_aduana', 'factura_aduana')");

            migrationBuilder.CreateIndex(
                name: "ix_embarque_permiso_aunap_lineas_especie_aunap_id",
                table: "embarque_permiso_aunap_lineas",
                column: "especie_aunap_id");

            migrationBuilder.CreateIndex(
                name: "ix_especies_aunap_codigo",
                table: "especies_aunap",
                column: "codigo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_productos_especies_aunap_especie_aunap_id",
                table: "productos",
                column: "especie_aunap_id",
                principalTable: "especies_aunap",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_productos_especies_aunap_especie_aunap_id",
                table: "productos");

            migrationBuilder.DropTable(
                name: "embarque_permiso_aunap_lineas");

            migrationBuilder.DropTable(
                name: "embarque_permisos_aunap");

            migrationBuilder.DropTable(
                name: "especies_aunap");

            migrationBuilder.DropIndex(
                name: "ix_productos_especie_aunap_id",
                table: "productos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_embarque_envios_tipo",
                table: "embarque_envios");

            migrationBuilder.DropCheckConstraint(
                name: "ck_embarque_documentos_tipo",
                table: "embarque_documentos");

            migrationBuilder.DropColumn(
                name: "especie_aunap_id",
                table: "productos");

            migrationBuilder.AddCheckConstraint(
                name: "ck_embarque_envios_tipo",
                table: "embarque_envios",
                sql: "tipo_documento IN ('preinvoice', 'flete', 'orden_cargue', 'empaque', 'factura', 'nota_credito', 'balance')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_embarque_documentos_tipo",
                table: "embarque_documentos",
                sql: "tipo IN ('preinvoice', 'flete', 'orden_cargue', 'empaque', 'factura', 'nota_credito', 'balance')");
        }
    }
}
