using System;
using System.Threading.Tasks;
using RssThrottle.Feeds;

namespace RssThrottle.Caching
{
    public interface ICache
    {
        Task CacheAsync(Parameters parameters, DateTime cacheUntil, string rss);
        Task<string> GetFromCacheAsync(Parameters parameters);
    }
}