using StackExchange.Redis;

namespace TextileScout.Web.Services
{
    public class RedisQueueService
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisQueueService(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        // Scraper tarafından yakalanan görsel URL'ini Python'ın okuyacağı kuyruğa atar
        public async Task EnqueueImageForAiProcessingAsync(string imageUrl, string sourceSite)
        {
            var db = _redis.GetDatabase();
            string payload = $"{{\"imageUrl\":\"{imageUrl}\", \"sourceSite\":\"{sourceSite}\"}}";

            // "textile:image:queue" isimli Redis kuyruğuna push eder
            await db.ListLeftPushAsync("textile:image:queue", payload);
        }
    }
}