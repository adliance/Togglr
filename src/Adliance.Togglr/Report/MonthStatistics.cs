using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Adliance.Togglr.Extensions;

namespace Adliance.Togglr.Report;

public static class MonthStatistics
{
    public static void WriteEveryMonth(StringBuilder sb, UserData userData)
    {
        sb.AppendLine("<div class=\"container\">");
        sb.AppendLine("<table class=\"table is-size-7 is-fullwidth\" style=\"margin: 2rem 0 0 0;\">");
        sb.AppendLine("<thead><tr>");
        sb.AppendLine("<th>Monat</th>");
        sb.AppendLine("<th class=\"has-text-right\">Soll (h)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Ist (h)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Verr. (%)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Überst. (h)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Saldo (h)</th>");
        sb.AppendLine("<th class=\"has-text-right\">HomeOffice (T)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Dienstreise (h)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Feiertag (T)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Krankenstand (T)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Sonderurlaub (T)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Urlaub (T)</th>");
        sb.AppendLine("<th class=\"has-text-right\">Urlaubsanspr. (T)*</th>");
        sb.AppendLine("</tr></thead>");
        sb.AppendLine("<tbody>");

        if (!userData.Days.Any()) return;

        var minDate = userData.Days.OrderBy(x => x.Key).Select(x => x.Value).First();
        var maxDate = userData.Days.OrderBy(x => x.Key).Select(x => x.Value).Last();
        var loopDate = new DateTime(minDate.Date.Year, minDate.Date.Month, 1);
        while (loopDate <= maxDate.Date)
        {
            var entries = userData.Days.Where(x => x.Key.Year == loopDate.Year && x.Key.Month == loopDate.Month).Select(x => x.Value).OrderBy(x => x.Date).ToList();
            sb.AppendLine("<tr>");
            sb.AppendLine(CultureInfo.CurrentCulture, $"<td>{loopDate:MMMM yyyy}</td>");
            sb.AppendLine(CultureInfo.CurrentCulture, $"<td class=\"has-text-right\">{entries.Sum(x => x.Expected).HideWhenZero()}</td>");
            sb.AppendLine(CultureInfo.CurrentCulture, $"<td class=\"has-text-right\">{entries.Sum(x => x.Total).HideWhenZero()}</td>");
            sb.AppendLine(CultureInfo.CurrentCulture, $"<td class=\"has-text-right\">{(100d / entries.Sum(x => x.BillableBase) * entries.Sum(x => x.BillableActual)).FormatBillable()}</td>");
            sb.AppendLine(CultureInfo.CurrentCulture, $"<td class=\"has-text-right\">{entries.Sum(x => x.Overtime).FormatColor()}</td>");
            sb.AppendLine(CultureInfo.CurrentCulture, $"<td class=\"has-text-right\">{entries.Last().RollingOvertime.FormatColor()}</td>");
            sb.AppendLine(CultureInfo.CurrentCulture, $"<td class=\"has-text-right\">{entries.Count(x => x.IsHomeOffice).HideWhenZero()}</td>");
            sb.AppendLine(CultureInfo.CurrentCulture, $"<td class=\"has-text-right\">{entries.Sum(x => x.BusinessTripHours).HideWhenZero()}</td>");
            sb.AppendLine(CultureInfo.CurrentCulture, $"<td class=\"has-text-right\">{entries.Count(x => x.IsHoliday).HideWhenZero()}</td>");
            sb.AppendLine(CultureInfo.CurrentCulture, $"<td class=\"has-text-right\">{entries.Count(x => x.IsSickDay).HideWhenZero()}</td>");
            sb.AppendLine(CultureInfo.CurrentCulture, $"<td class=\"has-text-right\">{entries.Count(x => x.IsSpecialVacation).HideWhenZero()}</td>");
            sb.AppendLine(CultureInfo.CurrentCulture, $"<td class=\"has-text-right\">{entries.Count(x => x.IsVacation).HideWhenZero()}</td>");
            sb.AppendLine(CultureInfo.CurrentCulture,
                $"<td class=\"has-text-right\" title=\"{entries.Last().RollingVacationInHours:N2} hours vacation\">{entries.Last().RollingVacationInDays.FormatColor()}</td>");
            sb.AppendLine("</tr>");

            loopDate = loopDate.AddMonths(1);
        }

        sb.AppendLine("</tbody>");

        sb.AppendLine("<tfoot>");
        sb.AppendLine("<tr>");
        sb.AppendLine("<th></th>");
        sb.AppendLine("<th></th>");
        sb.AppendLine("<th></th>");
        var allEntries = userData.Days.OrderBy(x => x.Key).Select(x => x.Value).ToList();
        sb.AppendLine(CultureInfo.CurrentCulture, $"<td class=\"has-text-right\">{(100d / allEntries.Sum(x => x.BillableBase) * allEntries.Sum(x => x.BillableActual)).FormatBillable()}</td>");
        sb.AppendLine("<th></th>");
        sb.AppendLine(CultureInfo.CurrentCulture, $"<th class=\"has-text-right\">{allEntries.Last().RollingOvertime.FormatColor()}</th>");
        sb.AppendLine(CultureInfo.CurrentCulture, $"<th class=\"has-text-right\">{allEntries.Count(x => x.IsHomeOffice)}</th>");
        sb.AppendLine(CultureInfo.CurrentCulture, $"<th class=\"has-text-right\">{allEntries.Sum(x => x.BusinessTripHours):N2}</th>");
        sb.AppendLine(CultureInfo.CurrentCulture, $"<th class=\"has-text-right\">{allEntries.Count(x => x.Specials.Any(y => y is { Value: > 0, Key: UserData.Special.Holiday }))}</th>");
        sb.AppendLine(CultureInfo.CurrentCulture, $"<th class=\"has-text-right\">{allEntries.Count(x => x.Specials.Any(y => y is { Value: > 0, Key: UserData.Special.Sick }))}</th>");
        sb.AppendLine(CultureInfo.CurrentCulture,
            $"<th class=\"has-text-right\">{allEntries.Count(x => x.Specials.Any(y => y is { Value: > 0, Key: UserData.Special.SpecialVacation }))}</th>");
        sb.AppendLine(CultureInfo.CurrentCulture, $"<th class=\"has-text-right\">{allEntries.Count(x => x.Specials.Any(y => y is { Value: > 0, Key: UserData.Special.Vacation }))}</th>");
        sb.AppendLine(CultureInfo.CurrentCulture, $"<th class=\"has-text-right\">{allEntries.Last().RollingVacationInDays.FormatColor(hideWhenZero: false)}</th>");
        sb.AppendLine("</tr>");
        sb.AppendLine("</tfoot>");
        sb.AppendLine("</table>");

        sb.AppendLine(CultureInfo.CurrentCulture, $"<div class=\"is-size-7 has-text-grey-light\">* Urlaubsanspruch wird auf Basis des aktuellen Anstellungsausmaßes von {userData.GetExpectedHours(DateTime.Now, true):N2}h/Tag berechnet. Verrechenbare Stunden werden exklusive Urlaub, Krankenstand und Feiertage berechnet.</div>");

        if (userData.Days.Any(x => x.Value.Warnings.Any()))
        {
            sb.AppendLine("<h2 class=\"title is-5\" style=\"margin: 2rem 0 0 0;\">Fehlerhafte Angaben</h2>");
            sb.AppendLine("<table class=\"table is-size-7 is-fullwidth\" style=\"margin: 2rem 0 0 0;\">");
            sb.AppendLine("<thead><tr>");
            sb.AppendLine("<th>Tag</th>");
            sb.AppendLine("<th>Anmerkungen</th>");
            sb.AppendLine("</tr></thead>");
            sb.AppendLine("<tbody>");
            foreach (var day in userData.Days.Where(x => x.Value.Warnings.Any()).OrderBy(x => x.Key).Select(x => x.Value))
            {
                sb.AppendLine("<tr>");
                sb.AppendLine(CultureInfo.CurrentCulture, $"<td>{day.Date:dd MMMM yyyy}</td>");
                sb.AppendLine("<td>");
                foreach (var w in day.Warnings)
                {
                    sb.Append(CultureInfo.CurrentCulture, $"<div><span class=\"tag is-danger\">{w}</span></div>");
                }

                sb.AppendLine("</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table>");
        }

        sb.AppendLine("</div>");
    }
}
