using System;
using System.Collections.Generic;
using System.Linq;
using Adliance.Togglr.Extensions;
using TogglApi.Client.Reports.Models.Response;

namespace Adliance.Togglr.Report;

public class UserData
{
    public UserConfiguration User { get; }
    public IDictionary<DateTime, Day> Days { get; }
    public IDictionary<(int year, int month, int weekNumber), Week> Weeks { get; private set; }

    public UserData(UserConfiguration user, IList<DetailedReportDatum> entries, DateTime homeOfficeStart)
    {
        User = user;
        var days = new List<Day>();
        foreach (var dayPair in entries.Where(x => x.Start.Date >= user.Begin.Date && x.Start.Date < user.End.Date.AddDays(1)).GroupByDay())
        {
            var d = new Day(dayPair.Key.Date)
            {
                Total = dayPair.Value.Sum(x => (x.End - x.Start).TotalHours),
                Billable = dayPair.Value.Where(x => x.IsBillable()).Sum(x => (x.End - x.Start).TotalHours),
                Specials =
                {
                    [Special.Doctor] = dayPair.Value.Where(x => x.IsDoctor()).Sum(x => (x.End - x.Start).TotalHours),
                    [Special.Holiday] = dayPair.Value.Where(x => x.IsHoliday()).Sum(x => (x.End - x.Start).TotalHours),
                    [Special.PersonalHoliday] = dayPair.Value.Where(x => x.IsPersonalHoliday()).Sum(x => (x.End - x.Start).TotalHours),
                    [Special.Sick] = dayPair.Value.Where(x => x.IsSick()).Sum(x => (x.End - x.Start).TotalHours),
                    [Special.Vacation] = dayPair.Value.Where(x => x.IsVacation()).Sum(x => (x.End - x.Start).TotalHours),
                    [Special.SpecialVacation] = dayPair.Value.Where(x => x.IsSpecialVacation()).Sum(x => (x.End - x.Start).TotalHours),
                    [Special.LegacyVacationHolidaySick] = dayPair.Value.Where(x => x.IsLegacyVacationHolidaySick()).Sum(x => (x.End - x.Start).TotalHours)
                },
                Expected = GetExpectedHours(dayPair.Key, false),
                HasEntryForHomeOffice = dayPair.Value.Any(x =>
                    x.Description.Contains("homeoffice", StringComparison.OrdinalIgnoreCase) || x.Description.Contains("home office", StringComparison.OrdinalIgnoreCase)),
                Start = dayPair.Value.Any() ? dayPair.Value.Select(x => x.Start).Min() : null,
                End = dayPair.Value.Any() ? dayPair.Value.Select(x => x.End).Max() : null,
                BusinessTripHours = dayPair.Value.Where(x => x.Description.Contains("dienstreise", StringComparison.OrdinalIgnoreCase)).Sum(x => (x.End - x.Start).TotalHours)
            };

            if (d.HasEntryForHomeOffice && d.Date >= homeOfficeStart.Date && !d.Specials.Where(x => x.Key != Special.Doctor).Any(x => x.Value > 0))
            {
                d.IsHomeOffice = true;
            }
            else if (d.Date < new DateTime(2022, 07, 1) && d.Date >= homeOfficeStart.Date && !d.Specials.Any(x => x.Value > 0))
            {
                // before July 2022 we used fixed days for HomeOffice
                // after that we only use the entry description for specifying home office days
                d.IsHomeOffice = user.HomeOfficeWeekdays.Contains(d.DayOfWeek);
                if (user.HomeOfficeDeviation.Any(x => x.Date == d.Date)) d.IsHomeOffice = !d.IsHomeOffice;
            }
            else
            {
                d.IsHomeOffice = false;
            }

            DetailedReportDatum? previousEntry = null;
            foreach (var e in dayPair.Value.OrderBy(x => x.Start))
            {
                if (previousEntry != null)
                {
                    var difference = e.Start - previousEntry.End;
                    d.Breaks += difference.TotalHours;

                    if (Math.Round(difference.TotalMinutes, 9) >= 30)
                    {
                        d.BreakStart = previousEntry.End;
                        d.BreakEnd = e.Start;
                    }
                }

                previousEntry = e;
            }

            days.Add(d);
        }

        Days = days.OrderBy(x => x.Date).ToDictionary(x => x.Date, x => x);
        Weeks = new Dictionary<(int year, int month, int weekNumber), Week>();
        AddMissingDays(User.Begin.Date, User.End.Date);
        Days = Days.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        CalculateRollingOvertime();
        CalculateRollingVacation();
        CalculateWeeks();
        AddWarnings(user.IgnoreBreakWarnings);
    }

