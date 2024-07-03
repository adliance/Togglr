using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Adliance.Togglr.Exceptions;
using Adliance.Togglr.Extensions;
using TogglApi.Client.Exceptions;
using TogglApi.Client.Reports;
using TogglApi.Client.Reports.Models;
using TogglApi.Client.Reports.Models.Request;
using TogglApi.Client.Reports.Models.Response;

namespace Adliance.Togglr.ProjectTimeReport;

public class ProjectTimeReportService
{
    public async Task Run(ProjectTimeReportParameter configuration)
    {
        Program.Logger.Info("Building project time report ...");
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        var entries = await LoadEntries(configuration);
        await RenderToMarkdown(entries, configuration);
        Program.Logger.Info("Everything done. Goodbye.");
    }

    private static async Task RenderToMarkdown(IList<Entry> entries, ProjectTimeReportParameter configuration)
    {
        var file = new FileInfo(configuration.TargetPath);
        var directory = new DirectoryInfo(Path.GetDirectoryName(file.FullName)!);
        Program.Logger.Info($"Writing result to {file.FullName} ...");

        if (!directory.Exists)
        {
            Program.Logger.Warn($"Creating directory {directory.FullName} ...");
        }

        var markdown = new StringBuilder();
        markdown.AppendLine(CultureInfo.CurrentCulture,
            $"Arbeitszeiten für Projekt **{entries.First().Project}** von **{configuration.From:dd. MMMM yyyy}** bis **{configuration.To:dd. MMMM yyyy}**:");
        markdown.AppendLine();
        markdown.AppendLine("| Tag | Person | Task | Stunden |");
        markdown.AppendLine("|-|-|-|-:|");

        foreach (var e in entries)
        {
            markdown.AppendLine(CultureInfo.CurrentCulture, $"| {e.Date:dd.MM.yyyy} | {e.User.Replace(" ", "&nbsp;")} | {e.Description} | {e.Hours:N2} |");
        }

        var totalHours = entries.Sum(x => x.Hours);
        markdown.AppendLine(CultureInfo.CurrentCulture, $"| | | **Summe** | **{totalHours:N2}** |");

        if (configuration.MaxPoolSize.HasValue)
        {
            double? totalHoursInProject;
            if (configuration.CurrentPoolSize.HasValue)
            {
                totalHoursInProject = configuration.CurrentPoolSize.Value + totalHours;
            }
            else
            {
                var allEntries = await LoadEntries(configuration.Workspace, configuration.ApiKey, configuration.From.AddYears(-5), configuration.To, configuration.Project);
                totalHoursInProject = allEntries.Sum(x => x.Hours);
            }

            markdown.AppendLine(CultureInfo.CurrentCulture, $"| | | <br />Gesamtumfang des Stundenpools | <br />{configuration.MaxPoolSize:N2} |");
            markdown.AppendLine(CultureInfo.CurrentCulture, $"| | | **Verbleibend im Stundenpool** | **{configuration.MaxPoolSize - totalHoursInProject.Value:N2}** |");
        }

        await File.WriteAllTextAsync(file.FullName, markdown.ToString());
    }

    private static async Task<IList<Entry>> LoadEntries(ProjectTimeReportParameter configuration)
    {
        try
        {
            return await LoadEntries(configuration.Workspace, configuration.ApiKey, configuration.From, configuration.To, configuration.Project);
        }
        catch (TogglApiException ex)
        {
            throw new TogglException(ex);
        }
    }

    private static async Task<IList<Entry>> LoadEntries(long workspace, string apiKey, DateTime from, DateTime to, long projectId)
    {
        Program.Logger.Info($"Downloading time entries for project \"{177670915}\" between {from:yyyy-MM-dd} and {to:yyyy-MM-dd} ...");
        var client = new TogglReportClient(new HttpClient(), Program.Logger);

        var currentFrom = from;
        var currentTo = to;
        if (currentFrom.Year != currentTo.Year)
        {
            currentTo = new DateTime(currentFrom.Year, 12, 31);
        }

        var entries = new List<DetailedReportDatum>();
        do
        {
            var apiResults = (await client.GetDetailedReport(new DetailedReportConfig
            (
                userAgent: "Adliance.Togglr by Adliance GmbH",
                workspaceId: workspace,
                since: currentFrom,
                until: currentTo,
                billableOptions: BillableOptions.Both,
                projectIds: new List<int>
                {
                    (int)projectId
                }
            ), apiToken: apiKey)).Data.OrderBy(x => x.Start).ToList();

            entries.AddRange(apiResults);
            currentFrom = currentTo.AddDays(1);
            currentTo = currentTo.AddYears(1);
            if (currentTo.Date > to.Date) currentTo = to.Date;
        } while (currentFrom < to);

        if (!entries.Any()) throw new NoEntriesException(projectId.ToString(CultureInfo.CurrentCulture));

        Program.Logger.Info($"\t{entries.Count} entries found. Combining similar entries and applying CET timezone ...");
        var result = new List<Entry>();
        foreach (var e in entries)
        {
            var entry = result.SingleOrDefault(x => x.Date == e.Start.UtcToCet().Date && x.Description.Trim().Equals(e.Description.Trim(), StringComparison.OrdinalIgnoreCase) && e.User == x.User);
            if (entry == null)
            {
                entry = new Entry
                {
                    Date = e.Start.UtcToCet().Date,
                    Description = e.Description.Trim(),
                    Hours = 0,
                    User = e.User.Trim(),
                    Project = e.Project.Trim()
                };
                result.Add(entry);
            }

            entry.Hours += (e.End - e.Start).TotalHours;
        }

        foreach (var e in result)
        {
            e.Description = e.Description.Replace("homeoffice", "", StringComparison.OrdinalIgnoreCase);
            e.Description = e.Description.Replace("home office", "", StringComparison.OrdinalIgnoreCase);
            e.Hours = RoundToQuarterHours(e.Hours);
        }

        Program.Logger.Info($"\t{result.Count} distinct entries remaining.");
        return result.OrderBy(x => x.Date).ThenBy(x => x.User).ToList();
    }

    public static double RoundToQuarterHours(double d)
    {
        // round to the next quarter hour
        var frac = d % 1;
        if (frac < 0.1) return Math.Truncate(d);
        if (frac <= .25) return Math.Truncate(d) + 0.25;
        if (frac <= .5) return Math.Truncate(d) + 0.5;
        if (frac <= .75) return Math.Truncate(d) + 0.75;
        return Math.Truncate(d) + 1;
    }

    public class Entry
    {
        public DateTime Date { get; init; }
        public string User { get; init; } = "";
        public string Description { get; set; } = "";
        public double Hours { get; set; }
        public string Project { get; set; } = "";
    }
}
