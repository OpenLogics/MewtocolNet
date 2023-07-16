using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MewtocolNet {

    public static class PlcFormat {

        /// <summary>
        /// Gets the TimeSpan as a PLC representation string fe.
        /// <code>
        /// T#1h10m30s20ms
        /// </code>
        /// </summary>
        /// <param name="timespan"></param>
        /// <returns></returns>
        public static string ToPlcTime(this TimeSpan timespan) {

            if (timespan == null || timespan == TimeSpan.Zero)
                return $"T#0s";

            StringBuilder sb = new StringBuilder("T#");

            int millis = timespan.Milliseconds;
            int seconds = timespan.Seconds;
            int minutes = timespan.Minutes;
            int hours = timespan.Hours;
            int days = timespan.Days;

            if (days > 0) sb.Append($"{days}d");
            if (hours > 0) sb.Append($"{hours}h");
            if (minutes > 0) sb.Append($"{minutes}m");
            if (seconds > 0) sb.Append($"{seconds}s");
            if (millis > 0) sb.Append($"{millis}ms");

            return sb.ToString();

        }

        public static TimeSpan ParsePlcTime(string plcTimeFormat) {

            var reg = new Regex(@"(?:T|t)#(?:(?<d>[0-9]{1,2})d)?(?:(?<h>[0-9]{1,2})h)?(?:(?<m>[0-9]{1,2})m)?(?:(?<s>[0-9]{1,2})s)?(?:(?<ms>[0-9]{1,3})ms)?");
            var match = reg.Match(plcTimeFormat);

            if (match.Success) {

                var days = match.Groups["d"].Value;
                var hours = match.Groups["h"].Value;
                var minutes = match.Groups["m"].Value;
                var seconds = match.Groups["s"].Value;
                var milliseconds = match.Groups["ms"].Value;

                TimeSpan retTime = TimeSpan.Zero;

                if (!string.IsNullOrEmpty(days)) retTime += TimeSpan.FromDays(int.Parse(days));
                if (!string.IsNullOrEmpty(hours)) retTime += TimeSpan.FromHours(int.Parse(hours));
                if (!string.IsNullOrEmpty(minutes)) retTime += TimeSpan.FromMinutes(int.Parse(minutes));
                if (!string.IsNullOrEmpty(seconds)) retTime += TimeSpan.FromSeconds(int.Parse(seconds));
                if (!string.IsNullOrEmpty(milliseconds)) retTime += TimeSpan.FromMilliseconds(int.Parse(milliseconds));

                if ((retTime.TotalMilliseconds % 10) != 0)
                    throw new NotSupportedException("Plc times can't have a millisecond component lower than 10ms");

                return retTime;

            }

            return TimeSpan.Zero;

        }

        /// <summary>
        /// Turns a bit array into a 0 and 1 string
        /// </summary>
        public static string ToBitString(this BitArray arr) {

            var bits = new bool[arr.Length];
            arr.CopyTo(bits, 0);
            return string.Join("", bits.Select(x => x ? "1" : "0"));

        }

    }

}
