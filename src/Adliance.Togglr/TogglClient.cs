using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using TogglApi.Client.Reports;
using TogglApi.Client.Reports.Models;
using TogglApi.Client.Reports.Models.Request;
using TogglApi.Client.Reports.Models.Response;

namespace Adliance.Togglr;

public class TogglClient (Configuration configuration)
{
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private readonly DateTime _useOldEntriesUpToDate = DateTime.UtcNow.AddDays(-configuration.UseOldEntriesXDaysBack);

    private async Task<IEnumerable<DetailedReportDatum>> DownloadEntries(DateTime from, DateTime to)
    {
        _logger.Info($"Downloading time entries from Toggl from {from:yyyy-MM-dd} to {to:yyyy-MM-dd} ...");

        var client = new TogglReportClient(new HttpClient(), _logger);

        var detailedReports = (await client.GetDetailedReport(new DetailedReportConfig
        (
            userAgent: "Adliance.Togglr by Adliance GmbH",
            workspaceId: configuration.WorkspaceId, //Adliance workspace
            since: from,
            until: to,
            billableOptions: BillableOptions.Both
        ), apiToken: configuration.ApiToken)).Data.OrderBy(x => x.Start);

        return detailedReports;
    }

    public async Task DownloadEntriesAndStoreLocally()
    {
        var entries = new List<DetailedReportDatum>();

        // check if file with old entries already exists; otherwise download all entries
        if (!File.Exists("entries.json"))
        {
            for (var i = 2015; i <= DateTime.UtcNow.Year; i++)
            {
                entries.AddRange(await DownloadEntries(
                    new DateTime(i, 1, 1),
                    new DateTime(i, 12, 31).AddDays(1).AddSeconds(-1)));
            }

            await File.WriteAllTextAsync("entries.json", JsonConvert.SerializeObject(entries));
        }
        else
        {
            // determine last entry in existing file
            var existingEntries = JsonConvert.DeserializeObject<List<DetailedReportDatum>>(await File.ReadAllTextAsync("entries.json")) ?? new List<DetailedReportDatum>();
            var lastRelevantEntry = existingEntries.Where(x => x.End < _useOldEntriesUpToDate).OrderBy(x => x.End).Last();

            // load new entries from date of last relevant entry on
            entries.AddRange(await DownloadEntries(
                new DateTime(lastRelevantEntry.Start.Year, lastRelevantEntry.Start.Month, lastRelevantEntry.Start.Day),
                new DateTime(DateTime.UtcNow.Year, 12, 31).AddDays(1).AddSeconds(-1)));

            var allEntries = existingEntries.Concat(entries).ToList();
            allEntries = allEntries.GroupBy(x => x.Id)
                .Select(x => x.OrderByDescending(y => y.Updated).First()).ToList();
            await File.WriteAllTextAsync("entries.json", JsonConvert.SerializeObject(allEntries));
        }

        _logger.Info($"Downloaded a total of {entries.Count:N0} time entries.");
    }

    public List<DetailedReportDatum> LoadEntriesLocallyAndFix()
    {
        var entries = JsonConvert.DeserializeObject<List<DetailedReportDatum>>(File.ReadAllText("entries.json")) ?? new List<DetailedReportDatum>();

        var fixedEntries = new List<DetailedReportDatum>();
        foreach (var entry in entries)
        {
            var fixedEntry = entry;

            var startDate = new DateTime(fixedEntry.Start.Year, fixedEntry.Start.Month, fixedEntry.Start.Day, fixedEntry.Start.Hour, fixedEntry.Start.Minute, 0);
            var endDate = new DateTime(fixedEntry.End.Year, fixedEntry.End.Month, fixedEntry.End.Day, fixedEntry.End.Hour, fixedEntry.End.Minute, 0);

            // dirty hack to work around time zones and time entries that start at 00:00 (usually holiday/vacation)
            while (startDate.Date != endDate.Date)
            {
                try
                {
                    startDate = startDate.AddHours(1);
                    endDate = endDate.AddHours(1);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable to correct dates {startDate} or {endDate}: {ex.Message}");
                }
            }

            fixedEntry = new DetailedReportDatum(
                fixedEntry.Id,
                fixedEntry.ProjectId,
                fixedEntry.TaskId,
                fixedEntry.UserId,
                fixedEntry.Description,
                startDate,
                endDate,
                fixedEntry.Updated,
                fixedEntry.DurationMs,
                fixedEntry.User,
                fixedEntry.UseStop,
                fixedEntry.Client,
                fixedEntry.Project,
                fixedEntry.ProjectColor,
                fixedEntry.ProjectHexColor,
                fixedEntry.Task,
                entry.BillableTimeMs,
                fixedEntry.IsBillable,
                fixedEntry.Currency,
                fixedEntry.Tags
            );

            fixedEntries.Add(fixedEntry);
        }

        return fixedEntries;
    }
}
