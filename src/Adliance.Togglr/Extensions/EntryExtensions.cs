using System;
using System.Linq;
using TogglApi.Client.Reports.Models.Response;

namespace Adliance.Togglr.Extensions;

public static class EntryExtensions
{
    private static readonly string[] NonBillableProjects = {
        "Nicht verrechenbar",
        "Akriva",
        "Adliance Website",
        "Zertifizierung, QM"
    };

    public static bool IsBillable(this DetailedReportDatum entry)
    {
        if (string.IsNullOrEmpty(entry.Project)) return false;
        return !NonBillableProjects.Any(x => (entry.Project ?? "").Contains(x, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsDoctor(this DetailedReportDatum entry)
    {
        return (entry.Project ?? "").Equals("Arztbesuch", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSick(this DetailedReportDatum entry)
    {
        return (entry.Project ?? "").Equals("Krankenstand", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsHoliday(this DetailedReportDatum entry)
    {
        return (entry.Project ?? "").Equals("Feiertag", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPersonalHoliday(this DetailedReportDatum entry)
    {
        return (entry.Project ?? "").Equals("Persönlicher Feiertag", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsVacation(this DetailedReportDatum entry)
    {
        return (entry.Project ?? "").Equals("Urlaub", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSpecialVacation(this DetailedReportDatum entry)
    {
        return (entry.Project ?? "").Equals("Sonderurlaub", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsLegacyVacationHolidaySick(this DetailedReportDatum entry)
    {
        return (entry.Project ?? "").Equals("Feiertag, Krankenstand, Urlaub", StringComparison.OrdinalIgnoreCase);
    }
}
