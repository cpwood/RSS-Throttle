using System;
using NodaTime;

namespace RssThrottle
{
    public interface IDateTimeProvider
    {
        ZonedDateTime Now(string timezone);
        ZonedDateTime FromDateTime(DateTime dt, string timezone);
    }
}