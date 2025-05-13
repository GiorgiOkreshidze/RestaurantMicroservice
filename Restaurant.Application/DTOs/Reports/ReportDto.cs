﻿namespace Restaurant.Application.DTOs.Reports
{
    public class ReportDto
    {
        public required string Location { get; set; }
        
        public required string LocationId { get; set; }

        public required string Date { get; set; }

        public required string Waiter { get; set; }

        public required string WaiterEmail { get; set; }

        public required decimal HoursWorked { get; set; }

        public required string OrderId { get; set; }
        
        public required decimal OrderRevenue { get; set; }

        public required double AverageServiceFeedback { get; set; }
        
        public required double AverageCuisineFeedback { get; set; }
        
        public required int MinimumCuisineFeedback { get; set; }

        public required int MinimumServiceFeedback { get; set; }
    }
}
