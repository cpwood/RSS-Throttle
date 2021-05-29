using System;
using System.Linq;
using RssThrottle.Feeds;
using Xunit;

namespace Tests.Feeds
{
    public class ParameterTests
    {
        [Fact]
        public void ParsesSuccessfully()
        {
            var querystring = "?url=https://foo.com/rss&mode=Delay&when=1:5T0620,6:7T18&limit=10&categories=Foo,!Bar&timezone=Europe/London";
            
            var param = new Parameters();
            param.Parse(querystring);
            
            Assert.Equal("https://foo.com/rss", param.Url);
            Assert.Equal(Parameters.Modes.Delay, param.Mode);
            Assert.Equal(2, param.When.Length);
            Assert.Equal("1:5T0620", param.When.First());
            Assert.Equal("6:7T18", param.When.Last());
            Assert.Equal(10, param.Limit);
            Assert.Equal(2, param.Categories.Length);
            Assert.Equal("Foo", param.Categories.First());
            Assert.Equal("!Bar", param.Categories.Last());
            Assert.Equal("Europe/London", param.Timezone);
        }
        
        [Fact]
        public void HandlesInvalidValuesSuccessfully()
        {
            var querystring = "?url=https://foo.com/rss&mode=INVALID&when=1-5T0620,6-7T18&limit=-100&categories=Foo,!Bar&timezone=Foo/Bar";
            
            var param = new Parameters();

            Assert.Throws<ArgumentException>(() =>
            {
                param.Parse(querystring);
            });
        }
    }
}