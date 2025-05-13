using Restaurant.Application.DTOs.Feedbacks;
using Restaurant.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Infrastructure.Interfaces
{
    public interface IFeedbackRepository
    {
        public Task<(List<Feedback>, string?)> GetFeedbacksAsync(string id, FeedbackQueryParameters queryParams);
        
        public Task UpsertFeedbackByReservationAndTypeAsync(Feedback feedback);

        public Task<IEnumerable<Feedback>> GetServiceFeedbacks(string reservationId);
        
        public Task<IEnumerable<Feedback>> GetCuisineFeedbacks(string reservationId);
    }
}
