using Amazon.DynamoDBv2.DataModel;
using MongoDB.Bson.Serialization.Attributes;

namespace Restaurant.Domain.Entities;

public class EmployeeInfo
{   
    [BsonId]
    public required string Email { get; set; }

    public required string LocationId { get; set; }
    
    public string? Role { get; set; }
}