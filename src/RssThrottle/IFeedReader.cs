using System.Threading.Tasks;
using CodeHollow.FeedReader;

namespace RssThrottle
{
    public interface IFeedReader
    {
        Task<Feed> ReadAsync(string url);
    }
}