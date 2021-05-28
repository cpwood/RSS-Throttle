using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RssThrottle
{
    public static class WhenParser
    {
        private const string DelayRegex =
            @"^(?<days>([1-7]+|[1-7]\:[1-7]|\*)+)T(?<hours>(((([0-1][0-9])|20|21|22|23)|(([0-1][0-9])|20|21|22|23)\:(([0-1][0-9])|20|21|22|23))|\*)+)$";
        
        private const string WindowRegex =
            @"^(?<fromDay>[1-7])T(?<fromHour>([0-1][0-9])|20|21|22|23)\-(?<toDay>[1-7])T(?<toHour>([0-1][0-9])|20|21|22|23)$";

        public static WhenDay[] Unpack(string[] values, Parameters.Modes mode)
        {
            if (!IsValid(values, mode))
                throw new ArgumentException("'When' values are invalid.", nameof(values));

            return mode == Parameters.Modes.Delay ? UnpackDelay(values) : UnpackIncludeExclude(values);
        }
        
        private static WhenDay[] UnpackDelay(string[] values)
        {
            var days = new List<WhenDay>();

            foreach (var value in values)
            {
                var currentDays = new List<WhenDay>();
                var m = Regex.Match(value, DelayRegex);
                var dayString = m.Groups["days"].Value;
                var hoursString = m.Groups["hours"].Value;
                
                // DAYSTRING
                // Deal with * first and then ignore everything
                // else, if present.
                if (dayString.Contains("*"))
                {
                    for (var i = 1; i <= 7; i++)
                    {
                        currentDays.AddIfNew(new WhenDay(i));
                    }
                }
                else
                {
                    // Otherwise, deal with ranges (x:y)
                    var ranges = Regex.Matches(dayString, @"(?<min>[1-7])\:(?<max>[1-7])");

                    foreach (Match range in ranges)
                    {
                        var min = int.Parse(range.Groups["min"].Value);
                        var max = int.Parse(range.Groups["max"].Value);

                        if (max < min)
                        {
                            // Swap min and max - they're the wrong way around
                            var temp = min;
                            min = max;
                            max = temp;
                        }

                        for (var i = min; i <= max; i++)
                        {
                            currentDays.AddIfNew(new WhenDay(i));
                        }
                    }

                    dayString = Regex.Replace(dayString, @"[1-7]\:[1-7]", string.Empty);

                    // Then deal with any remaining digits.
                    for (var i = 0; i < dayString.Length; i++)
                    {
                        currentDays.AddIfNew(new WhenDay(int.Parse(dayString.Substring(i, 1))));
                    }
                }
                
                // HOURSTRING
                // Deal with * first and then ignore everything
                // else, if present.
                if (hoursString.Contains("*"))
                {
                    foreach (var currentDay in currentDays)
                    {
                        for (var i = 0; i <= 23; i++)
                        {
                            currentDay.AddHour(i);
                        }
                    }
                    
                }
                else
                {
                    // Otherwise, deal with ranges (xx:yy)
                    // We're using a slightly looser regex here, but we've already validated
                    // against a tighter regex earlier, so we should be fine.
                    var ranges = Regex.Matches(hoursString, @"(?<min>[0-9]{2})\:(?<max>[0-9]{2})");

                    foreach (Match range in ranges)
                    {
                        var min = int.Parse(range.Groups["min"].Value);
                        var max = int.Parse(range.Groups["max"].Value);

                        if (max < min)
                        {
                            // Swap min and max - they're the wrong way around
                            var temp = min;
                            min = max;
                            max = temp;
                        }
                        
                        foreach (var currentDay in currentDays)
                        {
                            for (var i = min; i <= max; i++)
                            {
                                currentDay.AddHour(i);
                            }
                        }
                    }

                    hoursString = Regex.Replace(hoursString, @"[0-9]{2}\:[0-9]{2}", string.Empty);

                    // Then deal with any remaining double-digits.
                    for (var i = 0; i < hoursString.Length; i += 2)
                    {
                        foreach (var currentDay in currentDays)
                        {
                            currentDay.AddHour(int.Parse(hoursString.Substring(i, 2)));
                        }
                    }
                }
                
                days.Merge(currentDays);
            }
            
            return days.ToArray();
        }

        private static WhenDay[] UnpackIncludeExclude(string[] values)
        {
            var days = new List<WhenDay>();

            bool IsStartDay(int currentDay, int fromDay, int processed)
            {
                // If we're going from 1T15 to 1T10 (i.e. just under a week), we want 
                // to go from fromHour only when it's the first time we've encountered
                // this day.
                if (currentDay == fromDay && processed == 0)
                    return true;

                return false;
            }

            bool IsEndDay(int currentDay, int fromDay, int toDay, int fromHour, int toHour, int processed)
            {
                if (currentDay == toDay)
                {
                    // From and to days are different; OR
                    // From and to days are the same but the from-hour is before the to-hour; OR
                    // From and to days are the same, the to-hour is before the from-hour and we've done this day before:
                    if (fromDay != toDay || (fromDay == toDay && fromHour < toHour) ||
                        (fromDay == toDay && fromHour > toHour && processed > 0))
                        return true;
                }

                return false;
            }

            foreach (var value in values)
            {
                var currentDays = new List<WhenDay>();
                var m = Regex.Match(value, WindowRegex);
                var fromDay = int.Parse(m.Groups["fromDay"].Value);
                var toDay = int.Parse(m.Groups["toDay"].Value);
                var fromHour = int.Parse(m.Groups["fromHour"].Value);
                var toHour = int.Parse(m.Groups["toHour"].Value);

                var day = fromDay;
                var processed = 0;

                while (true)
                {
                    var currentDay = currentDays.FirstOrDefault(x => x.Day == day) ?? new WhenDay(day);
                    var startHour = 0;
                    var endHour = 23;

                    if (IsStartDay(day, fromDay, processed))
                    {
                        startHour = fromHour;
                    }
                    else if (IsEndDay(day, fromDay, toDay, fromHour, toHour, processed))
                    {
                        endHour = toHour;
                    }
                    
                    for (var i = startHour; i <= endHour; i++)
                    {
                        currentDay.AddHour(i);
                    }
                    
                    currentDays.AddIfNew(currentDay);

                    if (IsEndDay(day, fromDay, toDay, fromHour, toHour, processed))
                        break;

                    day++;
                    if (day == 8) day = 1;
                    processed++;
                }

                days.Merge(currentDays);
            }

            return days.ToArray();
        }

        public static bool IsValid(string[] values, Parameters.Modes mode)
        {
            var pattern = mode == Parameters.Modes.Delay ? DelayRegex : WindowRegex;
            
            foreach (var value in values)
            {
                if (!Regex.IsMatch(value, pattern))
                    return false;
            }

            return true;
        }
    }
}