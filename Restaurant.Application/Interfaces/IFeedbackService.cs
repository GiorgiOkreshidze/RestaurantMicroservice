using Restaurant.Application.DTOs.Feedbacks;
using Restaurant.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Interfaces
{
    public interface IFeedbackService
    {
        public Task<FeedbacksWithMetaData> GetFeedbacksByLocationIdAsync(string id, FeedbackQueryParameters queryParams);
    }
}
