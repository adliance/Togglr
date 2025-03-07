using System;
using System.Collections.Generic;
using System.Linq;

namespace Adliance.Togglr.Report;

public class TicketProjectData
{
    public required string Name { get; set; }
    public required int Year { get; set; }
    public List<TicketData> Tickets { get; set; } = new();
    public DateTime Start { get; set; } = DateTime.MaxValue;
    public DateTime End { get; set; } = DateTime.MinValue;
    public double Hours { get; set; }
    public double WithoutTicketHours { get; set; }
    public int WithoutTicketPercentage => (int)Math.Round(100d / (WithoutTicketHours + Hours) * WithoutTicketHours);

    public class TicketData
    {
        public required string Identifier { get; set; }
        public required string Text { get; set; }
        public List<TicketUser> Users { get; set; } = new();
        public DateTime Start { get; set; } = DateTime.MaxValue;
        public DateTime End { get; set; } = DateTime.MinValue;
        public double Hours => Users.Sum(x => x.Hours);

        public class TicketUser
        {
            public required string Name { get; set; }
            public double Hours { get; set; }

            public int Percentage(double total)
            {
                return (int)Math.Round(100d / total * Hours);
            }

            public string ShortName => Name.Split(' ')[0];
        }
    }
}

public class UnbookedTicketData
{
    public required string Name { get; set; }
    public required int Year { get; set; }
    public double BillableProjectHours { get; set; }
    public double UnbillableProjectHours { get; set; }
    public double UnbookedButBillableProjectHours { get; set; }
    public double AdditionalHours { get; set; }
    public int UnbookedPercentage => (int)Math.Round(100d / BillableProjectHours * UnbookedButBillableProjectHours);
}
