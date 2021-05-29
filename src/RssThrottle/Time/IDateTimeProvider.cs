using System;
using NodaTime;

namespace RssThrottle.Time
{
    public interface IDateTimeProvider
    {
        ZonedDateTime Now(string timezone);
        ZonedDateTime FromDateTime(DateTime dt, string timezone);
    }
}