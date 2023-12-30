using Microsoft.Extensions.Caching.Distributed;

namespace BokurApi.RateLimiting
{
    public static class ExtensionMethods
    {
        public async static Task<byte[]?> GetCachedValueAsync(this IDistributedCache cache, string key, CancellationToken token = default(CancellationToken))
        {
            var result = await cache.GetAsync(key, token);
            return result;
        }

        public async static Task SetCachedValueAsync(this IDistributedCache cache, string key, byte[] value, CancellationToken token = default(CancellationToken))
        {
            await cache.SetAsync(key, value, token);
        }

        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitingMiddleware>();
        }
    }
}
