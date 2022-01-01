using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Adliance.Togglr;

public class TogglrConfigurationGeneratorService
{
    private readonly TogglrConfigurationGeneratorParameters _configuration;

    public TogglrConfigurationGeneratorService(TogglrConfigurationGeneratorParameters configuration)
    {
        _configuration = configuration;
    }

    public async Task<ExitCode> Run()
    {
        var exampleConfiguration = new Configuration
        {
            WorkspaceId = 0,
            ApiToken = "",
  
            ProjectNameVacation = "Urlaub",
            ProjectNameSpecialVacation = "Sonderurlaub",
            ProjectNameHoliday = "Feiertag",
            ProjectNamePersonalHoliday = "Persönlicher Feiertag",
            ProjectNameSick = "Krankenstand",
            ProjectNameDoctor = "Arztbesuch",
            ProjectNameLegacyVacationHolidaySick = "ALT= Feiertag, Krankenstand, Urlaub",
  
            Users = new []
            {
                new UserConfiguration 
                {
                    Name = "Teammember 1",
                    Begin =  new DateTime(2018, 04, 03),
                    HoursPerDay = 7.7,
                    DifferentWorkTimes = new [] {
                        new ExpectedWorkTimeConfiguration {
                            Begin = new DateTime(2019, 06, 01),
                            End = new DateTime(2019, 06, 30),
                            HoursPerDay = 0.233
                        },
                        new ExpectedWorkTimeConfiguration
                        {
                            Begin = new DateTime(2020, 06, 01 ),
                            End = new DateTime(2022, 06, 30),
                            HoursPerDay = 4,
                            ResetHolidays = 0,
                            ResetOvertime = 0
                        }
                    },
                },
                new UserConfiguration 
                {
                    Name = "Teammember 2",
                    Begin = new DateTime(2019, 11, 03),
                    HoursPerDay = 7.7
                },
                new UserConfiguration 
                {
                    Name = "Teammember 3",
                    Begin = new DateTime(2020,01,01),
                    End = new DateTime(2020, 06, 30),
                    HoursPerDay= 4
                }
            }
        };

        var exampleConfigurationJsonString = JsonConvert.SerializeObject(exampleConfiguration, Formatting.Indented);
        await File.WriteAllTextAsync(_configuration.ConfigurationFilePath, exampleConfigurationJsonString);
            
        return ExitCode.Ok;
    }
}