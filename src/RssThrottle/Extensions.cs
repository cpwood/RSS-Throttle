using System.Collections.Generic;
using System.Linq;

namespace RssThrottle
{
    public static class Extensions
    {
        public static void AddIfNew<T>(this List<T> list, T value)
        {
            if (!list.Contains(value))
                list.Add(value);
        }

        public static void Merge(this List<WhenDay> destination, List<WhenDay> source)
        {
            foreach (var sourceDay in source)
            {
                var existingDay = destination.FirstOrDefault(x => x.Day == sourceDay.Day);

                if (existingDay == null)
                {
                    destination.Add(sourceDay);
                }
                else
                {
                    foreach (var hour in sourceDay.Hours)
                    {
                        existingDay.AddHour(hour);
                    }
                }
            }
        }
    }
}