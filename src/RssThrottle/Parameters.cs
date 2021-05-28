using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using NodaTime;

namespace RssThrottle
{
    public class Parameters
    {
        public enum Modes
        {
            Delay,
            Include,
            Exclude
        }

        private string[] _when = new string[0];
        private string _timezone = "UTC";
        private int _limit;
        
        public string Url { get; set; }
        public Modes Mode { get; set; } = Modes.Delay;

        public string[] When
        {
            get => _when;
            set
            {
                if (!WhenParser.IsValid(value, Mode))
                    throw new ArgumentException("Invalid When values received.", nameof(When));
                
                _when = value;
            }
        }

        public string Timezone
        {
            get => _timezone;
            set
            {
                if (!DateTimeZoneProviders.Tzdb.Ids.Contains(value))
                    throw new ArgumentException("Invalid timezone", nameof(Timezone));
                
                _timezone = value;
            }
        }

        public string[] Categories { get; set; } = new string[0];

        public int Limit
        {
            get => _limit;
            set
            {
                if (value < 0)
                    throw new ArgumentException("Limit must be 0 or higher.", nameof(Limit));
                
                _limit = value;
            }
        }
        
        public bool EnforceChronology { get; set; }

        public string Hash
        {
            get
            {
                var builder = new StringBuilder();
                builder.AppendLine(Url);
                builder.AppendLine(Mode.ToString());

                foreach (var item in When)
                {
                    builder.AppendLine(item);
                }

                builder.AppendLine(Timezone);

                foreach (var item in Categories)
                {
                    builder.AppendLine(item);
                }

                builder.AppendLine(EnforceChronology.ToString());
                builder.AppendLine(Limit.ToString());

                var hashBytes = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
                builder.Clear();
                
                foreach (var b in hashBytes)
                {
                    builder.Append(b.ToString("X2"));
                }

                return builder.ToString();
            }
        }

        [SuppressMessage("ReSharper", "NotResolvedInText")]
        public void Parse(string querystring)
        {
            var items = QueryHelpers.ParseQuery(querystring);

            if (items.ContainsKey("url"))
                Url = items["url"].ToString();

            if (items.ContainsKey("mode"))
            {
                if (!Enum.TryParse(typeof(Modes), items["mode"].ToString(), true, out var m))
                    throw new ArgumentException("Mode must be 'Delay', 'Include' or 'Exclude'.", "mode");
                
                if (m != null) Mode = (Modes) m;
            }
            
            if (items.ContainsKey("when"))
                When = items["when"].ToString().Split(',');

            if (items.ContainsKey("timezone"))
                Timezone = items["timezone"].ToString();
            
            if (items.ContainsKey("categories"))
                Categories = items["categories"].ToString().Split(',');

            if (items.ContainsKey("limit"))
            {
                if (!int.TryParse(items["limit"].ToString(), out var limit))
                    throw new ArgumentException("A positive number must be provided.", "limit");
                
                Limit = limit;
            }

            if (items.ContainsKey("enforceChronology"))
            {
                if (!bool.TryParse(items["enforceChronology"].ToString(), out var enforce))
                    throw new ArgumentException("This must be a boolean value.", "enforceChronology");

                EnforceChronology = enforce;
            }
        }
    }
}