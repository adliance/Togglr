using System;
using System.Globalization;
using NodaTime;

namespace Adliance.Togglr.Extensions;

public static class DateTimeExtensions
{
    public static int GetWeekNumber(this DateTime d)
    {
        var culture = new CultureInfo("de-AT");
        return culture.Calendar.GetWeekOfYear(d, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
    }

    public static DateTime UtcToCet(this DateTime dateTime)
    {
        var vienna = DateTimeZoneProviders.Tzdb["Europe/Vienna"];
        return LocalDateTime.FromDateTime(dateTime).InZoneStrictly(DateTimeZone.Utc).WithZone(vienna).ToDateTimeUnspecified();
    }
}
