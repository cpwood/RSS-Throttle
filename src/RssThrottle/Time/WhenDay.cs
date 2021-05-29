using System.Collections.Generic;

namespace RssThrottle.Time
{
    public class WhenDay
    {
        private readonly List<int> _hours = new List<int>();
        
        public int Day { get; }
        public int[] Hours => _hours.ToArray();

        public WhenDay(int day)
        {
            Day = day;
        }

        public void AddHour(int hour)
        {
            _hours.AddIfNew(hour);
        }

        private bool Equals(WhenDay other)
        {
            return Day == other.Day;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((WhenDay) obj);
        }

        public override int GetHashCode()
        {
            return Day;
        }
    }
}