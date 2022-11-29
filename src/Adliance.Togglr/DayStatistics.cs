using System;
using System.Linq;
using System.Text;
using Adliance.Togglr.Extensions;

namespace Adliance.Togglr;

public static class DayStatistics
{
    public static void WriteEveryDayInMonth(Configuration configuration, StringBuilder sb, DateTime firstDayOfMonth, CalculationService calculationService)
    {
        var lastDayOfMonth = new DateTime(firstDayOfMonth.Year, firstDayOfMonth.Month, 1).AddMonths(1).AddDays(-1).Date;
        var shouldPrintMonth = !configuration.PrintDetailsStart.HasValue || firstDayOfMonth >= configuration.PrintDetailsStart.Value.Date;
        shouldPrintMonth = shouldPrintMonth && (!configuration.PrintDetailsEnd.HasValue || lastDayOfMonth <= configuration.PrintDetailsEnd.Value.Date);

        if (!shouldPrintMonth) return;

        sb.AppendLine("<div class=\"container\">");
        sb.AppendLine($"<h2 class=\"title is-5\" style=\"margin: 2rem 0 0 0;\">{firstDayOfMonth:MMMM yyyy}</h2>");
        sb.AppendLine("<table class=\"table is-size-7\" style=\"margin:1rem 0 0 0;\">");
        sb.AppendLine("<thead><tr>");
        sb.AppendLine("<th>Tag</th>");
        sb.AppendLine("<th>Arbeitszeit</th>");
        sb.AppendLine("<th>Pause</th>");
        sb.AppendLine("<th class=\"has-text-right\">Soll (h)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Ist (h)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Verr. (%)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Pause (h)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Überst. (h)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Saldo (h)</th>");
        sb.AppendLine("<th>HomeOffice</th>");
        sb.AppendLine("<th>Sonstiges</th>");
        sb.AppendLine("</tr></thead>");
        sb.AppendLine("<tbody>");

        var loopDate = firstDayOfMonth;
        while (loopDate.Month == firstDayOfMonth.Month)
        {
            if (calculationService.Days.ContainsKey(loopDate.Date))
            {
                var shouldPrintDay = !configuration.PrintDetailsStart.HasValue || loopDate.Date >= configuration.PrintDetailsStart.Value.Date;
                shouldPrintDay = shouldPrintDay && (!configuration.PrintDetailsEnd.HasValue || loopDate.Date <= configuration.PrintDetailsEnd.Value.Date);
                if (!shouldPrintDay) continue;

                WriteDay(configuration, sb, calculationService.Days[loopDate.Date]);
            }

            loopDate = loopDate.AddDays(1);
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        sb.AppendLine("</div>");
    }

    private static void WriteDay(Configuration configuration, StringBuilder sb, CalculationService.Day day)
    {
        sb.AppendLine($"<tr class=\"{(day.Expected <= 0 ? "has-text-grey-light" : "")}\">");
        sb.AppendLine($"<td>{day.Date:dddd, dd.MM.yyyy}</td>");

        if (day.Expected > 0)
        {
            if (day.PrintTimes)
            {
                sb.AppendLine($"<td>{day.Start.UtcToCet():HH:mm}-{day.End.UtcToCet():HH:mm}</td>");
                if (day.BreakStart.HasValue && day.BreakEnd.HasValue)
                {
                    sb.AppendLine($"<td>{day.BreakStart.Value.UtcToCet():HH:mm}-{day.BreakEnd.Value.UtcToCet():HH:mm}</td>");
                }
                else
                {
                    sb.AppendLine("<td></td>");
                }
            }
            else
            {
                sb.AppendLine("<td></td><td></td>");
            }

            sb.AppendLine($"<td class=\"has-text-right\">{day.Expected:N2}</td>");
            sb.AppendLine($"<td class=\"has-text-right\">{day.Total:N2}</td>");
            sb.AppendLine($"<td class=\"has-text-right\">{(100d / day.BillableBase * day.BillableActual).FormatBillable()}</td>");

            if (day.PrintTimes)
            {
                sb.AppendLine($"<td class=\"has-text-right\">{day.Breaks:N2}</td>");
            }
            else
            {
                sb.AppendLine("<td></td>");
            }
        }
        else
        {
            sb.AppendLine("<td></td><td></td><td></td><td></td>");
        }

        sb.AppendLine($"<td class=\"has-text-right has-text-success\">{day.Overtime.FormatColor()}</td>");

        sb.AppendLine(day.Expected > 0
            ? $"<td class=\"has-text-right has-text-success\">{day.RollingOvertime.FormatColor()}</td>"
            : "<td></td>");

        if (day.IsHomeOffice)
        {
            sb.AppendLine("<td class=\"has-text-centered\">✔</td>");
        }
        else
        {
            sb.AppendLine("<td></td>");
        }

        sb.Append($"<td title=\"{day.VacationInHours:N2} hours vacation added\">");
        foreach (var s in day.Specials.Where(x => x.Value > 0))
        {
            sb.Append($"<span class=\"tag is-success\">{s.Key.GetName(configuration)}</span> ");
        }

        foreach (var w in day.Warnings)
        {
            sb.Append($"<span class=\"tag is-danger\">{w}</span> ");
        }

        sb.AppendLine("</td>");

        sb.AppendLine("</tr>");
    }
}