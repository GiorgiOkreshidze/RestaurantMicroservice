using Restaurant.Application.DTOs.Feedbacks;

namespace Restaurant.Application.Interfaces
{
    public interface IFeedbackService
    {
        public Task<FeedbacksWithMetaData> GetFeedbacksByLocationIdAsync(string id, FeedbackQueryParameters queryParams);
        
        Task AddFeedbackAsync(CreateFeedbackRequest feedbackRequest, string userId);
    }
}
