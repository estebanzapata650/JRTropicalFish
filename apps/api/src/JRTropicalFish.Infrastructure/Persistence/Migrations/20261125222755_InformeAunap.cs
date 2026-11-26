using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InformeAunap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "cites_fecha",
                table: "embarque_permisos_aunap",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cites_numero",
                table: "embarque_permisos_aunap",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dex_numero",
                table: "embarque_permisos_aunap",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "factura_dian",
                table: "embarque_permisos_aunap",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "proforma_fecha",
                table: "embarque_permisos_aunap",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "proforma_numero",
                table: "embarque_permisos_aunap",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "vuce_fecha_solicitud",
                table: "embarque_permisos_aunap",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "municipios_aunap",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    departamento = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    municipio = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    cuenca = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_municipios_aunap", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "parametros_aunap",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    representante_legal = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    resolucion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    vigencia_desde = table.Column<DateOnly>(type: "date", nullable: true),
                    vigencia_hasta = table.Column<DateOnly>(type: "date", nullable: true),
                    direccion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    correo = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    telefono = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parametros_aunap", x => x.id);
                    table.CheckConstraint("ck_parametros_aunap_fila_unica", "id = 1");
                });

            migrationBuilder.CreateTable(
                name: "centros_acopio_aunap",
                columns: table => new
                {
                    centro_acopio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    municipio_aunap_id = table.Column<int>(type: "integer", nullable: true),
                    excluir = table.Column<bool>(type: "boolean", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_centros_acopio_aunap", x => x.centro_acopio_id);
                    table.ForeignKey(
                        name: "fk_centros_acopio_aunap_centros_acopio_centro_acopio_id",
                        column: x => x.centro_acopio_id,
                        principalTable: "centros_acopio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_centros_acopio_aunap_municipios_aunap_municipio_aunap_id",
                        column: x => x.municipio_aunap_id,
                        principalTable: "municipios_aunap",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_embarque_permisos_aunap_proforma_numero",
                table: "embarque_permisos_aunap",
                column: "proforma_numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_centros_acopio_aunap_municipio_aunap_id",
                table: "centros_acopio_aunap",
                column: "municipio_aunap_id");

            migrationBuilder.CreateIndex(
                name: "ix_municipios_aunap_departamento_municipio",
                table: "municipios_aunap",
                columns: new[] { "departamento", "municipio" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "centros_acopio_aunap");

            migrationBuilder.DropTable(
                name: "parametros_aunap");

            migrationBuilder.DropTable(
                name: "municipios_aunap");

            migrationBuilder.DropIndex(
                name: "ix_embarque_permisos_aunap_proforma_numero",
                table: "embarque_permisos_aunap");

            migrationBuilder.DropColumn(
                name: "cites_fecha",
                table: "embarque_permisos_aunap");

            migrationBuilder.DropColumn(
                name: "cites_numero",
                table: "embarque_permisos_aunap");

            migrationBuilder.DropColumn(
                name: "dex_numero",
                table: "embarque_permisos_aunap");

            migrationBuilder.DropColumn(
                name: "factura_dian",
                table: "embarque_permisos_aunap");

            migrationBuilder.DropColumn(
                name: "proforma_fecha",
                table: "embarque_permisos_aunap");

            migrationBuilder.DropColumn(
                name: "proforma_numero",
                table: "embarque_permisos_aunap");

            migrationBuilder.DropColumn(
                name: "vuce_fecha_solicitud",
                table: "embarque_permisos_aunap");
        }
    }
}
