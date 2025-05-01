using System.ComponentModel.DataAnnotations;

namespace Restaurant.Application.DTOs.Reports
{
    public class ReportRequest
    {
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        public string? LocationId { get; set; }
    }
}
