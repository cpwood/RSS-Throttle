using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;
using RssThrottle.Time;

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
        
        public static DateTime GetDate(this FeedItem item, DateTime fallback)
        {
            if (item == null)
                return fallback;
            
            return item.PublishingDate ??
                   (item.SpecificItem.Element.Name.LocalName == "entry"
                       ? ((AtomFeedItem) item.SpecificItem).UpdatedDate ?? fallback
                       : fallback);
        }

        // TODO: remove workaround when third-party bug is fixed.
        // Workaround for: https://github.com/arminreiter/FeedReader/issues/38
        public static ICollection<string> GetCategories(this FeedItem item)
        {
            // RSS - works fine.
            if (item.SpecificItem.Element.Name.LocalName == "item")
                return item.Categories;

            // Atom - doesn't.
            var atomItem = (AtomFeedItem) item.SpecificItem;
            var result = atomItem.Element.Elements(XName.Get("category", "http://www.w3.org/2005/Atom"))
                .Select(x => (string) x.Attribute("term")).ToList();
            return result;
        }
    }
}