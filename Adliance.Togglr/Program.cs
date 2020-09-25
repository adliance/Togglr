using System;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace Adliance.Togglr
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var parserResult = Parser.Default.ParseArguments<
                TogglrReportGeneratorParameter,
                TogglrConfigurationGeneratorParameters>(args);
            
            await parserResult.WithParsedAsync<TogglrReportGeneratorParameter>(async configuration =>
            {    
                var exitCode = await new TogglrReportGeneratorService(configuration).Run();
                ExitWith(exitCode);
            });
            
            await parserResult.WithParsedAsync<TogglrConfigurationGeneratorParameters>(async configuration =>
            {
                var exitCode = await new TogglrConfigurationGeneratorService(configuration).Run();
                ExitWith(exitCode);
            });
            
            parserResult.WithNotParsed(errs =>
            {
                var helpText = HelpText.AutoBuild(parserResult, h => HelpText.DefaultParsingErrorsHandler(parserResult, h), e => e);
                Console.Error.Write(helpText);
                ExitWith(ExitCode.Parameters);
            });
        }
        
        private static void ExitWith(ExitCode exitWith, string? message = null)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine(message);
            }

            Environment.Exit((int) exitWith);
        }
    }
}