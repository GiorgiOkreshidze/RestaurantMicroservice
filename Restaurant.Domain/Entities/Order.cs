using MongoDB.Bson.Serialization.Attributes;

namespace Restaurant.Domain.Entities;

public class Order
{
    [BsonId]
    public required string Id { get; set; }
    
    public required string ReservationId { get; set; }

    public required List<Dish> Dishes { get; set; } = new();
    
    public decimal TotalPrice { get; set; }

    public required string CreatedAt { get; set; }
}