namespace Restaurant.Application.DTOs.Feedbacks;

public class CreateFeedbackRequest
{
    public string ReservationId { get; set; }
    
    public string? CuisineComment { get; set; }
    
    public string? ServiceComment { get; set; }
    
    public string? CuisineRating { get; set; }
    
    public string? ServiceRating { get; set; }
}