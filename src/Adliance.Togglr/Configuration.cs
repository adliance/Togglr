using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Adliance.Togglr
{
    public class Configuration
    {
        [JsonProperty("workspace_id")] public int WorkspaceId { get; set; }
        [JsonProperty("api_token")] public string ApiToken { get; set; } = "";
        
        [JsonProperty("project_vacation")] public string ProjectNameVacation  { get; set; } = "Urlaub";
        [JsonProperty("project_special_vacation")] public string ProjectNameSpecialVacation  { get; set; } = "Sonderurlaub";
        [JsonProperty("project_holiday")] public string ProjectNameHoliday  { get; set; } = "Feiertag";
        [JsonProperty("project_personal_holiday")] public string ProjectNamePersonalHoliday  { get; set; } = "Persönlicher Feiertag";
        [JsonProperty("project_sick")] public string ProjectNameSick  { get; set; } = "Krankenstand";
        [JsonProperty("project_doctor")] public string ProjectNameDoctor  { get; set; } = "Arztbesuch";
        [JsonProperty("project_legacy_vacation_holiday_sick")] public string ProjectNameLegacyVacationHolidaySick  { get; set; } = "ALT: Feiertag, Krankenstand, Urlaub";
        
        
        [JsonProperty("end")] public DateTime? End { get; set; }
        [JsonProperty("users")] public IList<UserConfiguration> Users { get; set; } = new List<UserConfiguration>();
    }

    public class UserConfiguration
    {
        [JsonProperty("name")] public string Name { get; set; } = "";
        [JsonProperty("different_work_times")] public IList<ExpectedWorkTimeConfiguration> DifferentWorkTimes { get; set; } = new List<ExpectedWorkTimeConfiguration>();

        [JsonProperty("begin")] public DateTime Begin { get; set; }
        [JsonProperty("end")] public DateTime End { get; set; }
        
        [JsonProperty("hours_per_day")] public double HoursPerDay { get; set; }
        [JsonProperty("reset_overtime")] public double ResetOvertime { get; set; }
        [JsonProperty("reset_holidays")] public double ResetHolidays { get; set; }

        [JsonProperty("ignore_break_warnings")] public bool IgnoreBreakWarnings { get; set; } = false;
    }

    public class ExpectedWorkTimeConfiguration
    {
        [JsonProperty("begin")] public DateTime Begin { get; set; }
        [JsonProperty("end")] public DateTime End { get; set; }
        [JsonProperty("hours_per_day")] public double HoursPerDay { get; set; }
        [JsonProperty("reset_overtime")] public double? ResetOvertime { get; set; }
        [JsonProperty("reset_holidays")] public double? ResetHolidays { get; set; }
    }
}