using Amazon.DynamoDBv2.DataModel;
namespace Restaurant.Domain.Entities
{
    [DynamoDBTable("Locations")]
    public class Location
    {
        [DynamoDBHashKey("id")]
        public required string Id { get; set; }
        
        [DynamoDBProperty("address")]
        public required string Address { get; set; }
        
        [DynamoDBProperty("averageOccupancy")]
        public double AverageOccupancy { get; set; }
        
        [DynamoDBProperty("rating")]
        public double Rating { get; set; }
        
        [DynamoDBProperty("totalCapacity")]
        public int TotalCapacity { get; set; }
        
        [DynamoDBProperty("imageUrl")]
        public required string ImageUrl { get; set; }
        
        [DynamoDBProperty("description")]
        public required string Description { get; set; }
    }
}