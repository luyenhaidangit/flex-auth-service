using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Flex.Infrastructures.Authentication
{
    public class TokenService : ITokenService
    {
        public string GenerateToken(JwtSettings settings, List<Claim> claims)
        {
            var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(settings.SecretKey)), settings.Algorithm);

            var token = new JwtSecurityToken(
                issuer: settings.Issuer,
                audience: settings.Audience,
                expires: DateTime.UtcNow.AddMinutes(settings.ExpiryInMinutes),
                claims: claims,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return tokenString;
        }
    }
}
