using MongoDB.Driver;
using Restaurant.Application.DTOs.Feedbacks;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Entities.Enums;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories;

public class FeedbackRepository : IFeedbackRepository
{
    private readonly IMongoCollection<Feedback> _collection;

    public FeedbackRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Feedback>("Feedbacks");
            
        CreateIndexes();
    }

    private void CreateIndexes()
    {
        _collection.Indexes.CreateOne(new CreateIndexModel<Feedback>( //equivalent to DateIndex and RatingIndex hash key
            Builders<Feedback>.IndexKeys.Ascending(f => f.LocationId)));

        _collection.Indexes.CreateOne(new CreateIndexModel<Feedback>( //equivalent to RatingByTypeIndex and DateByTypeIndex hash key
            Builders<Feedback>.IndexKeys.Ascending(f => f.LocationIdType)));

        _collection.Indexes.CreateOne(new CreateIndexModel<Feedback>( //equivalent to ReservationTypeIndex hash key
            Builders<Feedback>.IndexKeys.Ascending(f => f.ReservationIdType)));

        _collection.Indexes.CreateOne(new CreateIndexModel<Feedback>( //equivalent to DateIndex and DateByTypeIndex range key
            Builders<Feedback>.IndexKeys
                .Ascending(f => f.LocationId)
                .Ascending(f => f.Date)));

        _collection.Indexes.CreateOne(new CreateIndexModel<Feedback>( //equivalent to RatingIndex and RatingByTypeIndex range key
            Builders<Feedback>.IndexKeys
                .Ascending(f => f.LocationId)
                .Ascending(f => f.Rate)));
    }
        
    public async Task<(List<Feedback>, string?)> GetFeedbacksAsync(string id, FeedbackQueryParameters queryParams)
    {
        var filter = CreateQueryFilter(id, queryParams);
        var sort = CreateSortDefinition(queryParams);

        int itemsToSkip = 0;
        if (!string.IsNullOrEmpty(queryParams.NextPageToken) &&
            int.TryParse(queryParams.NextPageToken, out int skipCount))
        {
            itemsToSkip = skipCount;
        }

        var query = _collection.Find(filter)
            .Sort(sort)
            .Skip(itemsToSkip)
            .Limit(queryParams.PageSize + 1); // Request one extra item for pagination

        var pagedResults = await query.ToListAsync();

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

        var filter = Builders<Feedback>.Filter.Eq(f => f.ReservationIdType, reservationIdTypeKey);
        var existingFeedback = await _collection.Find(filter).FirstOrDefaultAsync();

        if (existingFeedback != null && !string.IsNullOrEmpty(existingFeedback.LocationId) &&
            !string.IsNullOrEmpty(existingFeedback.TypeDate))
        {
            await UpdateExistingFeedbackRateAndCommentAsync(feedback, existingFeedback);
        }
        else
        {
            feedback.TypeDate = $"{feedback.Type}#{feedback.Date}";
            await _collection.InsertOneAsync(feedback);
        }
    }
        
    private async Task UpdateExistingFeedbackRateAndCommentAsync(Feedback newFeedback, Feedback existingFeedback)
    {
        var filter = Builders<Feedback>.Filter.Eq(f => f.Id, existingFeedback.Id);
        var update = Builders<Feedback>.Update
            .Set(f => f.Rate, newFeedback.Rate)
            .Set(f => f.Comment, newFeedback.Comment)
            .Set(f => f.TypeDate, $"{newFeedback.Type}#{newFeedback.Date}")
            .Set(f => f.IsAnonymous, newFeedback.IsAnonymous);

        await _collection.UpdateOneAsync(filter, update);
    }
        
    private static FilterDefinition<Feedback> CreateQueryFilter(string locationId, FeedbackQueryParameters queryParams)
    {
        var filterBuilder = Builders<Feedback>.Filter;

        if (queryParams.SortProperty.ToLower() == "date")
        {
            if (queryParams.EnumType.HasValue)
            {
                return filterBuilder.Eq(f => 
                        f.LocationIdType, $"{locationId}#{queryParams.EnumType.Value.ToDynamoDBType()}"); //DateByTypeIndex
            }
            
            return filterBuilder.Eq(f => f.LocationId, locationId); //DateIndex
        }

        // Default to rating sort
        if (queryParams.EnumType.HasValue)
        {
            return filterBuilder.Eq(f => 
                    f.LocationIdType, $"{locationId}#{queryParams.EnumType.Value.ToDynamoDBType()}"); //RatingByTypeIndex
        }
        
        return filterBuilder.Eq(f => f.LocationId, locationId); //RatingIndex
    }
        
    public async Task<IEnumerable<Feedback>> GetServiceFeedbacks(string reservationId)
    {
        var key = $"{reservationId}#SERVICE_QUALITY";
        var filter = Builders<Feedback>.Filter.Eq(f => f.ReservationIdType, key);
        return await _collection.Find(filter).ToListAsync();
    }
        
    private static SortDefinition<Feedback> CreateSortDefinition(FeedbackQueryParameters queryParams)
    {
        var sortBuilder = Builders<Feedback>.Sort;
        bool isDescending = queryParams.SortDirection?.ToLower() == "desc";

        if (queryParams.SortProperty?.ToLower() == "date")
        {
            return isDescending ? sortBuilder.Descending(f => f.Date) : sortBuilder.Ascending(f => f.Date);
        }

        return isDescending ? sortBuilder.Descending(f => f.Rate) : sortBuilder.Ascending(f => f.Rate);
    }
}