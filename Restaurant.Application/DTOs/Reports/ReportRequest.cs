using System.ComponentModel.DataAnnotations;

namespace Restaurant.Application.DTOs.Reports
{
    public class ReportRequest
    {
        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }
        
        public string? LocationId { get; set; }
    }
}
