using Restaurant.Application.DTOs.Feedbacks;
using Restaurant.Domain.Entities;

namespace Restaurant.Application.Interfaces
{
    public interface IFeedbackService
    {
        public Task<FeedbacksWithMetaData> GetFeedbacksByLocationIdAsync(string id, FeedbackQueryParameters queryParams);
        
        Task AddFeedbackAsync(CreateFeedbackRequest feedbackRequest, string userId);

        public Task AddAnonymousFeedbackAsync(CreateFeedbackRequest feedbackRequest, Reservation reservation);
    }
}
