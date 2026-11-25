using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Retira la lista «David Ramírez» (decisión del negocio): los clientes de Colombia usan MAYORISTA_NACIONAL.
    /// Los precios se van en cascada. Si algún registro la usa, se conserva.
    /// </summary>
    public partial class EliminarListaDavidRamirez : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM listas_precios l
                WHERE l.codigo = 'DAVID_RAMIREZ'
                  AND NOT EXISTS (SELECT 1 FROM listas_precios h WHERE h.lista_base_id = l.id)
                  AND NOT EXISTS (SELECT 1 FROM embarques e WHERE e.lista_precios_id = l.id)
                  AND NOT EXISTS (SELECT 1 FROM clientes_exportacion c WHERE c.lista_precios_id = l.id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Dato eliminado a propósito: no se recrea (se puede volver a crear desde Listas de precios).
        }
    }
}
