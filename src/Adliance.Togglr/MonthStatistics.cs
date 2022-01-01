using System;
using System.Linq;
using System.Text;
using Adliance.Togglr.Extensions;

namespace Adliance.Togglr;

public static class MonthStatistics
{
    public static void WriteEveryMonth(StringBuilder sb, CalculationService calculationService)
    {
        sb.AppendLine("<div class=\"container\">");
        sb.AppendLine("<table class=\"table is-size-7\" style=\"margin: 2rem 0 0 0;\">");
        sb.AppendLine("<thead><tr>");
        sb.AppendLine("<th>Monat</th>");
        sb.AppendLine("<th class=\"has-text-right\">Soll (h)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Ist (h)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Verr. (%)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Überst. (h)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Saldo (h)</th>");
        sb.AppendLine("<th class=\"has-text-right\">HomeOffice (T)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Feiertag (T)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Krankenstand (T)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Sonderurlaub (T)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Urlaub (T)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Urlaubsanspr. (T)*</th>");
        sb.AppendLine("</tr></thead>");
        sb.AppendLine("<tbody>");

        var minDate = calculationService.Days.OrderBy(x => x.Key).Select(x => x.Value).First();
        var maxDate = calculationService.Days.OrderBy(x => x.Key).Select(x => x.Value).Last();
        var loopDate = new DateTime(minDate.Date.Year, minDate.Date.Month, 1);
        while (loopDate <= maxDate.Date)
        {
            var entries = calculationService.Days.Where(x => x.Key.Year == loopDate.Year && x.Key.Month == loopDate.Month).Select(x => x.Value).OrderBy(x => x.Date).ToList();
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{loopDate:MMMM yyyy}</td>");
            sb.AppendLine($"<td class=\"has-text-right\">{entries.Sum(x => x.Expected):N2}</td>");
            sb.AppendLine($"<td class=\"has-text-right\">{entries.Sum(x => x.Total):N2}</td>");
            sb.AppendLine($"<td class=\"has-text-right\">{(100d / entries.Sum(x => x.BillableBase) * entries.Sum(x => x.BillableActual)).FormatBillable()}</td>");
            sb.AppendLine($"<td class=\"has-text-right\">{entries.Sum(x => x.Overtime).FormatColor()}</td>");
            sb.AppendLine($"<td class=\"has-text-right\">{entries.Last().RollingOvertime.FormatColor()}</td>");
            sb.AppendLine($"<td class=\"has-text-right\">{entries.Count(x => x.IsHomeOffice)}</td>");
            sb.AppendLine($"<td class=\"has-text-right\">{entries.Count(x => x.Specials.Any(y => y.Value > 0 && y.Key == CalculationService.Special.Holiday))}</td>");
            sb.AppendLine($"<td class=\"has-text-right\">{entries.Count(x => x.Specials.Any(y => y.Value > 0 && y.Key == CalculationService.Special.Sick))}</td>");
            sb.AppendLine($"<td class=\"has-text-right\">{entries.Count(x => x.Specials.Any(y => y.Value > 0 && y.Key == CalculationService.Special.SpecialVacation))}</td>");
            sb.AppendLine($"<td class=\"has-text-right\">{entries.Count(x => x.Specials.Any(y => y.Value > 0 && y.Key == CalculationService.Special.Vacation))}</td>");
            sb.AppendLine($"<td class=\"has-text-right\">{entries.Last().RollingVacationInDays.FormatColor()}</td>");
            sb.AppendLine("</tr>");

            loopDate = loopDate.AddMonths(1);
        }

        sb.AppendLine("</tbody>");

        sb.AppendLine("<tfoot>");
        sb.AppendLine("<tr>");
        sb.AppendLine("<th></th>");
        sb.AppendLine("<th></th>");
        sb.AppendLine("<th></th>");
        var allEntries = calculationService.Days.OrderBy(x => x.Key).Select(x => x.Value).ToList();
        sb.AppendLine($"<td class=\"has-text-right\">{(100d / allEntries.Sum(x => x.BillableBase) * allEntries.Sum(x => x.BillableActual)).FormatBillable()}</td>");
        sb.AppendLine("<th></th>");
        sb.AppendLine($"<th class=\"has-text-right\">{allEntries.Last().RollingOvertime.FormatColor()}</th>");
        sb.AppendLine($"<th class=\"has-text-right\">{allEntries.Count(x => x.IsHomeOffice)}</th>");
        sb.AppendLine($"<th class=\"has-text-right\">{allEntries.Count(x => x.Specials.Any(y => y.Value > 0 && y.Key == CalculationService.Special.Holiday))}</th>");
        sb.AppendLine($"<th class=\"has-text-right\">{allEntries.Count(x => x.Specials.Any(y => y.Value > 0 && y.Key == CalculationService.Special.Sick))}</th>");
        sb.AppendLine($"<th class=\"has-text-right\">{allEntries.Count(x => x.Specials.Any(y => y.Value > 0 && y.Key == CalculationService.Special.SpecialVacation))}</th>");
        sb.AppendLine($"<th class=\"has-text-right\">{allEntries.Count(x => x.Specials.Any(y => y.Value > 0 && y.Key == CalculationService.Special.Vacation))}</th>");
        sb.AppendLine($"<th class=\"has-text-right\">{allEntries.Last().RollingVacationInDays.FormatColor(hideWhenZero: false)}</th>");
        sb.AppendLine("</tr>");
        sb.AppendLine("</tfoot>");
        sb.AppendLine("</table>");

        sb.AppendLine($"<div class=\"is-size-7 has-text-grey-light\">* Urlaubsanspruch wird auf Basis des aktuellen Anstellungsausmaßes von {calculationService.GetExpectedHours(DateTime.Now):N2}h/Tag berechnet. Verrechenbare Stunden werden exklusive Urlaub, Krankenstand und Feiertage berechnet.</div>");

        if (calculationService.Days.Any(x => x.Value.Warnings.Any()))
        {
            sb.AppendLine("<h2 class=\"title is-5\" style=\"margin: 2rem 0 0 0;\">Fehlerhafte Angaben</h2>");
            sb.AppendLine("<table class=\"table is-size-7\" style=\"margin: 2rem 0 0 0;\">");
            sb.AppendLine("<thead><tr>");
            sb.AppendLine("<th>Tag</th>");
            sb.AppendLine("<th>Anmerkungen</th>");
            sb.AppendLine("</tr></thead>");
            sb.AppendLine("<tbody>");
            foreach (var day in calculationService.Days.Where(x => x.Value.Warnings.Any()).OrderBy(x => x.Key).Select(x => x.Value))
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{day.Date:dd MMMM yyyy}</td>");
                sb.AppendLine("<td>");
                foreach (var w in day.Warnings)
                {
                    sb.Append($"<div><span class=\"tag is-danger\">{w}</span></div>");
                }

                sb.AppendLine("</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table>");
        }

        sb.AppendLine("</div>");
    }
}