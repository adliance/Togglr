using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using TogglApi.Client.Reports.Models.Response;
using Togglr.Extensions;

namespace Togglr
{
    public class Program
    {
        public static List<DetailedReportDatum> AllEntries = new List<DetailedReportDatum>();

        public static async Task Main()
        {
            SetupLogging();
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
            var logger = LogManager.GetCurrentClassLogger();

            logger.Info("Welcome to Adliance.Togglr.");
            logger.Trace($"You're running Adliance.Togglr v{typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}.");

            try
            {
                Configuration = JsonConvert.DeserializeObject<Configuration>(await File.ReadAllTextAsync("configuration.json"));
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, $"Unable to load configuration.json: {ex.Message}");
                Environment.Exit(-1);
            }

            if (Configuration.WorkspaceId == default || string.IsNullOrWhiteSpace(Configuration.ApiToken))
            {
                logger.Fatal("API Token or Workspace ID not configured in configuration.json");
                Environment.Exit(-1);
            }

            logger.Trace($"Loaded configuration with {Configuration.Users.Count} configured users.");

            var togglClient = new TogglClient();
            await togglClient.DownloadEntriesAndStoreLocally(Configuration);
            AllEntries = togglClient.LoadEntriesLocallyAndFix();

            foreach (var userPair in AllEntries.GroupByUser().OrderBy(x => x.Key))
            {
                logger.Info($"Working on {userPair.Key}...");

                var sb = new StringBuilder();
                HtmlHelper.WriteHtmlBegin(sb);
                HtmlHelper.WriteDocumentTitle(sb, userPair.Key);

                var userConfiguration = Configuration.Users.SingleOrDefault(x => x.Name.Equals(userPair.Key, StringComparison.InvariantCultureIgnoreCase));
                if (userConfiguration == null)
                {
                    logger.Warn($"No configuration found for {userPair.Key}. Ignoring this user ...");
                    continue;
                }

                userConfiguration.End = userConfiguration.End == default ? DateTime.UtcNow.Date.AddDays(1) : userConfiguration.End;

                var calculationService = new CalculationService(userConfiguration, userPair.Value);
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
        }

        private static Configuration Configuration { get; set; } = new Configuration();

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