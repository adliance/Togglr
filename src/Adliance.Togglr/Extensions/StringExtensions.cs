using System;

namespace Adliance.Togglr.Extensions
{
    public static class StringExtensions
    {
        public static DayOfWeek GetDayOfWeek(this string s)
        {
            return s.ToLower() switch
            {
                "mon" => DayOfWeek.Monday,
                "tue" => DayOfWeek.Tuesday,
                "wed" => DayOfWeek.Wednesday,
                "thu" => DayOfWeek.Thursday,
                "fri" => DayOfWeek.Friday,
                "sat" => DayOfWeek.Saturday,
                "sun" => DayOfWeek.Sunday,
                _ => throw new ArgumentOutOfRangeException(nameof(s), s, null)
            };
        }
    }
}