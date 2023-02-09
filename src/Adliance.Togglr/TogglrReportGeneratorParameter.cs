﻿using CommandLine;

namespace Adliance.Togglr;

[Verb("generate-report", true, HelpText = "Generate a report with specified configuration")]

public class TogglrReportGeneratorParameter
{
    [Option('c', "configuration", Required = false, Default = "configuration.json", HelpText = "Path to configuration file")] public string ConfigurationFilePath { get; set; } = "";
    [Option('o', "output-path", Required = false, Default = "./", HelpText = "Output path for report")] public string OutputPath { get; set; } = "";
}