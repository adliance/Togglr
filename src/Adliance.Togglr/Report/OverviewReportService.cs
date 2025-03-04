using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Adliance.Togglr.Extensions;

namespace Adliance.Togglr.Report;

public class OverviewReportService(ReportParameter reportParameter, Configuration configuration)
{
    private readonly Html _html = new(reportParameter);


    // generate a unit test for that
    public async Task Run()
    {
        var minYear = UserDataService.MinDate.Year;
        var maxYear = UserDataService.MaxDate.Year;

        _html.Title($"Übersicht ({minYear}-{maxYear})");
        RenderTable(null, null);

        for (var i = maxYear; i >= minYear; i--)
        {
            _html.Spacer();
            _html.Title($"Übersicht ({i})");
            RenderTable(new DateTime(i, 1, 1), new DateTime(i, 12, 31));
        }

        await _html.SaveToFile("Overview");
    }

    private void RenderTable(DateTime? min, DateTime? max)
    {
        var printPerYear = false;
        if (min.HasValue && max.HasValue)
        {
            Program.Logger.Info($"\t... for {min.Value.Year}");
        }
        else
        {
            printPerYear = true;
            Program.Logger.Info("\t... for all time");
        }

        _html.TableStart(
            "Person",
            "Einträge",
            "Soll (h)",
            "Ist (h)",
            "Verr. (%)",
            "Überst. (h)",
            "HomeOffice (T)",
            "Feiertag (T)",
            "Krankenst. (T)",
            "Sonderurl. (T)",
            "Url. (T)",
            "Urlaubsanspr. (T)"
        );

        foreach (var u in configuration.Users.Where(x => x.CreateReport).OrderBy(x => x.Begin))
        {
            var userData = UserDataService.Get(u);
            if (userData == null) continue;
            if (min.HasValue && max.HasValue) userData = new UserData(userData, min.Value, max.Value);
            if (!userData.Days.Any()) continue;

            var days = userData.Days.Select(x => x.Value).ToList();

            var years = (days.Last().Date - days.First().Date).Days / 365d;
            var homeOfficeDays = days.Count(x => x.IsHomeOffice);
            var vacationDays = days.Count(x => x.IsVacation);
            var sickDays = days.Count(x => x.IsSickDay);
            var holidayDays = days.Count(x => x.IsHoliday);

            _html.TableRow(
                u.Name,
                $"<span title=\"{years:N2} Jahre\">" + userData.Days.First().Key.Format(false) + "-" + userData.Days.Last().Key.Format(false) + "</span>",
                days.Sum(x => x.Expected).HideWhenZero(),
                days.Sum(x => x.Total).HideWhenZero(),
                (100d / days.Sum(x => x.BillableBase) * days.Sum(x => x.BillableActual)).FormatBillable(),
                days.Last().RollingOvertime.FormatColor(),
                homeOfficeDays.HideWhenZero(),
                $"{holidayDays.HideWhenZero()}",
                $"{sickDays.HideWhenZero()}" + (printPerYear ? " (" + (sickDays / years).ToString("N0", CultureInfo.CurrentCulture) + "/J)" : ""),
                days.Count(x => x.IsSpecialVacation).HideWhenZero(),
                vacationDays.HideWhenZero(),
                $"<span title=\"{days.Last().RollingVacationInHours:N2} Stunden\">" + days.Last().RollingVacationInDays.FormatColor() + "</span>"
            );
        }

        _html.TableEnd();
    }
}
