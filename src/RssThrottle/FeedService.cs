using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeHollow.FeedReader;
using WilderMinds.RssSyndication;
using Feed = WilderMinds.RssSyndication.Feed;

namespace RssThrottle
{
    public class FeedService
    {
        private readonly ICache _cache;
        private readonly IFeedReader _feedReader;
        private readonly IDateTimeProvider _provider;

        public FeedService(ICache cache = null)
        {
            _cache = cache ?? new NonCache();
            _feedReader = new FeedReader();
            _provider = new DateTimeProvider();
        }
        
        internal FeedService(
            ICache cache,
            IFeedReader feedReader,
            IDateTimeProvider provider)
        {
            _cache = cache ?? new NonCache();
            _feedReader = feedReader;
            _provider = provider;
        }
        
        public async Task<string> ProcessAsync(Parameters parameters)
        {
            if (parameters.Mode == Parameters.Modes.Delay)
            {
                var rss = await _cache.GetFromCacheAsync(parameters);
                if (!string.IsNullOrEmpty(rss))
                    return rss;
            }

            var nextDelay = DateTime.MinValue;
            var inputFeed = await _feedReader.ReadAsync(parameters.Url);
            
            var outputFeed = new Feed
            {
                Title = inputFeed.Title,
                Description = inputFeed.Description,
                Link = new Uri(inputFeed.Link),
                Copyright = inputFeed.Copyright,
                Language = inputFeed.Language
            };

            FeedItem[] items;
            
            if (parameters.Mode == Parameters.Modes.Delay)
            {
                var whenDays = WhenParser.Unpack(parameters.When, Parameters.Modes.Delay);
                var delayUntil = GetLastDelayEndUtc(whenDays, parameters.Timezone);
                nextDelay = GetNextDelayEndUtc(whenDays, parameters.Timezone);
                items = inputFeed.Items
                    .Where(x => x.PublishingDate != null && x.PublishingDate.Value <= delayUntil)
                    .ToArray();
            }
            else if (parameters.Mode == Parameters.Modes.Include)
            {
                var whenDays = WhenParser.Unpack(parameters.When, Parameters.Modes.Include);
                items = inputFeed.Items.Where(x =>
                        x.PublishingDate != null &&
                        IsWithinWindow(x.PublishingDate.Value, whenDays, parameters.Timezone))
                    .ToArray();
            }
            else
            {
                var whenDays = WhenParser.Unpack(parameters.When, Parameters.Modes.Exclude);
                items = inputFeed.Items.Where(x =>
                        x.PublishingDate != null &&
                        !IsWithinWindow(x.PublishingDate.Value, whenDays, parameters.Timezone))
                    .ToArray();
            }
            
            items = FilterAndLimit(items, parameters);

            foreach (var item in items)
            {
                outputFeed.Items.Add(new Item
                {
                    Title = item.Title,
                    Link = new Uri(item.Link),
                    Body = item.Description ?? item.Content,
                    Categories = item.Categories,
                    PublishDate = item.PublishingDate ?? DateTime.UtcNow,
                    Guid = item.Id,
                    Author =  !string.IsNullOrEmpty(item.Author) ? new Author
                    {
                        Name = item.Author
                    } : null,
                    FullHtmlContent = item.Description ?? item.Content,
                    Permalink = item.Link
                });
            }

            var result = outputFeed.Serialize(new SerializeOption
            {
                Encoding = Encoding.UTF8
            });

            if (parameters.Mode == Parameters.Modes.Delay)
                await _cache.CacheAsync(parameters, nextDelay, result);
            
            return result;
        }

        internal DateTime GetLastDelayEndUtc(WhenDay[] whenDays, string timezone)
        {
            // Get the start of the current hour in the provided timezone
            var dt = _provider.Now(timezone);
            dt = dt.PlusMinutes(dt.Minute * -1);
            dt = dt.PlusSeconds(dt.Second * -1);

            // To avoid any infinite loops - stop after a week of hours.
            var maxHours = 24 * 7;
            var hours = 0;

            while (hours <= maxHours)
            {
                if (whenDays.Any(x => x.Day == (int) dt.DayOfWeek && x.Hours.Any(y => y == dt.Hour)))
                    return dt.ToDateTimeUtc();

                dt = dt.PlusHours(-1);
                hours++;
            }

            // If we've arrived here, we can't determine a date and time, so use "now".
            return _provider.Now(timezone).ToDateTimeUtc();
        }
        
        internal DateTime GetNextDelayEndUtc(WhenDay[] whenDays, string timezone)
        {
            // Get the start of the current hour in the provided timezone
            var dt = _provider.Now(timezone);
            dt = dt.PlusMinutes(dt.Minute * -1);
            dt = dt.PlusSeconds(dt.Second * -1);
            dt = dt.PlusHours(1);

            // To avoid any infinite loops - stop after a week of hours.
            var maxHours = 24 * 7;
            var hours = 0;

            while (hours <= maxHours)
            {
                if (whenDays.Any(x => x.Day == (int) dt.DayOfWeek && x.Hours.Any(y => y == dt.Hour)))
                    return dt.ToDateTimeUtc();

                dt = dt.PlusHours(1);
                hours++;
            }

            // If we've arrived here, we can't determine a date and time, so use "now".
            return _provider.Now(timezone).ToDateTimeUtc();
        }

        internal bool IsWithinWindow(DateTime published, WhenDay[] whenDays, string timezone)
        {
            var local = _provider.FromDateTime(published, timezone);
            return whenDays.Any(x => x.Day == (int) local.DayOfWeek && x.Hours.Any(y => y == local.Hour));
        }

        private FeedItem[] FilterAndLimit(FeedItem[] items, Parameters parameters)
        {

            if (parameters.Categories.Any())
            {
                var include = parameters.Categories.Where(x => !x.StartsWith("!")).ToArray();
                var exclude = parameters.Categories.Where(x => x.StartsWith("!")).Select(x => x.Substring(1))
                    .ToArray();

                if (include.Any())
                    items = items.Where(x => x.Categories.Any(y => include.Contains(y))).ToArray();

                if (exclude.Any())
                    items = items.Where(x => x.Categories.All(y => !exclude.Contains(y))).ToArray();
            }

            if (parameters.EnforceChronology)
                items = items.OrderByDescending(x => x.PublishingDate ?? DateTime.UtcNow).ToArray();

            if (parameters.Limit > 0)
            {
                items = items.Take(parameters.Limit).ToArray();
            }

            return items;
        }
    }
}