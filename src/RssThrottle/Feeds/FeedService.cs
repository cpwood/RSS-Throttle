using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using CodeHollow.FeedReader;
using RssThrottle.Caching;
using RssThrottle.Time;

namespace RssThrottle.Feeds
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
        
        public async Task<FeedResult> ProcessAsync(Parameters parameters)
        {
            if (parameters.Mode == Parameters.Modes.Delay)
            {
                var cached = await _cache.GetFromCacheAsync(parameters);
                if (!string.IsNullOrEmpty(cached))
                    return new FeedResult(cached);
            }

            var nextDelay = DateTime.MinValue;
            var inputFeed = await _feedReader.ReadAsync(parameters.Url);

            FeedItem[] items;
            
            if (parameters.Mode == Parameters.Modes.Delay)
            {
                var whenDays = WhenParser.Unpack(parameters.When, Parameters.Modes.Delay);
                var delayUntil = GetLastDelayEndUtc(whenDays, parameters.Timezone);
                nextDelay = GetNextDelayEndUtc(whenDays, parameters.Timezone);
                items = inputFeed.Items
                    .Where(x => x.GetDate(delayUntil) <= delayUntil)
                    .ToArray();
            }
            else if (parameters.Mode == Parameters.Modes.Include)
            {
                var whenDays = WhenParser.Unpack(parameters.When, Parameters.Modes.Include);
                items = inputFeed.Items.Where(x =>
                        IsWithinWindow(x.GetDate(DateTime.UtcNow), whenDays, parameters.Timezone))
                    .ToArray();
            }
            else
            {
                var whenDays = WhenParser.Unpack(parameters.When, Parameters.Modes.Exclude);
                items = inputFeed.Items.Where(x =>
                        !IsWithinWindow(x.GetDate(DateTime.UtcNow), whenDays, parameters.Timezone))
                    .ToArray();
            }
            
            items = FilterAndLimit(items, parameters);

            var result = CreateOutputFeed(inputFeed, items);

            if (parameters.Mode == Parameters.Modes.Delay)
                await _cache.CacheAsync(parameters, nextDelay, result);
            
            return new FeedResult(result);
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
                    items = items.Where(x => x.GetCategories().Any(y => include.Contains(y))).ToArray();

                if (exclude.Any())
                    items = items.Where(x => x.GetCategories().All(y => !exclude.Contains(y))).ToArray();
            }

            if (parameters.EnforceChronology)
                items = items.OrderByDescending(x => x.PublishingDate ?? DateTime.UtcNow).ToArray();

            if (parameters.Limit > 0)
            {
                items = items.Take(parameters.Limit).ToArray();
            }

            return items;
        }

        private static string CreateOutputFeed(Feed inputFeed, FeedItem[] outputItems)
        {
            var doc =
                inputFeed.SpecificFeed.Element.Document ?? throw new InvalidOperationException();

            var itemName = "item";
            
            var maxPublished =
                outputItems.OrderByDescending(x => x.GetDate(DateTime.UtcNow)).FirstOrDefault()
                    ?.GetDate(DateTime.UtcNow) ?? DateTime.UtcNow;

            if (inputFeed.Type == FeedType.Atom)
            {
                itemName = "entry";
                
                // 2021-05-29 18:57:15Z
                doc.Root?.Element(XName.Get("updated", "http://www.w3.org/2005/Atom"))?.SetValue(maxPublished.ToString("u"));
                
                // 2021-05-29 18:57:15Z
                doc.Root?.Element(XName.Get("date", "http://purl.org/dc/elements/1.1/"))
                    ?.SetValue(maxPublished.ToString("u"));
            }
            else
            {
                // Sat, 29 May 2021 18:57:15 GMT
                doc.Root?.Element("channel")?.Element("pubDate")?.SetValue(maxPublished.ToString("R"));
                doc.Root?.Element("channel")?.Element("lastBuildDate")?.SetValue(maxPublished.ToString("R"));
                
                // 2021-05-29 18:57:15Z
                doc.Root?.Element("channel")?.Element(XName.Get("date", "http://purl.org/dc/elements/1.1/"))
                    ?.SetValue(maxPublished.ToString("u"));
            }

            var inputElements = inputFeed.SpecificFeed.Element.Descendants()
                .Where(x => x.Name.LocalName == itemName).ToArray();

            var outputElements = outputItems.Select(x => x.SpecificItem.Element).ToArray();

            var toRemove = inputElements.Except(outputElements).ToArray();

            foreach (var r in toRemove)
            {
                r.Remove();
            }
            
            return doc.ToString();
        }
    }
}