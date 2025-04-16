using Restaurant.Application.DTOs.Feedbacks;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Entities.Enums;
using Restaurant.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Services
{
    public class FeedbackService(IFeedbackRepository feedbackRepository) : IFeedbackService
    {
        public async Task<FeedbacksWithMetaData> GetFeedbacksByLocationIdAsync(string id, FeedbackQueryParameters queryParams)
        {
            if (!string.IsNullOrEmpty(queryParams.Type))
            {
                queryParams.EnumType = queryParams.Type.ToFeedbackType();
            }

            var (feedbacks, token) = await GetPaginatedFeedbacks(id, queryParams);
            var Response = BuildResponseBody(feedbacks, queryParams, token);

            return Response;
        }

        private async Task<(List<Feedback>, string?)> GetPaginatedFeedbacks(
            string id,
            FeedbackQueryParameters queryParams)
        {
            // Page 1 is simply a direct query
            if (queryParams.Page <= 1)
            {
                // Clear any token if we're requesting page 1 explicitly
                queryParams.NextPageToken = null;
                return await feedbackRepository.GetFeedbacksAsync(id, queryParams);
            }

            // For pages > 1, we need to follow the tokens
            string? currentToken = null;
            List<Feedback> currentPageFeedbacks = [];

            // Start with page 1
            var (firstPageFeedbacks, nextToken) = await feedbackRepository.GetFeedbacksAsync(id, queryParams);

            // If there's no first page or no next token, we can't get to page 2+
            if (firstPageFeedbacks.Count == 0 || nextToken == null)
            {
                return ([], null);
            }

            currentToken = nextToken;

            // Follow the tokens to get to the requested page
            for (int i = 1; i < queryParams.Page; i++)
            {
                // Update the token for the next query
                queryParams.NextPageToken = currentToken;

                // Get the next page
                var (pageFeedbacks, pageToken) = await feedbackRepository.GetFeedbacksAsync(id, queryParams);

                // If we got no results or no further token, we've reached the end
                if (pageFeedbacks.Count == 0)
                {
                    return ([], null);
                }

                currentPageFeedbacks = pageFeedbacks;
                currentToken = pageToken;

                // If there's no next token, we've reached the last page
                if (currentToken == null)
                {
                    break;
                }
            }

            return (currentPageFeedbacks, currentToken);
        }

        private FeedbacksWithMetaData BuildResponseBody(
            List<Feedback> feedbacks, 
            FeedbackQueryParameters queryParams,
            string? token
            )
        {
            return new FeedbacksWithMetaData
            {
                Content = feedbacks.Select(f => new FeedbackDto
                {
                    Id = f.Id,
                    Rate = f.Rate,
                    Comment = f.Comment,
                    UserName = f.UserName,
                    UserAvartarUrl = f.UserAvatarUrl,
                    Date = f.Date,
                    Type = f.Type,
                    LocationId = f.LocationId
                }).ToList(),
                Sort = new FeedbacksSortMetaData
                {
                    Direction = queryParams.SortDirection.ToUpper(),
                    NullHandling = "string",
                    Ascending = queryParams.SortDirection.ToLower() == "asc",
                    Property = queryParams.SortProperty,
                    IgnoreCase = true
                },
                Token = token 
            };
        }
    }
}
