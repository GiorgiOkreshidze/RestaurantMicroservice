namespace Restaurant.Infrastructure.ExternalServices;

    public class ReportResponse
    {
        public IEnumerable<LocationSummaryResponse> Sales { get; set; } = new List<LocationSummaryResponse>();
        
        public IEnumerable<ReportResponse> Performance { get; set; } = new List<ReportResponse>();
    }