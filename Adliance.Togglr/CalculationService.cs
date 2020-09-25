using System;
using System.Collections.Generic;
using System.Linq;
using TogglApi.Client.Reports.Models.Response;
using Adliance.Togglr.Extensions;

namespace Adliance.Togglr
{
    public class CalculationService
    {
        private UserConfiguration User { get; }
        public IDictionary<DateTime, Day> Days { get; }

        public CalculationService(UserConfiguration user, IList<DetailedReportDatum> entries)
        {
            User = user;
            var days = new List<Day>();
            foreach (var dayPair in entries.Where(x => x.Start.Date >= user.Begin.Date && x.Start.Date < user.End.Date.AddDays(1)).GroupByDay())
            {
                var d = new Day(dayPair.Key.Date)
                {
                    Total = dayPair.Value.Sum(x => (x.End - x.Start).TotalHours),
                    Specials =
                    {
                        [Special.Doctor] = dayPair.Value.Where(x => x.IsDoctor()).Sum(x => (x.End - x.Start).TotalHours),
                        [Special.Holiday] = dayPair.Value.Where(x => x.IsHoliday()).Sum(x => (x.End - x.Start).TotalHours),
                        [Special.PersonalHoliday] = dayPair.Value.Where(x => x.IsPersonalHoliday()).Sum(x => (x.End - x.Start).TotalHours),
                        [Special.Sick] = dayPair.Value.Where(x => x.IsSick()).Sum(x => (x.End - x.Start).TotalHours),
                        [Special.Vacation] = dayPair.Value.Where(x => x.IsVacation()).Sum(x => (x.End - x.Start).TotalHours),
                        [Special.SpecialVacation] = dayPair.Value.Where(x => x.IsSpecialVacation()).Sum(x => (x.End - x.Start).TotalHours),
                        [Special.LegacyVacationHolidaySick] = dayPair.Value.Where(x => x.IsLegacyVacationHolidaySick()).Sum(x => (x.End - x.Start).TotalHours),
                    },
                    Expected = GetExpectedHours(dayPair.Key)
                };

                DetailedReportDatum? previousEntry = null;
                foreach (var e in dayPair.Value.OrderBy(x => x.Start))
                {
                    if (previousEntry != null)
                    {
                        var difference = e.Start - previousEntry.End;
                        d.Breaks += difference.TotalHours;
                        d.Has30MinutesBreak = d.Has30MinutesBreak || difference.TotalMinutes > 29.8; // don't use 30, because for some reason Toggl sometimes has times like 11:29:55 instead of 11:30:00
                    }

                    previousEntry = e;
                }

                days.Add(d);
            }

            Days = days.OrderBy(x => x.Date).ToDictionary(x => x.Date, x => x);
            AddMissingDays();
            CalculateRollingOvertime();
            CalculateRollingVacation();
            AddWarnings();
        }

        private void AddMissingDays()
        {
            var loopDate = User.Begin.Date;
            while (loopDate <= User.End.Date)
            {
                if (!Days.ContainsKey(loopDate))
                {
                    var previousDay = loopDate.AddDays(-1);
                    Days[loopDate] = new Day(loopDate, Days.ContainsKey(previousDay) ? Days[previousDay].RollingOvertime : 0)
                    {
                        Expected = GetExpectedHours(loopDate)
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
            var currentExpectedHours = GetExpectedHours(DateTime.UtcNow, false);
            var rollingVacation = User.ResetHolidays;
            foreach (var d in Days.OrderBy(x => x.Key).Select(x => x.Value))
            {
                var differentWorkTime = User.DifferentWorkTimes.FirstOrDefault(x => x.Begin.Date == d.Date && x.ResetHolidays.HasValue);
                if (differentWorkTime != null)
                {
                    // ReSharper disable once PossibleInvalidOperationException
                    rollingVacation = differentWorkTime.ResetHolidays!.Value;
                }

                var expected = GetExpectedHours(d.Date, false);
                rollingVacation += expected * 25.0 / (DateTime.IsLeapYear(d.Date.Year) ? 366.0 : 365.0);
                rollingVacation -= d.Specials[Special.Vacation];
                d.RollingVacationInDays = rollingVacation / currentExpectedHours;
            }
        }

        private void AddWarnings()
        {
            foreach (var d in Days.Select(x => x.Value))
            {
                if (d.Total - d.Specials.Sum(x => x.Value) > 6 && !d.Has30MinutesBreak)
                {
                    d.Warnings.Add("Pause von mindestens 30 Minuten fehlt.");
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
                    d.Warnings.Add($"Persönlicher Feiertag ist für {d.Specials[Special.Holiday]:N2}h eingetragen, es müssten aber {d.Expected:N2}h sein.");
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

                if (d.Date.IsWeekend() && (d.Specials[Special.Holiday] > 0 || d.Specials[Special.Sick] > 0 || d.Specials[Special.Vacation] > 0 || d.Specials[Special.SpecialVacation] > 0))
                {
                    d.Warnings.Add("Wochenende, kein Urlaub/Sonderurlaub/Krankenstand/Feiertag möglich.");
                }

                var holidaysOfOthers = TogglrReportGeneratorService.AllEntries.Where(x => x.Start.Date == d.Date && x.IsHoliday()).ToList();
                if (d.Specials[Special.Holiday] <= 0 && holidaysOfOthers.Any())
                {
                    if (d.Date.Month == 12 && (d.Date.Day == 24 || d.Date.Day == 31))
                    {
                        continue;
                    }

                    d.Warnings.Add($"Kein Feiertag eingetragen, aber {string.Join(", ", holidaysOfOthers.Select(x => x.User))} {(holidaysOfOthers.Count == 1 ? "hat" : "haben")} Feiertag eingetragen.");
                }
            }
        }

        public double GetExpectedHours(DateTime day, bool countWeekendAsZero = true)
        {
            if (countWeekendAsZero && day.IsWeekend())
            {
                return 0;
            }

            var result = User.HoursPerDay;

            var differentWorkTime = User.DifferentWorkTimes.FirstOrDefault(x => day.Date >= x.Begin.Date && day.Date.AddDays(1).AddSeconds(-1) <= x.End.Date);
            if (differentWorkTime != null)
            {
                result = differentWorkTime.HoursPerDay;
            }

            return result;
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
            public double Breaks { get; set; }
            public bool Has30MinutesBreak { get; set; }
            public double Expected { get; set; }
            public double Overtime => Total - Expected;
            public double RollingOvertime { get; set; }
            
            public double RollingVacationInDays { get; set; }

            public IDictionary<Special, double> Specials { get; } = new Dictionary<Special, double>();
            public IList<string> Warnings { get; } = new List<string>();
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
        public static string GetName(this CalculationService.Special special, Configuration configuration)
        {
            return special switch
            {
                CalculationService.Special.Vacation => configuration.ProjectNameVacation,
                CalculationService.Special.SpecialVacation => configuration.ProjectNameSpecialVacation,
                CalculationService.Special.Holiday => configuration.ProjectNameHoliday,
                CalculationService.Special.PersonalHoliday => configuration.ProjectNamePersonalHoliday,
                CalculationService.Special.Sick => configuration.ProjectNameSick,
                CalculationService.Special.Doctor => configuration.ProjectNameDoctor,
                _ => configuration.ProjectNameLegacyVacationHolidaySick
            };
        }
    }
}