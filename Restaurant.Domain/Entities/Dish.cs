using Amazon.DynamoDBv2.DataModel;

namespace Restaurant.Domain.Entities
{
    [DynamoDBTable("Dishes")]
    public class Dish
    {
        [DynamoDBHashKey("id")]
        public required string Id { get; set; }

        [DynamoDBProperty("name")]
        public required string Name { get; set; }

        [DynamoDBProperty("price")]
        public required string Price { get; set; }

        [DynamoDBProperty("weight")]
        public required string Weight { get; set; }

        [DynamoDBProperty("imageUrl")]
        public required string ImageUrl { get; set; }

        [DynamoDBProperty("calories")]
        public string? Calories { get; set; }

        [DynamoDBProperty("carbohydrates")]
        public string? Carbohydrates { get; set; }

        [DynamoDBProperty("description")]
        public string? Description { get; set; }

        [DynamoDBProperty("dishType")]
        public string? DishType { get; set; }

        [DynamoDBProperty("fats")]
        public string? Fats { get; set; }

        [DynamoDBProperty("proteins")]
        public string? Proteins { get; set; }

        [DynamoDBProperty("state")]
        public string? State { get; set; }

        [DynamoDBProperty("vitamins")]
        public string? Vitamins { get; set; }

        [DynamoDBProperty("isPopular")]
        public bool IsPopular { get; set; }

        [DynamoDBGlobalSecondaryIndexHashKey("GSI1")]
        [DynamoDBProperty("locationId")]
        public string? LocationId { get; set; }
    }
}
