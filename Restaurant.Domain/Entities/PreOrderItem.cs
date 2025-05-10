using MongoDB.Bson.Serialization.Attributes;

namespace Restaurant.Domain.Entities;

public class PreOrderItem
{
    [BsonId]
    public required string Id { get; set; }
    
    public required string DishId { get; set; }

    public required string DishName { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public required string DishImageUrl { get; set; }

    public string Notes { get; set; } = string.Empty;
}
