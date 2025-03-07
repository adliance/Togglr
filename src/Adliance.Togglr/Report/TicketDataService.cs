using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Adliance.Togglr.Extensions;
using TogglApi.Client.Reports.Models.Response;

namespace Adliance.Togglr.Report;

public class TicketDataService(List<DetailedReportDatum> allEntries)
{
    public List<TicketProjectData> Projects { get; } = new();
    public List<UnbookedTicketData> UnbookedTickets { get; } = new();
    public int MinYear => Projects.SelectMany(x => x.Tickets).Min(x => x.End.Year);
    public int MaxYear => Projects.SelectMany(x => x.Tickets).Max(x => x.End.Year);

    public void Calculate()
    {
        Projects.Clear();
        UnbookedTickets.Clear();

        CalculateTickets();
        CalculateUnbookedTickets();
    }

    private void CalculateTickets()
    {
        foreach (var entry in allEntries)
        {
            if (entry.IsSpecial()) continue;
            if (!entry.IsBillable) continue;

            var hours = (entry.End - entry.Start).TotalHours;

            var project = Projects.SingleOrDefault(x => x.Name == entry.Project && x.Year == entry.End.Year);
            if (project == null)
            {
                project = new TicketProjectData
                {
                    Name = entry.Project,
                    Year = entry.End.Year
                };
                Projects.Add(project);
            }

            if (project.Start > entry.Start) project.Start = entry.Start;
            if (project.End < entry.End) project.End = entry.End;

            var identifier = ParseTicketIdentifier(entry.Description);
            if (string.IsNullOrWhiteSpace(identifier))
            {
                project.WithoutTicketHours += hours;
                continue;
            }

            project.Hours += hours;
            var ticket = project.Tickets.SingleOrDefault(x => x.Identifier == identifier);
            if (ticket == null)
            {
                ticket = new TicketProjectData.TicketData
                {
                    Identifier = identifier,
                    Text = entry.Description
                };
                project.Tickets.Add(ticket);
            }

            if (ticket.Start > entry.Start) ticket.Start = entry.Start;
            if (ticket.End < entry.End) ticket.End = entry.End;
            if (!ticket.Text.Contains(entry.Description, StringComparison.OrdinalIgnoreCase)) ticket.Text = (ticket.Text + ", " + entry.Description).Trim();

            var user = ticket.Users.SingleOrDefault(x => x.Name == entry.User);
            if (user == null)
            {
                user = new TicketProjectData.TicketData.TicketUser
                {
                    Name = entry.User
                };
                ticket.Users.Add(user);
            }

            user.Hours += hours;
        }
    }

    private void CalculateUnbookedTickets()
    {
        foreach (var entry in allEntries)
        {
            var identifier = ParseTicketIdentifier(entry.Description);
            var hours = (entry.End - entry.Start).TotalHours;

            var unbooked = UnbookedTickets.SingleOrDefault(x => x.Name == entry.User && x.Year == entry.End.Year);
            if (unbooked == null)
            {
                unbooked = new UnbookedTicketData
                {
                    Name = entry.User,
                    Year = entry.End.Year
                };
                UnbookedTickets.Add(unbooked);
            }

            if (entry.IsSpecial()) unbooked.AdditionalHours += hours;
            else if (entry.IsBillable()) unbooked.BillableProjectHours += hours;
            else unbooked.UnbillableProjectHours += hours;

            if (string.IsNullOrWhiteSpace(identifier))
            {
                if (entry.IsBillable() && !entry.IsSpecial()) unbooked.UnbookedButBillableProjectHours += hours;
            }
        }
    }

    public static string? ParseTicketIdentifier(string text)
    {
        var match = Regex.Match(text, @"(\#\d*)");
        if (match.Success) return match.Groups[1].Value;

        match = Regex.Match(text, @"(GPD\-\d*)");
        if (match.Success) return match.Groups[1].Value;

        match = Regex.Match(text, @"(AGOS\-\d*)");
        if (match.Success) return match.Groups[1].Value;

        match = Regex.Match(text, @"(TT\-\d*)");
        if (match.Success) return match.Groups[1].Value;

        match = Regex.Match(text, @"(\d{3,5})");
        if (match.Success) return match.Groups[1].Value;

        return null;
    }
}
