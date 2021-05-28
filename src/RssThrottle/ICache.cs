using System;
using System.Threading.Tasks;

namespace RssThrottle
{
    public interface ICache
    {
        Task CacheAsync(Parameters parameters, DateTime cacheUntil, string rss);
        Task<string> GetFromCacheAsync(Parameters parameters);
    }
}