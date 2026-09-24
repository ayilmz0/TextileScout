using Microsoft.Extensions.Caching.Distributed;
using System.Net;

namespace TextileScout.Web.Middlewares
{
    public class RateLimitationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDistributedCache _cache;

        public RateLimitationMiddleware(RequestDelegate next, IDistributedCache cache)
        {
            _next = next;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string cacheKey = $"rate_limit:{ipAddress}";

            var currentRequestCount = await _cache.GetStringAsync(cacheKey);
            int count = string.IsNullOrEmpty(currentRequestCount) ? 0 : int.Parse(currentRequestCount);

            // 1 dakika içinde maksimum 100 isteğe izin verilir
            if (count >= 100)
            {
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                await context.Response.WriteAsync("Çok fazla istek attınız. Lütfen 1 dakika sonra tekrar deneyiniz.");
                return;
            }

            count++;
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
            };
            await _cache.SetStringAsync(cacheKey, count.ToString(), options);

            await _next(context);
        }
    }
}