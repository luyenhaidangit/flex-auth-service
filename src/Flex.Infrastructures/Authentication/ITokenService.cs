using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Flex.Infrastructures.Authentication
{
    public interface ITokenService
    {
        string GenerateToken(JwtSettings settings, List<Claim> claims);
    }
}
