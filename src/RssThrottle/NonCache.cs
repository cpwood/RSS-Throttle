using System;
using System.Threading.Tasks;

namespace RssThrottle
{
    public class NonCache : ICache
    {
        public Task CacheAsync(Parameters parameters, DateTime cacheUntil, string rss)
        {
            return Task.CompletedTask;
        }

        public Task<string> GetFromCacheAsync(Parameters parameters)
        {
            return Task.FromResult((string) null);
        }
    }
}