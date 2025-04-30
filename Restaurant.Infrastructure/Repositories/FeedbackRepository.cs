using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Restaurant.Application.DTOs.Feedbacks;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Entities.Enums;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories
{
    public class FeedbackRepository(IDynamoDBContext context) : IFeedbackRepository
    {
        public async Task<(List<Feedback>, string?)> GetFeedbacksAsync(string id, FeedbackQueryParameters queryParams)
        {
            var queryConfig = CreateQueryConfiguration(id, queryParams);
            var search = context.FromQueryAsync<Feedback>(queryConfig);

            int itemsToSkip = 0;
            if (!string.IsNullOrEmpty(queryParams.NextPageToken) &&
                int.TryParse(queryParams.NextPageToken, out int skipCount))
            {
                itemsToSkip = skipCount;
            }

            var feedbacks = new List<Feedback>();
            var searchResponse = await search.GetRemainingAsync();

            var pagedResults = searchResponse.Skip(itemsToSkip).Take(queryParams.PageSize + 1).ToList();

            string? nextPageToken = null;

            if (pagedResults.Count > queryParams.PageSize)
            {
                pagedResults.RemoveAt(queryParams.PageSize);
                nextPageToken = (itemsToSkip + queryParams.PageSize).ToString();
            }

            return (pagedResults, nextPageToken);
        }

        public async Task UpsertFeedbackByReservationAndTypeAsync(Feedback feedback)
        {
            var reservationIdTypeKey = $"{feedback.ReservationId}#{feedback.Type}";
            feedback.ReservationIdType = reservationIdTypeKey;
            feedback.LocationIdType = $"{feedback.LocationId}#{feedback.Type}";

            var config = new QueryOperationConfig
            {
                IndexName = "ReservationTypeIndex",
                Filter = new QueryFilter(),
            };

            config.Filter.AddCondition("reservationId#type", QueryOperator.Equal, reservationIdTypeKey);

            var feedbacks = await context.FromQueryAsync<Feedback>(config).GetRemainingAsync();

            var existingFeedback = feedbacks.FirstOrDefault();

            if (existingFeedback != null && !String.IsNullOrEmpty(existingFeedback.LocationId) &&
                !String.IsNullOrEmpty(existingFeedback.TypeDate))
            {
                await UpdateExistingFeedbackRateAndCommentAsync(feedback, existingFeedback);
            }
            else
            {
                feedback.TypeDate = $"{feedback.Type}#{feedback.Date}";
                await context.SaveAsync(feedback); // Insert new feedback
            }
        }

        private async Task UpdateExistingFeedbackRateAndCommentAsync(Feedback newFeedback, Feedback existingFeedback)
        {
            existingFeedback.Rate = newFeedback.Rate;
            existingFeedback.Comment = newFeedback.Comment;
            existingFeedback.TypeDate = $"{newFeedback.Type}#{newFeedback.Date}";
            existingFeedback.IsAnonymous = newFeedback.IsAnonymous;

            await context.SaveAsync(existingFeedback);
        }

        private QueryOperationConfig CreateQueryConfiguration(string locationId, FeedbackQueryParameters queryParams)
        {
            var config = new QueryOperationConfig
            {
                Limit = queryParams.PageSize + 1, // Request one extra item to determine if there are more pages
                BackwardSearch = queryParams.SortDirection?.ToLower() == "desc"
            };

            // Determine which index and query to use based on sort property and type filter
            if (queryParams.SortProperty?.ToLower() == "date")
            {
                if (queryParams.EnumType.HasValue)
                {
                    // Using DateByTypeIndex
                    config.IndexName = "DateByTypeIndex";
                    config.Filter = new QueryFilter();
                    config.Filter.AddCondition("locationId#type", QueryOperator.Equal,
                        $"{locationId}#{queryParams.EnumType.Value.ToDynamoDBType()}");
                }
                else
                {
                    // Using DateIndex
                    config.IndexName = "DateIndex";
                    config.Filter = new QueryFilter();
                    config.Filter.AddCondition("locationId", QueryOperator.Equal, locationId);
                }
            }
            else // Default to rating sort
            {
                if (queryParams.EnumType.HasValue)
                {
                    // Using RatingByTypeIndex
                    config.IndexName = "RatingByTypeIndex";
                    config.Filter = new QueryFilter();
                    config.Filter.AddCondition("locationId#type", QueryOperator.Equal,
                        $"{locationId}#{queryParams.EnumType.Value.ToDynamoDBType()}");
                }
                else
                {
                    // Using RatingIndex
                    config.IndexName = "RatingIndex";
                    config.Filter = new QueryFilter();
                    config.Filter.AddCondition("locationId", QueryOperator.Equal, locationId);
                }
            }

            return config;
        }

        public async Task<IEnumerable<Feedback>> GetServiceFeedbacks(string reservationId)
        {
            var key = $"{reservationId}#SERVICE_QUALITY";

            var queryConfig = new DynamoDBOperationConfig
            {
                IndexName = "ReservationTypeIndex"
            };

            var result = await context.QueryAsync<Feedback>(key, queryConfig).GetRemainingAsync();

            return result;
        }
    }
}