using Amazon.DynamoDBv2.DataModel;
namespace Restaurant.Domain.Entities
{
    [DynamoDBTable("tm2-Locations-dev5")]
    public class Location
    {
        [DynamoDBHashKey("id")]
        public string Id { get; set; }
        
        [DynamoDBProperty("address")]
        public string Address { get; set; }
        
        [DynamoDBProperty("averageOccupancy")]
        public double AverageOccupancy { get; set; }
        
        [DynamoDBProperty("rating")]
        public double Rating { get; set; }
        
        [DynamoDBProperty("totalCapacity")]
        public int TotalCapacity { get; set; }
        
        [DynamoDBProperty("imageUrl")]
        public string ImageUrl { get; set; }
        
        [DynamoDBProperty("description")]
        public string Description { get; set; }
    }
}