using Restaurant.Application.DTOs.Feedbacks;

namespace Restaurant.Application.Interfaces;

public interface IAnonymousFeedbackService
{
    Task<string> ValidateTokenAndGetReservationId(string token);
    
    Task SubmitAnonymousFeedback(CreateFeedbackRequest request);
}