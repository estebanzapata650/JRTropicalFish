using JRTropicalFish.Application.Seguridad;

namespace JRTropicalFish.Tests.Seguridad;

public class ReglasPasswordTests
{
    [Theory]
    [InlineData("Clave123")]
    [InlineData("peces tropicales 2026")]
    public void Acepta_claves_de_ocho_o_mas_con_letras_y_numeros(string password) =>
        Assert.Null(ReglasPassword.Validar(password));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Ab1")]
    [InlineData("solamenteletras")]
    [InlineData("1234567890")]
    public void Rechaza_claves_cortas_o_sin_mezcla(string? password) =>
        Assert.NotNull(ReglasPassword.Validar(password));

    [Fact]
    public void El_bloqueo_llega_al_alcanzar_el_maximo_de_intentos()
    {
        var ahora = new DateTime(2026, 11, 18, 15, 0, 0, DateTimeKind.Utc);

        Assert.Null(ReglasBloqueo.BloqueoTrasFallo(ReglasBloqueo.IntentosMaximos - 2, ahora));
        Assert.Equal(ahora + ReglasBloqueo.DuracionBloqueo, ReglasBloqueo.BloqueoTrasFallo(ReglasBloqueo.IntentosMaximos - 1, ahora));
    }

    [Fact]
    public void Los_minutos_restantes_se_redondean_hacia_arriba()
    {
        var ahora = new DateTime(2026, 11, 18, 15, 0, 0, DateTimeKind.Utc);
        Assert.Equal(15, ReglasBloqueo.MinutosRestantes(ahora.AddMinutes(14).AddSeconds(1), ahora));
        Assert.Equal(1, ReglasBloqueo.MinutosRestantes(ahora.AddSeconds(5), ahora));
    }
}
