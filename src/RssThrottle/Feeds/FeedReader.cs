using System.Threading.Tasks;
using CodeHollow.FeedReader;

namespace RssThrottle.Feeds
{
    public class FeedReader : IFeedReader
    {
        public Task<Feed> ReadAsync(string url)
        {
            return CodeHollow.FeedReader.FeedReader.ReadAsync(url);
        }
    }
}