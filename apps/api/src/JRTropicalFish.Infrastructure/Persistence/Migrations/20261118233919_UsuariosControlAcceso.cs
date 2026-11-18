using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UsuariosControlAcceso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "bloqueado_hasta",
                table: "usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "intentos_fallidos",
                table: "usuarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "sesion_invalida_antes",
                table: "usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ultimo_acceso_en",
                table: "usuarios",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bloqueado_hasta",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "intentos_fallidos",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "sesion_invalida_antes",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "ultimo_acceso_en",
                table: "usuarios");
        }
    }
}
