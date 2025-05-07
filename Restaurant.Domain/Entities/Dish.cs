using Amazon.DynamoDBv2.DataModel;
using MongoDB.Bson.Serialization.Attributes;

namespace Restaurant.Domain.Entities;

public class Dish
{
    [BsonId]
    public required string Id { get; set; }
    
    public required string Name { get; set; }

    public required decimal Price { get; set; }

    public required string Weight { get; set; }

    public required string ImageUrl { get; set; }
    
    public string? Calories { get; set; }

    public string? Carbohydrates { get; set; }

    public string? Description { get; set; }
    
    public string? DishType { get; set; }
    
    public string? Fats { get; set; }

    public string? Proteins { get; set; }

    public string? State { get; set; }

    public string? Vitamins { get; set; }

    public bool IsPopular { get; set; }

    public string? LocationId { get; set; }

    public int Quantity { get; set; }
}