using MongoDB.Bson.Serialization.Attributes;

namespace Restaurant.Domain.Entities;

public class RestaurantTable
{
    [BsonId]
    public required string Id { get; set; }
    
    public required string TableNumber { get; set; }

    public required int Capacity { get; set; }

    public required string LocationId { get; set; }
    
    public required string LocationAddress { get; set; }
}