    public UserData(UserData userData, DateTime min, DateTime max)
    {
        User = userData.User;
        if (min.Date < User.Begin) min = User.Begin.Date;
        if (max.Date > User.End) max = User.End.Date;
        if (max.Date > DateTime.Today) max = DateTime.Today;

        Days = userData.Days
            .Where(x => x.Key >= min.Date && x.Key <= max.Date)
            .ToDictionary(x => x.Key, x => x.Value);

        Weeks = new Dictionary<(int year, int month, int weekNumber), Week>();
        if (!Days.Any()) return;
        AddMissingDays(min, max);
        Days = Days.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        CalculateRollingOvertime();
        CalculateRollingVacation();
        CalculateWeeks();
        AddWarnings(userData.User.IgnoreBreakWarnings);
    }

    private void AddMissingDays(DateTime min, DateTime max)
    {
        var loopDate = min;
        while (loopDate <= max.Date)
        {
            if (!Days.ContainsKey(loopDate))
            {
                Days.TryGetValue(loopDate.AddDays(-1), out var previousEntry);
                Days[loopDate] = new Day(loopDate, previousEntry?.RollingOvertime ?? 0)
                {
                    Expected = GetExpectedHours(loopDate, false)
                };
            }

            loopDate = loopDate.AddDays(1);
        }
    }

    private void CalculateRollingOvertime()
    {
        var rollingOvertime = User.ResetOvertime;
        foreach (var d in Days.OrderBy(x => x.Key).Select(x => x.Value))
        {
            var differentWorkTime = User.DifferentWorkTimes.FirstOrDefault(x => x.Begin.Date == d.Date && x.ResetOvertime.HasValue);
            if (differentWorkTime != null)
            {
                // ReSharper disable once PossibleInvalidOperationException
                rollingOvertime = differentWorkTime.ResetOvertime!.Value;
            }

            rollingOvertime += d.Overtime;
            d.RollingOvertime = rollingOvertime;
        }
    }

    private void CalculateRollingVacation()
    {
        // the expected hours based on the current date/time
        var currentExpectedHours = GetExpectedHours(DateTime.UtcNow, true);
        var rollingVacation = User.ResetHolidays;
        foreach (var d in Days.OrderBy(x => x.Key).Select(x => x.Value))
        {
            var differentWorkTime = User.DifferentWorkTimes.FirstOrDefault(x => x.Begin.Date == d.Date && x.ResetHolidays.HasValue);
            if (differentWorkTime != null)
            {
                // ReSharper disable once PossibleInvalidOperationException
                rollingVacation = differentWorkTime.ResetHolidays!.Value;
            }

            var expected = GetExpectedHours(d.Date, true);

            // we need to make sure that the vacancy hours each day are calculated based on the number of workdays (so that the hours per day stay the same, regardless if it's 5 or 4 hour work week)
            var vacation = expected * (25.0 / 5.0 * GetExpectedNumberOfWorkdays(d.Date)) / (DateTime.IsLeapYear(d.Date.Year) ? 366.0 : 365.0);
            rollingVacation += vacation;
            rollingVacation -= d.Specials[Special.Vacation];
            d.VacationInHours = vacation;
            d.RollingVacationInDays = rollingVacation / currentExpectedHours;
            d.RollingVacationInHours = rollingVacation;
        }
    }

    private void CalculateWeeks()
    {
        Week? currentWeek = null;
        foreach (var d in Days.OrderBy(x => x.Key).Select(x => x.Value))
        {
            if (d.Date.Day == 1 || d.Date.DayOfWeek == DayOfWeek.Monday || currentWeek == null)
            {
                currentWeek = new Week();
            }

            currentWeek.Expected += d.Expected;
            currentWeek.Billable += d.Billable;
            currentWeek.Total += d.Total;
            if (d.IsHomeOffice)
            {
                currentWeek.HasEntryForHomeOffice = true;
            }

            currentWeek.RollingOvertime = d.RollingOvertime;
            currentWeek.RollingVacationInDays = d.RollingVacationInDays;
            currentWeek.RollingVacationInHours = d.RollingVacationInHours;
            currentWeek.VacationInHours += d.VacationInHours;
            currentWeek.BusinessTripHours += d.BusinessTripHours;
            currentWeek.BreakDuration += d.Breaks;

            Weeks[(d.Date.Year, d.Date.Month, d.Date.GetWeekNumber())] = currentWeek;
        }
    }

