namespace Restaurant.Application.DTOs.Feedbacks;

public class FeedbackDto
{
    public required string Id { get; set; }
    public required int Rate { get; set; }
    public required string Comment { get; set; }
    public required string UserName { get; set; }
    public required string UserAvatarUrl { get; set; }
    public required string Date { get; set; }
    public required string Type { get; set; }
    public required string LocationId { get; set; }
    public required string ReservationId { get; set; }
}