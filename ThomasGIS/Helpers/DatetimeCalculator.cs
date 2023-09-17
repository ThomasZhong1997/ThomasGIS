using System;
using System.Globalization;

namespace ThomasGIS.Helpers
{
    public static class DatetimeCalculator
    {
        public static int DatetimeToTimestamp(string datetime, string format)
        {
            DateTime newDatetime = DateTime.ParseExact(datetime, format, CultureInfo.InvariantCulture);
            DateTime startTime = new DateTime(1970, 1, 1);
            return (int)((newDatetime.Ticks / 10000000.0 - startTime.Ticks / 10000000.0));
        }

        public static string TimestampToDatetime(double timestamp, string format)
        {
            DateTime dtStart = new DateTime(1970, 1, 1, 0, 0, 0);
            long lTime = long.Parse(((int)timestamp).ToString() + "0000000");
            TimeSpan toNow = new TimeSpan(lTime);
            return dtStart.Add(toNow).ToString(format);
        }
    }
}
