using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EstandaresEmpaque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "empaque_bolsas",
                columns: table => new
                {
                    tipo_bolsa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    litros_agua = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_empaque_bolsas", x => x.tipo_bolsa_id);
                    table.CheckConstraint("ck_empaque_bolsas_litros", "litros_agua IS NULL OR (litros_agua > 0 AND litros_agua <= 50)");
                    table.ForeignKey(
                        name: "fk_empaque_bolsas_tipo_bolsa_tipo_bolsa_id",
                        column: x => x.tipo_bolsa_id,
                        principalTable: "tipos_bolsa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "empaque_costos_aditivo",
                columns: table => new
                {
                    medicamento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    costo_cop_unidad = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: true),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_empaque_costos_aditivo", x => x.medicamento_id);
                    table.CheckConstraint("ck_empaque_costos_aditivo", "costo_cop_unidad IS NULL OR costo_cop_unidad >= 0");
                    table.ForeignKey(
                        name: "fk_empaque_costos_aditivo_medicamentos_medicamento_id",
                        column: x => x.medicamento_id,
                        principalTable: "medicamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "estandares_empaque",
                columns: table => new
                {
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_bolsa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    peces_min = table.Column<int>(type: "integer", nullable: true),
                    peces_max = table.Column<int>(type: "integer", nullable: true),
                    litros_agua = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    notas = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    revisado = table.Column<bool>(type: "boolean", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_estandares_empaque", x => x.producto_id);
                    table.CheckConstraint("ck_estandares_empaque_litros", "litros_agua IS NULL OR (litros_agua > 0 AND litros_agua <= 50)");
                    table.CheckConstraint("ck_estandares_empaque_peces", "(peces_min IS NULL OR peces_min > 0) AND (peces_max IS NULL OR peces_max > 0) AND (peces_min IS NULL OR peces_max IS NULL OR peces_min <= peces_max)");
                    table.ForeignKey(
                        name: "fk_estandares_empaque_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_estandares_empaque_tipo_bolsa_tipo_bolsa_id",
                        column: x => x.tipo_bolsa_id,
                        principalTable: "tipos_bolsa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parametros_empaque",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    costo_heat_pack_cop = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    costo_oxigeno_bolsa_cop = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parametros_empaque", x => x.id);
                    table.CheckConstraint("ck_parametros_empaque_costos", "(costo_heat_pack_cop IS NULL OR costo_heat_pack_cop >= 0) AND (costo_oxigeno_bolsa_cop IS NULL OR costo_oxigeno_bolsa_cop >= 0)");
                    table.CheckConstraint("ck_parametros_empaque_fila_unica", "id = 1");
                });

            migrationBuilder.CreateTable(
                name: "estandar_empaque_aditivos",
                columns: table => new
                {
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    medicamento_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_estandar_empaque_aditivos", x => new { x.producto_id, x.medicamento_id });
                    table.ForeignKey(
                        name: "fk_estandar_empaque_aditivos_estandares_empaque_producto_id",
                        column: x => x.producto_id,
                        principalTable: "estandares_empaque",
                        principalColumn: "producto_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_estandar_empaque_aditivos_medicamentos_medicamento_id",
                        column: x => x.medicamento_id,
                        principalTable: "medicamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_estandar_empaque_aditivos_medicamento_id",
                table: "estandar_empaque_aditivos",
                column: "medicamento_id");

            migrationBuilder.CreateIndex(
                name: "ix_estandares_empaque_tipo_bolsa_id",
                table: "estandares_empaque",
                column: "tipo_bolsa_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "empaque_bolsas");

            migrationBuilder.DropTable(
                name: "empaque_costos_aditivo");

            migrationBuilder.DropTable(
                name: "estandar_empaque_aditivos");

            migrationBuilder.DropTable(
                name: "parametros_empaque");

            migrationBuilder.DropTable(
                name: "estandares_empaque");
        }
    }
}
