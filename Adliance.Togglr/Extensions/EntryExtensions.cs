using System;
using TogglApi.Client.Reports.Models.Response;

namespace Adliance.Togglr.Extensions
{
    public static class EntryExtensions
    {
        public static bool IsDoctor(this DetailedReportDatum entry)
        {
            return (entry.Project ?? "").Equals("Arztbesuch", StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsSick(this DetailedReportDatum entry)
        {
            return (entry.Project ?? "").Equals("Krankenstand", StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsHoliday(this DetailedReportDatum entry)
        {
            return (entry.Project ?? "").Equals("Feiertag", StringComparison.InvariantCultureIgnoreCase);
        }
        
        public static bool IsPersonalHoliday(this DetailedReportDatum entry)
        {
            return (entry.Project ?? "").Equals("Persönlicher Feiertag", StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsVacation(this DetailedReportDatum entry)
        {
            return (entry.Project ?? "").Equals("Urlaub", StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsSpecialVacation(this DetailedReportDatum entry)
        {
            return (entry.Project ?? "").Equals("Sonderurlaub", StringComparison.InvariantCultureIgnoreCase);
        }
        
        public static bool IsLegacyVacationHolidaySick(this DetailedReportDatum entry)
        {
            return (entry.Project ?? "").Equals("Feiertag, Krankenstand, Urlaub", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}