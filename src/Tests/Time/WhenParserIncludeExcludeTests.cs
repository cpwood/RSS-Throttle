using System.Linq;
using RssThrottle.Feeds;
using RssThrottle.Time;
using Xunit;

namespace Tests.Time
{
    public class WhenParserIncludeExcludeTests
    {
        [Fact]
        public void SimpleSuccessful()
        {
            var result = WhenParser.Unpack(new[] {"1T15-5T20"}, Parameters.Modes.Include);
            
            Assert.Equal(5, result.Length);
            
            // Monday
            AssertDay(result, 1, 15, 23);

            // Tuesday, Wednesday, Thursday
            for (var d = 2; d <= 4; d++)
            {
                AssertDay(result, d, 0, 23); 
            }
            
            // Friday
            AssertDay(result, 5, 0, 20);
        }
        
        [Fact]
        public void CrossWeekSuccessful()
        {
            var result = WhenParser.Unpack(new[] {"5T00-2T00"}, Parameters.Modes.Include);
            
            Assert.Equal(5, result.Length);
     
            // Friday, Saturday, Sunday, Monday
            foreach(var d in new []{5,6,7,1})
            {
                AssertDay(result, d, 0, 23); 
            }
            
            // Tuesday
            AssertDay(result, 2, 0, 0);
        }
        
        [Fact]
        public void FullWeekSameStartEndDaySuccessful()
        {
            var result = WhenParser.Unpack(new[] {"5T15-5T10"}, Parameters.Modes.Include);
            
            Assert.Equal(7, result.Length);
            
            // Friday
            var day = result.FirstOrDefault(x => x.Day == 5);
            Assert.NotNull(day);
            Assert.Equal(20, day.Hours.Length);

            foreach (var h in new [] {0,1,2,3,4,5,6,7,8,9,10,15,16,17,18,19,20,21,22,23})
            {
                Assert.Contains(day.Hours, x => x == h);
            }
     
            // Saturday, Sunday, Monday, Tuesday, Wednesday, Thursday
            foreach(var d in new [] {6,7,1,2,3,4})
            {
                AssertDay(result, d, 0, 23); 
            }
        }

        private void AssertDay(WhenDay[] result, int day, int fromHour, int toHour)
        {
            var record = result.FirstOrDefault(x => x.Day == day);
            Assert.NotNull(record);
            Assert.Equal((toHour - fromHour) + 1, record.Hours.Length);

            for (var i = fromHour; i <= toHour; i++)
            {
                Assert.Contains(record.Hours, x => x == i);
            }
        }
    }
}