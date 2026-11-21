using JRTropicalFish.Domain.Seguridad;
using JRTropicalFish.Infrastructure.Persistence.Sembrado;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace JRTropicalFish.Tests.Seguridad;

public class SembradoPermisosTests
{
    private static List<string> SqlGenerado()
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        SembradoPermisos.Aplicar(builder);
        return [.. builder.Operations.OfType<SqlOperation>().Select(o => o.Sql)];
    }

    [Fact]
    public void Asignar_permisos_a_un_perfil_plantilla_revoca_las_sesiones_de_sus_usuarios()
    {
        var asignaciones = SqlGenerado().Where(s => s.Contains("INSERT INTO perfil_permisos", StringComparison.Ordinal)).ToList();

        // Un bloque por perfil plantilla con permisos, y todos revocan solo si hubo filas nuevas.
        Assert.Equal(PerfilesPlantilla.Definicion.Count(p => p.Value.Count > 0), asignaciones.Count);
        Assert.All(asignaciones, sql =>
        {
            Assert.Contains("ON CONFLICT DO NOTHING", sql, StringComparison.Ordinal);
            Assert.Contains("RETURNING perfil_id", sql, StringComparison.Ordinal);
            Assert.Contains("sesion_invalida_antes = now()", sql, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void El_catalogo_de_permisos_se_siembra_con_todos_los_codigos() =>
        Assert.All(Permisos.Todos.Keys, codigo => Assert.Contains(SqlGenerado(), sql => sql.Contains($"'{codigo}'", StringComparison.Ordinal)));
}
