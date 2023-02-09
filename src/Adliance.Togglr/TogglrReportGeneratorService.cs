using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Adliance.Togglr.Extensions;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using TogglApi.Client.Reports.Models.Response;

namespace Adliance.Togglr;

public class TogglrReportGeneratorService
{
    private readonly TogglrReportGeneratorParameter _togglrReportGeneratorParameter;

    public TogglrReportGeneratorService(TogglrReportGeneratorParameter togglrReportGeneratorParameter)
    {
        _togglrReportGeneratorParameter = togglrReportGeneratorParameter;
    }

    private Configuration Configuration { get; set; } = new();
    public static List<DetailedReportDatum> AllEntries = new();

    public async Task<ExitCode> Run()
    {
        Program.Logger.Info("Welcome to Togglr.");
        Program.Logger.Trace($"You're running Togglr v{typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}.");

        try
        {
            Configuration = JsonConvert.DeserializeObject<Configuration>(await File.ReadAllTextAsync(_togglrReportGeneratorParameter.ConfigurationFilePath)) ?? throw new Exception("Unable to deserialize configuration.");
        }
        catch (Exception ex)
        {
            Program.Logger.Fatal(ex, $"Unable to load {_togglrReportGeneratorParameter.ConfigurationFilePath}: {ex.Message}");
            return ExitCode.Error;
        }

        if (Configuration.WorkspaceId == default || string.IsNullOrWhiteSpace(Configuration.ApiToken))
        {
            Program.Logger.Fatal("API Token or Workspace ID not configured in configuration.json");
            return ExitCode.Error;
        }

        Program.Logger.Trace($"Loaded configuration with {Configuration.Users.Count} configured users.");

        var togglClient = new TogglClient();
        await togglClient.DownloadEntriesAndStoreLocally(Configuration);
        AllEntries = togglClient.LoadEntriesLocallyAndFix();

        foreach (var userPair in AllEntries.GroupByUser().OrderBy(x => x.Key))
        {
            var userConfiguration = Configuration.Users.SingleOrDefault(x => x.Name.Equals(userPair.Key, StringComparison.InvariantCultureIgnoreCase));
            if (userConfiguration == null)
            {
                Program.Logger.Warn($"No configuration found for {userPair.Key}. Ignoring this user ...");
                continue;
            }

            if (!userConfiguration.CreateReport)
            {
                Program.Logger.Warn($"No report should be created for {userPair.Key}. Ignoring this user ...");
                continue;
            }

            Program.Logger.Info($"Working on {_togglrReportGeneratorParameter.OutputPath}{userPair.Key}...");

            var sb = new StringBuilder();
            HtmlHelper.WriteHtmlBegin(sb);
            HtmlHelper.WriteDocumentTitle(sb, userPair.Key);

            userConfiguration.End = userConfiguration.End == default ? Configuration.End ?? DateTime.UtcNow.Date : userConfiguration.End;
            if (Configuration.End.HasValue && Configuration.End.Value < userConfiguration.End) userConfiguration.End = Configuration.End.Value; // if we have a user end, and a global end, use the glabal end

            var calculationService = new CalculationService(userConfiguration, userPair.Value, Configuration.HomeOfficeStart ?? DateTime.MaxValue);
            MonthStatistics.WriteEveryMonth(sb, calculationService);

            var loopDate = new DateTime(userConfiguration.End.Year, userConfiguration.End.Month, 1);
            while (loopDate >= new DateTime(userConfiguration.Begin.Year, userConfiguration.Begin.Month, 1))
            {
                DayStatistics.WriteEveryDayInMonth(Configuration, sb, loopDate, calculationService);
                loopDate = loopDate.AddMonths(-1);
            }

            HtmlHelper.WriteHtmlEnd(sb);
            await File.WriteAllTextAsync($"{_togglrReportGeneratorParameter.OutputPath}{userPair.Key}.html", sb.ToString());
        }

        Program.Logger.Info("Everything done. Goodbye.");
        return ExitCode.Ok;
    }
}