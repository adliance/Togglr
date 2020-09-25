using System;
using System.Globalization;

namespace Togglr.Extensions
{
    public static class DateTimeExtensions
    {
        public static int GetWeekNumber(this DateTime d)
        {
            var culture = new CultureInfo("de-AT");
            return culture.Calendar.GetWeekOfYear(d, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        }
        
        public static bool IsWeekend(this DateTime d)
        {
            return d.DayOfWeek == DayOfWeek.Sunday || d.DayOfWeek == DayOfWeek.Saturday;
        }
    }
}