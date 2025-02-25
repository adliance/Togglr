using CommandLine;

namespace Adliance.Togglr.ConfigurationGenerator;

[Verb("generate-configuration", HelpText = "Generate a template 'configuration.json' in the current folder")]
public class ConfigurationGeneratorParameters
{
    [Option('t', "targetFileName", Required = false, Default = "configuration.json", HelpText = "Target filename of the configuration file")] public string ConfigurationFilePath { get; set; } = "";
}
