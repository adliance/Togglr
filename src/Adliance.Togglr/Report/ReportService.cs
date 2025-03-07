using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Adliance.Togglr.Extensions;
using Newtonsoft.Json;
using TogglApi.Client.Reports.Models.Response;

namespace Adliance.Togglr.Report;

public class ReportService(ReportParameter reportParameter)
{
    private Configuration Configuration { get; set; } = new();
    public static List<DetailedReportDatum> AllEntries = new();

    public async Task<ExitCode> Run()
    {
        Program.Logger.Info("Welcome to Togglr.");
        Program.Logger.Trace($"You're running Togglr v{typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}.");

        try
        {
            Configuration = JsonConvert.DeserializeObject<Configuration>(await File.ReadAllTextAsync(reportParameter.ConfigurationFilePath)) ?? throw new Exception("Unable to deserialize configuration.");
        }
        catch (Exception ex)
        {
            Program.Logger.Fatal(ex, $"Unable to load {reportParameter.ConfigurationFilePath}: {ex.Message}");
            return ExitCode.Error;
        }

        if (Configuration.WorkspaceId == 0 || string.IsNullOrWhiteSpace(Configuration.ApiToken))
        {
            Program.Logger.Fatal("API Token or Workspace ID not configured in configuration.json");
            return ExitCode.Error;
        }

        Program.Logger.Trace($"Loaded configuration with {Configuration.Users.Count} configured users.");

        var togglClient = new TogglClient();
        await togglClient.DownloadEntriesAndStoreLocally(Configuration);
        AllEntries = togglClient.LoadEntriesLocallyAndFix(Configuration);

        Program.Logger.Info("Working on the tickets ...");
        var ticketsService = new TicketReportService(reportParameter);
        await ticketsService.Run(AllEntries);

        Program.Logger.Info("Crunching the numbers ...");
        UserDataService.CalculateForAllUsers(Configuration, AllEntries);

        foreach (var userPair in AllEntries.GroupByUser().OrderBy(x => x.Key))
        {
            var userData = UserDataService.Get(userPair.Key);

            if (userData == null)
            {
                Program.Logger.Warn($"No configuration found for {userPair.Key}. Ignoring this user ...");
                continue;
            }

            if (!userData.User.CreateReport)
            {
                Program.Logger.Warn($"No report should be created for {userPair.Key}. Ignoring this user ...");
                continue;
            }

            Program.Logger.Info($"Working on {reportParameter.OutputPath}{userPair.Key}...");

            var sb = new StringBuilder();
            HtmlHelper.WriteHtmlBegin(sb);
            HtmlHelper.WriteDocumentTitle(sb, userPair.Key);
            MonthStatistics.WriteEveryMonth(sb, userData);

            var loopDate = new DateTime(userData.User.End.Year, userData.User.End.Month, 1);
            while (loopDate >= new DateTime(userData.User.Begin.Year, userData.User.Begin.Month, 1))
            {
                DayStatistics.WriteEveryDayInMonth(Configuration, sb, loopDate, userData);
                loopDate = loopDate.AddMonths(-1);
            }

            HtmlHelper.WriteHtmlEnd(sb);
            await File.WriteAllTextAsync($"{reportParameter.OutputPath}{userPair.Key}.html", sb.ToString());
        }

        Program.Logger.Info("Working on the overview ...");
        var overviewReportService = new OverviewReportService(reportParameter, Configuration);
        await overviewReportService.Run();

        Program.Logger.Info("Everything done. Goodbye.");
        return ExitCode.Ok;
    }
}
