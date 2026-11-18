using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace JRTropicalFish.Infrastructure.Auth;

public class JwtTokenService(IOptions<JwtOptions> options, IReloj reloj) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public AuthResult GenerarToken(UsuarioSesion usuario)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Email, usuario.Email),
            new(JwtClaimTypes.Perfil, usuario.Perfil),
        };
        claims.AddRange(usuario.Permisos.Select(p => new Claim(JwtClaimTypes.Permiso, p)));

        var expiraEn = reloj.AhoraUtc.AddMinutes(_options.ExpiracionMinutos);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: reloj.AhoraUtc,
            expires: expiraEn,
            signingCredentials: credentials);

        return new AuthResult(new JwtSecurityTokenHandler().WriteToken(token), expiraEn, usuario);
    }
}