    private void AddWarnings(bool ignoreBreakWarnings)
    {
        foreach (var d in Days.Select(x => x.Value))
        {
            if (!ignoreBreakWarnings)
            {
                if (Math.Round(d.Total - d.Specials.Sum(x => x.Value), 9) > 6 && !d.Has30MinutesBreak)
                {
                    d.Warnings.Add("Pause von mindestens 30 Minuten fehlt.");
                }
            }

            if (d.Specials[Special.LegacyVacationHolidaySick] > 0)
            {
                d.Warnings.Add("Es sind noch Stunden auf das ungültige, veraltete Urlaub/Krankenstand/Feiertag Sammel-Projekt gebucht.");
            }

            if (d.Specials[Special.Holiday] > 0 && Math.Abs(d.Specials[Special.Holiday] - d.Expected) > 0.01)
            {
                d.Warnings.Add($"Feiertag ist für {d.Specials[Special.Holiday]:N2}h eingetragen, es müssten aber {d.Expected:N2}h sein.");
            }

            if (d.Specials[Special.PersonalHoliday] > 0 && Math.Abs(d.Specials[Special.PersonalHoliday] - d.Expected) > 0.01)
            {
                d.Warnings.Add($"Persönlicher Feiertag ist für {d.Specials[Special.PersonalHoliday]:N2}h eingetragen, es müssten aber {d.Expected:N2}h sein.");
            }
            else if (d.Specials[Special.Sick] > 0 && Math.Abs(d.Specials[Special.Sick] - d.Expected) > 0.01)
            {
                d.Warnings.Add($"Krankenstand ist für {d.Specials[Special.Sick]:N2}h eingetragen, es müssten aber {d.Expected:N2}h sein.");
            }
            else if (d.Specials[Special.Vacation] > 0 && Math.Abs(d.Specials[Special.Vacation] - d.Expected) > 0.01)
            {
                d.Warnings.Add($"Urlaub ist für {d.Specials[Special.Vacation]:N2}h eingetragen, es müssten aber {d.Expected:N2}h sein.");
            }
            else if (d.Specials[Special.SpecialVacation] > 0 && Math.Abs(d.Specials[Special.SpecialVacation] - d.Expected) > 0.01)
            {
                d.Warnings.Add($"Sonderurlaub ist für {d.Specials[Special.SpecialVacation]:N2}h eingetragen, es müssten aber {d.Expected:N2}h sein.");
            }

            if (d.Expected <= 0 && (d.Specials[Special.Holiday] > 0 || d.Specials[Special.Sick] > 0 || d.Specials[Special.Vacation] > 0 || d.Specials[Special.SpecialVacation] > 0))
            {
                d.Warnings.Add("Kein Arbeitstag, kein Urlaub/Sonderurlaub/Krankenstand/Feiertag möglich.");
            }

            var holidaysOfOthers = ReportService.AllEntries.Where(x => x.Start.Date == d.Date && x.IsHoliday()).ToList();
            if (d.Specials[Special.Holiday] <= 0 && holidaysOfOthers.Any())
            {
                // no warning when the user is not supposed to work on this day
                if (d.Expected <= 0) continue;

                // no warning on 24th and 31th of decemger
                if (d.Date is { Month: 12, Day: 24 or 31 }) continue;

                d.Warnings.Add($"Kein Feiertag eingetragen, aber {string.Join(", ", holidaysOfOthers.Select(x => x.User))} {(holidaysOfOthers.Count == 1 ? "hat" : "haben")} Feiertag eingetragen.");
            }
        }
    }

    public double GetExpectedHours(DateTime day, bool ignoreWeekday)
    {
        var result = User.HoursPerDay;
        var weekdays = User.Weekdays;

        var differentWorkTime = User.DifferentWorkTimes.FirstOrDefault(x => day.Date >= x.Begin.Date && day.Date <= x.End.Date.AddDays(1).AddSeconds(-1));
        if (differentWorkTime != null)
        {
            result = differentWorkTime.HoursPerDay;
            weekdays = differentWorkTime.Weekdays;
        }

        if (ignoreWeekday) return result;

        // if the user does not work on this day of week, return 0;
        if (!weekdays.Any(x => x.Equals(day.DayOfWeek.ToString().Substring(0, 3), StringComparison.OrdinalIgnoreCase))) return 0;

        return result;
    }

    public int GetExpectedNumberOfWorkdays(DateTime day)
    {
        var weekdays = User.Weekdays;

        var differentWorkTime = User.DifferentWorkTimes.FirstOrDefault(x => day.Date >= x.Begin.Date && day.Date <= x.End.Date.AddDays(1).AddSeconds(-1));
        if (differentWorkTime != null)
        {
            weekdays = differentWorkTime.Weekdays;
        }

        return weekdays.Count();
    }

    public class Day
    {
        public Day(DateTime date, double rollingOvertime = 0)
        {
            Date = date;
            RollingOvertime = rollingOvertime;
            Specials[Special.Doctor] = 0;
            Specials[Special.Holiday] = 0;
            Specials[Special.PersonalHoliday] = 0;
            Specials[Special.Sick] = 0;
            Specials[Special.Vacation] = 0;
            Specials[Special.SpecialVacation] = 0;
            Specials[Special.LegacyVacationHolidaySick] = 0;
        }

