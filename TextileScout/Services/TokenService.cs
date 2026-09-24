using Microsoft.Extensions.Caching.Distributed;
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
        private readonly IDistributedCache _cache;

        public TokenService(IConfiguration configuration, IDistributedCache cache)
        {
            _configuration = configuration;
            _cache = cache;
        }

        // 1. KISA ÖMÜRLÜ ACCESS TOKEN (15 Dakika)
        public virtual string GenerateAccessToken(int userId, string username, string role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var jwtKey = _configuration["JwtSettings:Key"]
                         ?? throw new InvalidOperationException("JWT Secret Key (JwtSettings:Key) yapılandırmada bulunamadı!");

            var key = Encoding.UTF8.GetBytes(jwtKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, role)
                }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // 2. UZUN ÖMÜRLÜ REFRESH TOKEN ÜRETİMİ
        public virtual string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        // 3. REFRESH TOKEN'I REDIS'E KAYDETME (7 Gün Süreli)
        public virtual async Task SaveRefreshTokenAsync(int userId, string refreshToken)
        {
            string cacheKey = $"refresh_token:{userId}";
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
            };
            await _cache.SetStringAsync(cacheKey, refreshToken, options);
        }

        // 4. REDIS'TEKİ REFRESH TOKEN'I DOĞRULAMA
        public virtual async Task<bool> ValidateRefreshTokenAsync(int userId, string refreshToken)
        {
            string cacheKey = $"refresh_token:{userId}";
            var storedToken = await _cache.GetStringAsync(cacheKey);
            return storedToken != null && storedToken == refreshToken;
        }

        // 5. KULLANICI ÇIKIŞ YAPTIĞINDA (LOGOUT) REFRESH TOKEN'I SILME
        public virtual async Task RevokeRefreshTokenAsync(int userId)
        {
            string cacheKey = $"refresh_token:{userId}";
            await _cache.RemoveAsync(cacheKey);
        }
    }
}