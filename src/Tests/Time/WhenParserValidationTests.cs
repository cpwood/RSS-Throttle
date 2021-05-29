using RssThrottle.Feeds;
using RssThrottle.Time;
using Xunit;

namespace Tests.Time
{
    public class WhenParserValidationTests
    {
        [Fact]
        public void DelayValidatesSuccessfully()
        {
            Assert.True(WhenParser.IsValid(new []
            {
                "1T10",
                "1:2T10",
                "1:25T10",
                "1:34:5T10",
                "123T10",
                "*T10",
                "1T1012",
                "1T10:12",
                "1T*"
            }, Parameters.Modes.Delay));
        }

        [Fact]
        public void IncludeExcludeValidatesSuccessfully()
        {
            Assert.True(WhenParser.IsValid(new []
            {
                "1T10-5T15"
            }, Parameters.Modes.Include));
        }

        [Fact]
        public void DelayFailsSuccessfully()
        {
            Assert.False(WhenParser.IsValid(new[] {"8T30"}, Parameters.Modes.Delay));
            Assert.False(WhenParser.IsValid(new[] {"1:23:T10"}, Parameters.Modes.Delay));
            Assert.False(WhenParser.IsValid(new[] {"1T10:15:"}, Parameters.Modes.Delay));
        }
    }
}