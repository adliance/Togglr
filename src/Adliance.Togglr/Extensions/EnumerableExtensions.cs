using System;
using System.Collections.Generic;
using System.Linq;
using TogglApi.Client.Reports.Models.Response;

namespace Adliance.Togglr.Extensions;

public static class EnumerableExtensions
{
    public static IDictionary<string, List<DetailedReportDatum>> GroupByUser(this IEnumerable<DetailedReportDatum> entries)
    {
        return entries.GroupBy(x => x.User).ToDictionary(x => x.Key, x => x.ToList());
    }

    public static IDictionary<DateTime, List<DetailedReportDatum>> GroupByMonth(this IEnumerable<DetailedReportDatum> entries)
    {
        return entries.GroupBy(x => new DateTime(x.Start.Year, x.Start.Month, 1)).ToDictionary(x => x.Key, x => x.ToList());
    }

    public static IDictionary<int, List<DetailedReportDatum>> GroupByWeek(this IEnumerable<DetailedReportDatum> entries)
    {
        return entries.GroupBy(x => x.Start.GetWeekNumber()).ToDictionary(x => x.Key, x => x.ToList());
    }

    public static IDictionary<DateTime, List<DetailedReportDatum>> GroupByDay(this IEnumerable<DetailedReportDatum> entries)
    {
        return entries.GroupBy(x => x.Start.Date).ToDictionary(x => x.Key, x => x.ToList());
    }
}