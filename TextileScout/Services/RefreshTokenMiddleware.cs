using Microsoft.EntityFrameworkCore;
using TextileScout.Web.Data;

namespace TextileScout.Web.Services
{
    public class RefreshTokenMiddleware
    {
        private readonly RequestDelegate _next;

        public RefreshTokenMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            var accessToken = context.Request.Cookies["X-Access-Token"];
            var refreshToken = context.Request.Cookies["X-Refresh-Token"];

            // Access Token yok veya geçersizse AMA Refresh Token varsa yenilemeyi dene
            if (string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken))
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var tokenService = scope.ServiceProvider.GetRequiredService<TokenService>();

                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

                if (user != null && user.RefreshTokenExpiryTime > DateTime.UtcNow)
                {
                    // Yeni Access Token ve Refresh Token üret
                    var newAccessToken = tokenService.GenerateAccessToken(user.Id, user.Username, user.Role);
                    var newRefreshToken = tokenService.GenerateRefreshToken();

                    user.RefreshToken = newRefreshToken;
                    user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                    await dbContext.SaveChangesAsync();

                    // Cookie'leri güncelle
                    context.Response.Cookies.Append("X-Access-Token", newAccessToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddMinutes(15)
                    });

                    context.Response.Cookies.Append("X-Refresh-Token", newRefreshToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddDays(7)
                    });

                    // Istek doğrulaması için Header'a yeni token'ı ekle
                    context.Request.Headers["Authorization"] = $"Bearer {newAccessToken}";
                }
            }

            await _next(context);
        }
    }
}