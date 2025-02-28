using System;
using System.Collections.Generic;
using System.Linq;
using TogglApi.Client.Reports.Models.Response;

namespace Adliance.Togglr.Report;

public static class UserDataService
{
    private static readonly Dictionary<string, UserData> CachedUserData = new();

    public static DateTime MinDate { get; private set; } = DateTime.MinValue;
    public static DateTime MaxDate { get; private set; } = DateTime.MaxValue;

    public static UserData? Get(string user)
    {
        return CachedUserData.GetValueOrDefault(user);
    }

    public static UserData? Get(UserConfiguration user)
    {
        return Get(user.Name);
    }

    public static void CalculateForAllUsers(Configuration configuration, IList<DetailedReportDatum> allEntries)
    {
        allEntries = allEntries
            .Where(x => x.Start.Date >= configuration.Users.Min(y => y.Begin)) // ignore time entries before the first user started
            .OrderBy(x => x.Start).ToList();
        MinDate = allEntries.First().Start.Date;
        MaxDate = allEntries.Last().Start.Date;

        foreach (var user in configuration.Users)
        {
            user.End = user.End == default ? configuration.End ?? DateTime.UtcNow.Date : user.End;
            if (configuration.End.HasValue && configuration.End.Value < user.End) user.End = configuration.End.Value; // if we have a user end, and a global end, use the global end

            CachedUserData[user.Name] = new UserData(
                user,
                allEntries.Where(x => x.User.Equals(user.Name, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Start).ToList(),
                configuration.HomeOfficeStart ?? DateTime.MinValue);
        }
    }
}
