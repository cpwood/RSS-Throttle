using System.Threading.Tasks;
using CodeHollow.FeedReader;
using RssThrottle.Feeds;
using FeedReader = CodeHollow.FeedReader.FeedReader;

namespace Tests
{
    public class TestFeedReader : IFeedReader
    {
        private readonly string _content;

        public TestFeedReader(string content)
        {
            _content = content;
        }
        
        public Task<Feed> ReadAsync(string url)
        {
            return Task.FromResult(FeedReader.ReadFromString(_content));
        }
    }
}