        public DateTime Date { get; }
        public double Total { get; set; }

        public double Billable { get; set; }

        public double BillableActual => Billable - Specials.Where(x => !new[]
        {
            Special.Doctor
        }.Contains(x.Key)).Sum(x => x.Value);

        public double BillableBase => Total - Specials.Where(x => !new[]
        {
            Special.Doctor
        }.Contains(x.Key)).Sum(x => x.Value);

        public double Expected { get; set; }
        public double BusinessTripHours { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public DateTime? BreakStart { get; set; }
        public DateTime? BreakEnd { get; set; }
        public double Breaks { get; set; }
        public bool Has30MinutesBreak => BreakStart.HasValue && BreakEnd.HasValue && Math.Round((BreakEnd.Value - BreakStart.Value).TotalMinutes, 9) >= 30;

        public double Overtime => Total - Expected;
        public double RollingOvertime { get; set; }
        public bool IsHomeOffice { get; set; }
        public bool IsHoliday => Specials.Any(y => y is { Value: > 0, Key: Special.Holiday or Special.PersonalHoliday or Special.LegacyVacationHolidaySick });
        public bool IsSickDay => Specials.Any(y => y is { Value: > 0, Key: Special.Sick });
        public bool IsVacation => Specials.Any(y => y is { Value: > 0, Key: Special.Vacation });
        public bool IsSpecialVacation => Specials.Any(y => y is { Value: > 0, Key: Special.SpecialVacation });

        public bool HasEntryForHomeOffice { get; set; }
        public double RollingVacationInDays { get; set; }
        public double RollingVacationInHours { get; set; }
        public double VacationInHours { get; set; }
        public DayOfWeek DayOfWeek => Date.DayOfWeek;
        public IDictionary<Special, double> Specials { get; } = new Dictionary<Special, double>();
        public IList<string> Warnings { get; } = new List<string>();

        public bool PrintTimes
        {
            get
            {
                foreach (var s in Specials)
                {
                    if (s.Value > 0 && new[]
                        {
                            Special.Holiday,
                            Special.Sick,
                            Special.Vacation,
                            Special.PersonalHoliday,
                            Special.SpecialVacation,
                            Special.LegacyVacationHolidaySick
                        }.Contains(s.Key)) return false;
                }

                return true;
            }
        }
    }

    public class Week
    {
        public Week(double rollingOvertime = 0)
        {
            RollingOvertime = rollingOvertime;
            Specials[Special.Doctor] = 0;
            Specials[Special.Holiday] = 0;
            Specials[Special.PersonalHoliday] = 0;
            Specials[Special.Sick] = 0;
            Specials[Special.Vacation] = 0;
            Specials[Special.SpecialVacation] = 0;
            Specials[Special.LegacyVacationHolidaySick] = 0;
        }

        public double Total { get; set; }

        public double Billable { get; set; }

        public double BillableActual => Billable - Specials.Where(x => !new[]
        {
            Special.Doctor
        }.Contains(x.Key)).Sum(x => x.Value);

        public double BillableBase => Total - Specials.Where(x => !new[]
        {
            Special.Doctor
        }.Contains(x.Key)).Sum(x => x.Value);

        public double BusinessTripHours { get; set; }
        public double BreakDuration { get; set; }
        public double Expected { get; set; }
        public double Overtime => Total - Expected;
        public double RollingOvertime { get; set; }

        public bool HasEntryForHomeOffice { get; set; }
        public double RollingVacationInDays { get; set; }
        public double RollingVacationInHours { get; set; }
        public double VacationInHours { get; set; }
        public IDictionary<Special, double> Specials { get; } = new Dictionary<Special, double>();
    }

    public enum Special
    {
        Vacation,
        SpecialVacation,
        Holiday,
        PersonalHoliday,
        Sick,
        Doctor,
        LegacyVacationHolidaySick
    }
}

public static class SpecialExtensions
{
    public static string GetName(this UserData.Special special, Configuration configuration)
    {
        return special switch
        {
            UserData.Special.Vacation => configuration.ProjectNameVacation,
            UserData.Special.SpecialVacation => configuration.ProjectNameSpecialVacation,
            UserData.Special.Holiday => configuration.ProjectNameHoliday,
            UserData.Special.PersonalHoliday => configuration.ProjectNamePersonalHoliday,
            UserData.Special.Sick => configuration.ProjectNameSick,
            UserData.Special.Doctor => configuration.ProjectNameDoctor,
            _ => configuration.ProjectNameLegacyVacationHolidaySick
        };
    }
}
