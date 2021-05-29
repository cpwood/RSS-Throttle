using System;
using System.IO;
using System.Linq;
using CodeHollow.FeedReader;
using Moq;
using NodaTime;
using RssThrottle;
using RssThrottle.Caching;
using RssThrottle.Feeds;
using RssThrottle.Time;
using Xunit;
using FeedReader = CodeHollow.FeedReader.FeedReader;

namespace Tests.Feeds
{
    public class FeedServiceTests
    {
        [Fact]
        public void GetLastDelayEndSuccessful()
        {
            var mock = new Mock<IDateTimeProvider>();
            mock.Setup(x => x.Now(It.IsAny<string>()))
                .Returns(new ZonedDateTime(Instant.FromUtc(2021, 5, 28, 14, 15, 45),
                    DateTimeZoneProviders.Tzdb["Europe/London"]));
            var provider = mock.Object;

            var service = new FeedService(null, null, provider);
            var whenDays = WhenParser.Unpack(new[] {"*T0620"}, Parameters.Modes.Delay);
            var delayUntil = service.GetLastDelayEndUtc(whenDays, "Europe/London");

            // BST on 28/05/2021, so 6am BST is 5am UTC.
            var expected = new DateTime(2021, 5, 28, 5, 0, 0, DateTimeKind.Utc);
            Assert.Equal(expected, delayUntil);
        }
        
        [Fact]
        public void GetNextDelayEndSuccessful()
        {
            var provider = CreateDateTimeProvider(2021, 5, 28, 21, 15, 16);
            var service = new FeedService(null, null, provider);
            var whenDays = WhenParser.Unpack(new[] {"*T0620"}, Parameters.Modes.Delay);
            var delayUntil = service.GetNextDelayEndUtc(whenDays, "Europe/London");

            // BST on 29/05/2021, so 6am BST is 5am UTC.
            var expected = new DateTime(2021, 5, 29, 5, 0, 0, DateTimeKind.Utc);
            Assert.Equal(expected, delayUntil);
        }

        [Fact]
        public async void GetRssSuccessful()
        {
            var provider = CreateDateTimeProvider(2021, 5, 29, 21, 15, 16);
            var service = new FeedService(new NonCache(),
                new TestFeedReader(await File.ReadAllTextAsync("example-rss.xml")), provider);
            
            var result = await service.ProcessAsync(new Parameters
            {
                Url = "https://wiganwarriors.com/blog/feed/",
                When = new [] {"*T20"},
                Mode = Parameters.Modes.Delay,
                Timezone = "Europe/London",
                Limit = 5,
                Categories = new []{"Most Popular", "!Obituaries"}
            });

            var feed = ParseResult(result);
            
            Assert.Equal(5, feed.Items.Count);
            Assert.True(feed.Items.All(x =>
                x.GetDate(DateTime.UtcNow) <= new DateTime(2021, 5, 29, 19, 0, 0, DateTimeKind.Utc)));
            Assert.True(feed.Items.All(x => x.Categories.Any(y => y == "Most Popular")));
            
            // Story with title "Kilner leaves Wigan" should not be included since we're excluding
            // anything with the category "Obituaries". (PS: he's not actually dead!)
            Assert.Null(feed.Items.FirstOrDefault(x =>
                x.Title == "Kilner leaves Wigan"));
            
            Assert.Equal("application/rss+xml", result.MimeType);
        }
        
        [Fact]
        public async void GetAtomSuccessful()
        {
            var provider = CreateDateTimeProvider(2021, 5, 29, 21, 15, 16);
            var service = new FeedService(new NonCache(),
                new TestFeedReader(await File.ReadAllTextAsync("example-atom.xml")), provider);
            
            var result = await service.ProcessAsync(new Parameters
            {
                Url = "https://wiganwarriors.com/blog/feed/atom/",
                When = new [] {"*T20"},
                Mode = Parameters.Modes.Delay,
                Timezone = "Europe/London",
                Limit = 5,
                // TODO: reinstate when Atom categories bug resolved in FeedReader:
                // https://github.com/arminreiter/FeedReader/issues/38
                Categories = new []{"Most Popular", "!Obituaries"}
            });

            var feed = ParseResult(result);
            
            Assert.Equal(5, feed.Items.Count);
            Assert.True(feed.Items.All(x =>
                x.GetDate(DateTime.UtcNow) <= new DateTime(2021, 5, 29, 19, 0, 0, DateTimeKind.Utc)));
            Assert.True(feed.Items.All(x => x.GetCategories().Any(y => y == "Most Popular")));
            
            // Story with title "Kilner leaves Wigan" should not be included since we're excluding
            // anything with the category "Obituaries". (PS: he's not actually dead!)
            Assert.Null(feed.Items.FirstOrDefault(x => x.Title == "Kilner leaves Wigan"));
            
            Assert.Equal("application/atom+xml", result.MimeType);
        }
        
         private IDateTimeProvider CreateDateTimeProvider(
             int year,
             int monthOfYear,
             int dayOfMonth,
             int hourOfDay,
             int minuteOfHour,
             int secondOfMinute
             )
         {
             var mock = new Mock<IDateTimeProvider>();
             mock.Setup(x => x.Now(It.IsAny<string>()))
                 .Returns(new ZonedDateTime(Instant.FromUtc(year, monthOfYear, dayOfMonth, hourOfDay, minuteOfHour, secondOfMinute),
                     DateTimeZoneProviders.Tzdb["Europe/London"]));
             return mock.Object;
         }

         private Feed ParseResult(FeedResult result)
         {
             return FeedReader.ReadFromString(result.Content);
         }
    }
}