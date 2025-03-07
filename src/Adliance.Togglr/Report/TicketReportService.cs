using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Adliance.Togglr.Extensions;
using TogglApi.Client.Reports.Models.Response;

namespace Adliance.Togglr.Report;

public class TicketReportService(ReportParameter reportParameter)
{
    private readonly Html _html = new(reportParameter);

    public async Task Run(List<DetailedReportDatum> allEntries)
    {
        var dataService = new TicketDataService(allEntries);
        dataService.Calculate();

        for (var year = dataService.MaxYear; year >= dataService.MinYear; year--)
        {
            RenderYear(dataService, year);
        }

        await _html.SaveToFile("Tickets");
    }

    private void RenderYear(TicketDataService dataService, int year)
    {
        var yearProjects = dataService.Projects
            .Where(x => x.Year == year)
            .OrderByDescending(x => x.Hours + x.WithoutTicketHours)
            .ToList();

        _html.Title("Tickets (" + year + ")");

        _html.WriteLine("""
                        <script>
                            function toggleProject(project) {
                                document.querySelectorAll("[data-project='" + project + "']").forEach(x => x.classList.toggle("is-hidden"));
                            }
                        </script>
                        """);

        _html.TableStart();

        _html.Write("<tr>");
        _html.Write("<th style=\"text-align:left;\">Projekt / Ticket</th>");
        _html.Write("<th style=\"text-align:left;\">Zeitraum</th>");
        _html.Write("<th style=\"text-align:right;\">Stunden</th>");
        _html.Write("<th style=\"text-align:left;\"></th>");
        _html.Write("</tr>");

        foreach (var project in yearProjects)
        {
            _html.Write("<tr>");
            _html.Write("<td style=\"text-align:left; white-space: nowrap; overflow: hidden;\">"
                        + "<a href=\"javascript:toggleProject('" + project.Name + "');\" style=\"font-weight:bold; text-decoration:none; color:unset;\">"
                        + project.Name
                        + "</a></td>");
            _html.Write("<td style=\"text-align:left; white-space: nowrap;\">"
                        + project.Start.Format(false)
                        + "-"
                        + project.End.Format(false)
                        + "</td>");
            _html.Write("<td style=\"text-align:right; white-space: nowrap;\">"
                        + project.Hours.ToString("N0", CultureInfo.InvariantCulture)
                        + "</td>");
            _html.Write("<td style=\"text-align:left; white-space: nowrap;\">"
                        + project.WithoutTicketHours.ToString("N2", CultureInfo.InvariantCulture) + " (" + project.WithoutTicketPercentage.ToString("N0", CultureInfo.CurrentCulture) + "%) ohne Ticket"
                        + "</td>");
            _html.Write("</tr>");


            foreach (var ticket in project.Tickets.OrderByDescending(x => x.Hours))
            {
                _html.Write("<tr data-project=\"" + project.Name + "\" class=\"is-hidden\">");
                _html.Write("<td style=\"text-align:left; overflow: hidden;\">" + ticket.Text + "</td>");
                _html.Write("<td style=\"text-align:left; white-space: nowrap;\">" + ticket.Start.Format(false) + "-" + ticket.End.Format(false) + "</td>");
                _html.Write("<td style=\"text-align:right; white-space: nowrap;\">" + ticket.Hours.ToString("N2", CultureInfo.InvariantCulture) + "</td>");
                _html.Write("<td style=\"text-align:left; white-space: nowrap;\">"
                            + string.Join(", ", ticket.Users.Select(x => x.ShortName + " (" + x.Percentage(ticket.Hours).ToString("N0", CultureInfo.CurrentCulture) + "%)"))
                            + "</td>");
                _html.Write("</tr>");
            }
        }

        _html.TableEnd();

        _html.TableStart("Person", "", "Insgesamt (h)", "Ingesamt verrechenbar (h)", "Davon ohne Ticket (h)");
        foreach (var u in dataService.UnbookedTickets.Where(x => x.Year == year).OrderBy(x => x.Name))
        {
            _html.TableRow(
                u.Name,
                "",
                (u.BillableProjectHours + u.UnbillableProjectHours + u.AdditionalHours).ToString("N2", CultureInfo.CurrentCulture),
                u.BillableProjectHours.ToString("N2", CultureInfo.CurrentCulture),
                u.UnbookedButBillableProjectHours.ToString("N2", CultureInfo.CurrentCulture) + " (" + u.UnbookedPercentage.ToString("N0", CultureInfo.CurrentCulture) + "%)"
            );
        }

        _html.TableEnd();
        _html.Spacer();
    }
}
