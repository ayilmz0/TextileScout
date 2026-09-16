using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace TextileScout.Web.Services
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // 1. KISA ÖMÜRLÜ ACCESS TOKEN (15 Dakika)
        public string GenerateAccessToken(int userId, string username, string role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // appsettings.json veya User Secrets içerisinden anahtarı güvenle okur
            var jwtKey = _configuration["JwtSettings:Key"]
                         ?? throw new InvalidOperationException("JWT Secret Key (JwtSettings:Key) yapılandırmada bulunamadı!");

            var key = Encoding.UTF8.GetBytes(jwtKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()), // USER ID CLAIM
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, role)
                }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // 2. UZUN ÖMÜRLÜ REFRESH TOKEN (Rastgele Güvenli String)
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}