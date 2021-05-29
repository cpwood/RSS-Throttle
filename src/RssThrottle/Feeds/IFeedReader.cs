using System.Threading.Tasks;
using CodeHollow.FeedReader;

namespace RssThrottle.Feeds
{
    public interface IFeedReader
    {
        Task<Feed> ReadAsync(string url);
    }
}