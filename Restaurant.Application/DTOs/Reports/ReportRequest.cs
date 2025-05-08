using System.ComponentModel.DataAnnotations;

namespace Restaurant.Application.DTOs.Reports
{
    public class ReportRequest
    {
        public required string StartDate { get; set; }
        
        public required string EndDate { get; set; }
        
        public string? LocationId { get; set; }
    }
}
