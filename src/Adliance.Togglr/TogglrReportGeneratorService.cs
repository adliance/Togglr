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

namespace Adliance.Togglr
{
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
            SetupLogging();
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
            var logger = LogManager.GetCurrentClassLogger();

            logger.Info("Welcome to Togglr.");
            logger.Trace($"You're running Togglr v{typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}.");

            try
            {
                Configuration = JsonConvert.DeserializeObject<Configuration>(await File.ReadAllTextAsync(_togglrReportGeneratorParameter.ConfigurationFilePath));
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, $"Unable to load {_togglrReportGeneratorParameter.ConfigurationFilePath}: {ex.Message}");
                return ExitCode.Error;
            }

            if (Configuration.WorkspaceId == default || string.IsNullOrWhiteSpace(Configuration.ApiToken))
            {
                logger.Fatal("API Token or Workspace ID not configured in configuration.json");
                return ExitCode.Error;
            }

            logger.Trace($"Loaded configuration with {Configuration.Users.Count} configured users.");

            var togglClient = new TogglClient();
            await togglClient.DownloadEntriesAndStoreLocally(Configuration);
            AllEntries = togglClient.LoadEntriesLocallyAndFix();

            foreach (var userPair in AllEntries.GroupByUser().OrderBy(x => x.Key))
            {
                var userConfiguration = Configuration.Users.SingleOrDefault(x => x.Name.Equals(userPair.Key, StringComparison.InvariantCultureIgnoreCase));
                if (userConfiguration == null)
                {
                    logger.Warn($"No configuration found for {userPair.Key}. Ignoring this user ...");
                    continue;
                }

                if (!userConfiguration.CreateReport)
                {
                    logger.Warn($"No report should be created for {userPair.Key}. Ignoring this user ...");
                    continue;
                }
                
                logger.Info($"Working on {userPair.Key}...");

                var sb = new StringBuilder();
                HtmlHelper.WriteHtmlBegin(sb);
                HtmlHelper.WriteDocumentTitle(sb, userPair.Key);


                userConfiguration.End = userConfiguration.End == default ? Configuration.End ?? DateTime.UtcNow.Date : userConfiguration.End;

                var calculationService = new CalculationService(userConfiguration, userPair.Value, Configuration.HomeOfficeStart ?? DateTime.MaxValue);
                MonthStatistics.WriteEveryMonth(sb, calculationService);

                var loopDate = new DateTime(userConfiguration.End.Year, userConfiguration.End.Month, 1);
                while (loopDate >= new DateTime(userConfiguration.Begin.Year, userConfiguration.Begin.Month, 1))
                {
                    DayStatistics.WriteEveryDayInMonth(Configuration, sb, loopDate, calculationService);
                    loopDate = loopDate.AddMonths(-1);
                }

                HtmlHelper.WriteHtmlEnd(sb);
                await File.WriteAllTextAsync($"{userPair.Key}.html", sb.ToString());
            }

            logger.Info("Everything done. Goodbye.");
            return ExitCode.Ok;
        }

        private static void SetupLogging()
        {
            var loggingConfig = new LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget("Colored Console") {Layout = new SimpleLayout("${time} ${message}")};
            loggingConfig.AddTarget(consoleTarget);
            loggingConfig.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, consoleTarget));

            var fileTarget = new FileTarget("File")
            {
                Layout = new SimpleLayout("${longdate} ${uppercase:${level}} ${message}"),
                FileName = "log.txt",
                ArchiveEvery = FileArchivePeriod.Day,
                ArchiveNumbering = ArchiveNumberingMode.Date,
                MaxArchiveFiles = 10
            };
            loggingConfig.AddTarget(fileTarget);
            loggingConfig.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, fileTarget));

            LogManager.Configuration = loggingConfig;
        }
    }
}