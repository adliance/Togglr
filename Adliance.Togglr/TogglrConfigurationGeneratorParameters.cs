using CommandLine;

namespace Adliance.Togglr
{
    [Verb("configuration-generator", HelpText = "Generate a template 'configuration.json' in the current folder")]
    public class TogglrConfigurationGeneratorParameters 
    {
        [Option('t', "targetFileName", Required = false, Default = "configuration.json", HelpText = "Target filename of the configuration file")] public string ConfigurationFilePath { get; set; } = "";
    }
}