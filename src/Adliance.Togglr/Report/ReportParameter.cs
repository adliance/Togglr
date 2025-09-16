using CommandLine;

namespace Adliance.Togglr.Report;

[Verb("generate-report", true, HelpText = "Generate a report with specified configuration")]

public class ReportParameter
{
    [Option('c', "configuration", Required = false, Default = "./", HelpText = "Path to configuration folder which holds \"configuration.json\" and eventually \"entries.json\"")] public string ConfigurationPath { get; set; } = "";
    [Option('o', "output-path", Required = false, Default = "./", HelpText = "Output path for report")] public string OutputPath { get; set; } = "";
}
