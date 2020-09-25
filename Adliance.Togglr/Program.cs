using System;
using System.Threading.Tasks;
using CommandLine;

namespace Adliance.Togglr
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var parserResult = Parser.Default.ParseArguments<TogglrConfiguration>(args);
            
            await parserResult.WithParsedAsync<TogglrConfiguration>(async configuration =>
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