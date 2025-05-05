namespace Restaurant.Infrastructure.ExternalServices;

    public class ReportResponse
    {
        public string Location { get; set; } = string.Empty;
        
        public string StartDate { get; set; } = string.Empty;
        
        public string EndDate { get; set; } = string.Empty;
      
        public string WaiterName { get; set; } = string.Empty;
        
        public string WaiterEmail { get; set; } = string.Empty;
        
        public double CurrentHours { get; set; }
        
        public double PreviousHours { get; set; }
        
        public double DeltaHours { get; set; }
        
        public double CurrentAverageServiceFeedback { get; set; }
        
        public double MinimumServiceFeedback { get; set; }
        
        public double PreviousAverageServiceFeedback { get; set; }

        public double DeltaAverageServiceFeedback { get; set; }
    }