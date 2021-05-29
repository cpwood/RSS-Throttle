using System.Linq;
using RssThrottle.Feeds;
using RssThrottle.Time;
using Xunit;

namespace Tests.Time
{
    public class WhenParserDelayTests
    {
        [Fact]
        public void ParsesSimpleSingleSuccessfully()
        {
            var result = WhenParser.Unpack(new[] {"3T12"}, Parameters.Modes.Delay);

            Assert.Single(result);
            Assert.Single(result.First().Hours);
            Assert.Equal(3, result.First().Day);
            Assert.All(result.First().Hours, x => Assert.Equal(12, x));
        }
        
        [Fact]
        public void ParsesSimpleDoubleSuccessfully()
        {
            var result = WhenParser.Unpack(new[] {"3T12", "5T17"}, Parameters.Modes.Delay);

            Assert.Equal(2, result.Length);
            
            Assert.Single(result.First().Hours);
            Assert.Equal(3, result.First().Day);
            Assert.All(result.First().Hours, x => Assert.Equal(12, x));
            
            Assert.Single(result.Last().Hours);
            Assert.Equal(5, result.Last().Day);
            Assert.All(result.Last().Hours, x => Assert.Equal(17, x));
        }
        
        [Fact]
        public void ParsesIdenticalDoubleSuccessfully()
        {
            var result = WhenParser.Unpack(new[] {"3T12", "3T12"}, Parameters.Modes.Delay);

            Assert.Single(result);
            Assert.Single(result.First().Hours);
            Assert.Equal(3, result.First().Day);
            Assert.All(result.First().Hours, x => Assert.Equal(12, x));
        }
        
        [Fact]
        public void ParsesSameDayDoubleSuccessfully()
        {
            var result = WhenParser.Unpack(new[] {"3T12", "3T17"}, Parameters.Modes.Delay);

            Assert.Single(result);
            Assert.Equal(2, result.First().Hours.Length);
            Assert.Equal(3, result.First().Day);
            Assert.Contains(result.First().Hours, x => x == 12);
            Assert.Contains(result.First().Hours, x => x == 17);
        }
        
        [Fact]
        public void ParsesDayRangeSuccessfully()
        {
            var result = WhenParser.Unpack(new[] {"3:5T12"}, Parameters.Modes.Delay);

            Assert.Equal(3, result.Length);

            for (var i = 3; i <= 5; i++)
            {
                var day = result.FirstOrDefault(x => x.Day == i);
                Assert.NotNull(day);
                Assert.Contains(day.Hours, x => x == 12);
            }
        }
        
        [Fact]
        public void ParsesDayRangeWithExtraSuccessfully()
        {
            var result = WhenParser.Unpack(new[] {"3:56T12"}, Parameters.Modes.Delay);

            Assert.Equal(4, result.Length);

            for (var i = 3; i <= 6; i++)
            {
                var day = result.FirstOrDefault(x => x.Day == i);
                Assert.NotNull(day);
                Assert.Contains(day.Hours, x => x == 12);
            }
        }
        
        [Fact]
        public void ParsesInverseDayRangeSuccessfully()
        {
            var result = WhenParser.Unpack(new[] {"5:3T12"}, Parameters.Modes.Delay);

            Assert.Equal(3, result.Length);

            for (var i = 3; i <= 5; i++)
            {
                var day = result.FirstOrDefault(x => x.Day == i);
                Assert.NotNull(day);
                Assert.Contains(day.Hours, x => x == 12);
            }
        }
        
        [Fact]
        public void ParsesAllDaysSuccessfully()
        {
            var result = WhenParser.Unpack(new[] {"*T12"}, Parameters.Modes.Delay);

            Assert.Equal(7, result.Length);

            for (var i = 1; i <= 7; i++)
            {
                var day = result.FirstOrDefault(x => x.Day == i);
                Assert.NotNull(day);
                Assert.Contains(day.Hours, x => x == 12);
            }
        }

        [Fact]
        public void ParsesHourRangeSuccessfully()
        {
            var result = WhenParser.Unpack(new[] {"3T12:15"}, Parameters.Modes.Delay);

            Assert.Single(result);

            var day = result.FirstOrDefault(x => x.Day == 3);
            Assert.NotNull(day);
            Assert.Equal(4, day.Hours.Length);

            for (var i = 12; i <= 15; i++)
            {
                Assert.Contains(day.Hours, x => x == i);
            }
        }
        
        [Fact]
        public void ParsesHourRangeWithExtraSuccessfully()
        {
            var result = WhenParser.Unpack(new[] {"3T12:1516"}, Parameters.Modes.Delay);

            Assert.Single(result);

            var day = result.FirstOrDefault(x => x.Day == 3);
            Assert.NotNull(day);
            Assert.Equal(5, day.Hours.Length);

            for (var i = 12; i <= 16; i++)
            {
                Assert.Contains(day.Hours, x => x == i);
            }
        }
        
        [Fact]
        public void ParsesInverseHourRangeSuccessfully()
        {
            var result = WhenParser.Unpack(new[] {"3T15:12"}, Parameters.Modes.Delay);

            Assert.Single(result);

            var day = result.FirstOrDefault(x => x.Day == 3);
            Assert.NotNull(day);
            Assert.Equal(4, day.Hours.Length);

            for (var i = 12; i <= 15; i++)
            {
                Assert.Contains(day.Hours, x => x == i);
            }
        }
        
        [Fact]
        public void ParsesAllHoursSuccessfully()
        {
            var result = WhenParser.Unpack(new[] {"3T*"}, Parameters.Modes.Delay);

            Assert.Single(result);

            var day = result.FirstOrDefault(x => x.Day == 3);
            Assert.NotNull(day);
            Assert.Equal(24, day.Hours.Length);

            for (var i = 0; i <= 23; i++)
            {
                Assert.Contains(day.Hours, x => x == i);
            }
        }
    }
}