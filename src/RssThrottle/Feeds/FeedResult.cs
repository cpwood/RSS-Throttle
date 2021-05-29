namespace RssThrottle.Feeds
{
    public class FeedResult
    {
        public string MimeType { get; }
        public string Content { get; }

        public FeedResult(string content)
        {
            Content = content;
            MimeType = content.Contains("<rss") ? "application/rss+xml" : "application/atom+xml";
        }
    }
}