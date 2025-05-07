using Amazon.DynamoDBv2.DataModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Restaurant.Domain.Entities
{
    public class Location
    {
        [BsonId]
        public required string Id { get; set; }
        public required string Address { get; set; }
        public double AverageOccupancy { get; set; }
        public double Rating { get; set; }
        public int TotalCapacity { get; set; }
        public required string ImageUrl { get; set; }
        public required string Description { get; set; }
    }
}