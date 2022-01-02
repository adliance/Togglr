using System;
using CommandLine;

namespace Adliance.Togglr.ProjectTimeReport;

[Verb("project-time", HelpText = "Generates a detailed report for a specific project and time-range.")]
public class ProjectTimeReportParameter
{
    [Option('w', "workspace", Required = true, HelpText = "The Toggl workspace.")] public long Workspace { get; set; }
    [Option('k', "apikey", Required = true, HelpText = "The Toggl API key.")] public string ApiKey { get; set; } = "";
    [Option('p', "project", Required = true, HelpText = "The Toggl project ID.")] public int Project { get; set; }
    [Option('f', "from", Required = false, HelpText = "The lower boundary of the time range. Defaults to first day of last month.")] public DateTime? FromDate { get; set; }
    [Option('t', "to", Required = false, HelpText = "The upper boundary of the time range. Defaults to last day of last month.")] public DateTime? ToDate { get; set; }
    [Option('o', "output", Required = false, Default = "./project-time-report.md", HelpText = "The output path.")] public string TargetPath { get; set; } = "";

    [Option('m', "max-poolsize", Required = false, HelpText = "(Optional) The maximum hours pool size for the specified project.")] public double? MaxPoolSize { get; set; }
    [Option('c', "current-poolsize", Required = false, HelpText = "(Optional) The currently remaining hours pool size for the specified project. Leave empty to calculate automatically.")] public double? CurrentPoolSize { get; set; }


    public DateTime From
    {
        get
        {
            if (FromDate.HasValue)
            {
                return FromDate.Value;
            }

            var from = DateTime.UtcNow.AddMonths(-1);
            return new DateTime(from.Year, from.Month, 1);
        }
    }

    public DateTime To
    {
        get
        {
            if (ToDate.HasValue)
            {
                return ToDate.Value;
            }

            var to = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            return to.AddDays(-1);
        }
    }
}