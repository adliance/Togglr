using System;
using System.Globalization;
using System.Threading.Tasks;
using Adliance.Togglr.ProjectTimeReport;
using CommandLine;
using CommandLine.Text;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace Adliance.Togglr;

public class Program
{
    public static ILogger Logger = LogManager.GetCurrentClassLogger();

    public static async Task Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        SetupLogging();
        Logger = LogManager.GetCurrentClassLogger();

        var parserResult = Parser.Default.ParseArguments<ProjectTimeReportParameter, TogglrReportGeneratorParameter, TogglrConfigurationGeneratorParameters>(args);
        await parserResult.WithParsedAsync<ProjectTimeReportParameter>(async parameter =>
        {
            try
            {
                await new ProjectTimeReportService().Run(parameter);
                Exit(0);
            }
            catch (Exception ex)
            {
                Exit(ex);
            }
        });

        await parserResult.WithParsedAsync<TogglrReportGeneratorParameter>(async configuration =>
        {
            var exitCode = await new TogglrReportGeneratorService(configuration).Run();
            Exit(exitCode);
        });

        await parserResult.WithParsedAsync<TogglrConfigurationGeneratorParameters>(async configuration =>
        {
            var exitCode = await new TogglrConfigurationGeneratorService(configuration).Run();
            Exit(exitCode);
        });

        parserResult.WithNotParsed(errs =>
        {
            var helpText = HelpText.AutoBuild(parserResult, h => HelpText.DefaultParsingErrorsHandler(parserResult, h), e => e);
            Console.Error.Write(helpText);
            Exit(ExitCode.Parameters);
        });
    }

    private static void Exit(ExitCode exitWith, string? message = null)
    {
        Exit((int)exitWith, message);
    }

    private static void Exit(int exitCode, string? message = null)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            Logger.Fatal(message);
        }

        Environment.Exit(exitCode);
    }

    private static void Exit(Exception ex)
    {
        Exit(-1, ex.Message);
    }

    private static void SetupLogging()
    {
        var loggingConfig = new LoggingConfiguration();
        var consoleTarget = new ColoredConsoleTarget("Colored Console") { Layout = new SimpleLayout("${time} ${message}") };
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
