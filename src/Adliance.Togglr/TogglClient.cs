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

public class TogglClient
{
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    private async Task<IEnumerable<DetailedReportDatum>> DownloadEntries(Configuration configuration, DateTime from, DateTime to)
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

    public async Task DownloadEntriesAndStoreLocally(Configuration configuration)
    {
        var entries = new List<DetailedReportDatum>();

        if (configuration.CacheEntriesUntilYear.HasValue)
        {
            // check if file with old entries already exists; otherwise download the old entries
            if (!File.Exists("entries_until_" + configuration.CacheEntriesUntilYear + ".json"))
            {
                for (var i = 2015; i <= configuration.CacheEntriesUntilYear; i++)
                {
                    entries.AddRange(await DownloadEntries(
                        configuration,
                        new DateTime(i, 1, 1),
                        new DateTime(i, 12, 31).AddDays(1).AddSeconds(-1)));
                }
                
                await File.WriteAllTextAsync("entries_until_" + configuration.CacheEntriesUntilYear + ".json", JsonConvert.SerializeObject(entries));
                entries = new List<DetailedReportDatum>();
            }
        }
        
        for (var i = configuration.CacheEntriesUntilYear + 1 ?? 2015; i <= DateTime.UtcNow.Year; i++)
        {
            entries.AddRange(await DownloadEntries(
                configuration,
                new DateTime(i, 1, 1),
                new DateTime(i, 12, 31).AddDays(1).AddSeconds(-1)));
        }

        _logger.Info($"Downloaded a total of {entries.Count:N0} time entries.");
        await File.WriteAllTextAsync("entries.json", JsonConvert.SerializeObject(entries));
    }

    public List<DetailedReportDatum> LoadEntriesLocallyAndFix(Configuration configuration)
    {
        var entries = JsonConvert.DeserializeObject<List<DetailedReportDatum>>(File.ReadAllText("entries.json")) ?? new List<DetailedReportDatum>();

        if (configuration.CacheEntriesUntilYear.HasValue)
        {
            var oldEntries =
                JsonConvert.DeserializeObject<List<DetailedReportDatum>>(
                    File.ReadAllText("entries_until_" + configuration.CacheEntriesUntilYear + ".json")) ??
                new List<DetailedReportDatum>();
            entries = entries.Concat(oldEntries).ToList();
        }
        
        var fixedEntries = new List<DetailedReportDatum>();
        foreach (var entry in entries)
        {
            var fixedEntry = entry;

            var startDate = new DateTime(fixedEntry.Start.Year, fixedEntry.Start.Month, fixedEntry.Start.Day, fixedEntry.Start.Hour, fixedEntry.Start.Minute, 0);
            var endDate = new DateTime(fixedEntry.End.Year, fixedEntry.End.Month, fixedEntry.End.Day, fixedEntry.End.Hour, fixedEntry.End.Minute, 0);

            // dirty hack to work around time zones and time entries that start at 00:00 (usually holiday/vacation)
            while (startDate.Date != endDate.Date)
            {
                startDate = startDate.AddHours(1);
                endDate = endDate.AddHours(1);
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