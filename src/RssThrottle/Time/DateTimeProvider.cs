using System;
using NodaTime;
using NodaTime.Extensions;

namespace RssThrottle.Time
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public ZonedDateTime Now(string timezone)
        {
            var zone = DateTimeZoneProviders.Tzdb[timezone];
            var clock = SystemClock.Instance.InZone(zone);
            
            return clock.GetCurrentZonedDateTime();
        }

        public ZonedDateTime FromDateTime(DateTime dt, string timezone)
        {
            return new ZonedDateTime(dt.ToInstant(), DateTimeZoneProviders.Tzdb[timezone]);
        }
    }
}