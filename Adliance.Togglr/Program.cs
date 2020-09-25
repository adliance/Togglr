using System;
using System.Threading.Tasks;
using CommandLine;

namespace Adliance.Togglr
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var parserResult = Parser.Default.ParseArguments<TogglrReportGeneratorParameter>(args);
            
            await parserResult.WithParsedAsync<TogglrReportGeneratorParameter>(async configuration =>
            {
                var exitCode = await new TogglrReportGeneratorService(configuration).Run();
                ExitWith(exitCode);
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