using System;
using Azure.Storage.Blobs;
using Moq;
using NodaTime;
using RssThrottle;
using Xunit;

namespace Tests
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
            var mock = new Mock<IDateTimeProvider>();
            mock.Setup(x => x.Now(It.IsAny<string>()))
                .Returns(new ZonedDateTime(Instant.FromUtc(2021, 5, 28, 14, 15, 45),
                    DateTimeZoneProviders.Tzdb["Europe/London"]));
            var provider = mock.Object;

            var service = new FeedService(null, null, provider);
            var whenDays = WhenParser.Unpack(new[] {"*T0620"}, Parameters.Modes.Delay);
            var delayUntil = service.GetNextDelayEndUtc(whenDays, "Europe/London");

            // BST on 28/05/2021, so 8pm BST is 7pm UTC.
            var expected = new DateTime(2021, 5, 28, 19, 0, 0, DateTimeKind.Utc);
            Assert.Equal(expected, delayUntil);
        }
        
        // [Fact]
        //  public async void GetGuardianNews()
        //  {
        //      // Integration Test
        //      var blobServiceClient = new BlobServiceClient("");
        //      var containerClient = blobServiceClient.GetBlobContainerClient("rss");
        //      
        //      var service = new FeedService(new BlobStorageCache(containerClient));
        //      var result = await service.ProcessAsync(new Parameters
        //      {
        //          Url = "https://www.theguardian.com/uk/rss",
        //          When = new [] {"*T0620"},
        //          Mode = Parameters.Modes.Delay,
        //          Limit = 5,
        //          Timezone = "Europe/London"
        //      });
        //  }
    }
}