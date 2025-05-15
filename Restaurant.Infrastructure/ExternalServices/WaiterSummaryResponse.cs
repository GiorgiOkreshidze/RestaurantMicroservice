namespace Restaurant.Infrastructure.ExternalServices;

public class WaiterSummaryResponse
{
    public string Location { get; set; }
    
    public string StartDate { get; set; }
    
    public string EndDate { get; set; }
    
    public string WaiterName { get; set; }
    
    public string WaiterEmail { get; set; }
    
    public double CurrentHours { get; set; }
    
    public double PreviousHours { get; set; }
    
    public double DeltaHours { get; set; }
    
    public double CurrentAverageServiceFeedback { get; set; }
    
    public double MinimumServiceFeedback { get; set; }
    
    public decimal CurrentRevenue { get; set; }
    
    public decimal PreviousAverageServiceFeedback { get; set; }
    
    public decimal DeltaAverageServiceFeedback { get; set; }
}