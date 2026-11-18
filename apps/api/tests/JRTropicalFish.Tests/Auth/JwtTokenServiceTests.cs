using System.IdentityModel.Tokens.Jwt;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auth;
using JRTropicalFish.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace JRTropicalFish.Tests.Auth;

public class JwtTokenServiceTests
{
    private sealed class RelojFijo(DateTime ahora) : IReloj
    {
        public DateTime AhoraUtc => ahora;
        public DateOnly Hoy => DateOnly.FromDateTime(ahora);
    }

    private static readonly DateTime Ahora = new(2026, 11, 18, 15, 0, 0, DateTimeKind.Utc);

    private static JwtTokenService Servicio() => new(
        Options.Create(new JwtOptions
        {
            Issuer = "JRTropicalFish",
            Audience = "JRTropicalFish",
            SigningKey = "clave-de-pruebas-de-al-menos-32-bytes-0123456789",
            ExpiracionMinutos = 60,
        }),
        new RelojFijo(Ahora));

    [Fact]
    public void El_token_lleva_un_claim_por_permiso_y_el_perfil()
    {
        var usuario = new UsuarioSesion(
            Guid.CreateVersion7(), "Ana", "ana@jrtf.local", "Operativo", ["inventario:leer", "inventario:ajustar"]);

        var resultado = Servicio().GenerarToken(usuario);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(resultado.Token);

        Assert.Equal(usuario.Id.ToString(), token.Subject);
        Assert.Equal("Operativo", token.Claims.Single(c => c.Type == JwtClaimTypes.Perfil).Value);
        Assert.Equal(
            ["inventario:leer", "inventario:ajustar"],
            token.Claims.Where(c => c.Type == JwtClaimTypes.Permiso).Select(c => c.Value));
    }

    [Fact]
    public void La_expiracion_sale_de_la_configuracion_y_del_reloj()
    {
        var resultado = Servicio().GenerarToken(new UsuarioSesion(Guid.CreateVersion7(), "Ana", "a@b.c", "Consulta", []));

        Assert.Equal(Ahora.AddMinutes(60), resultado.ExpiraEn);
    }
}
