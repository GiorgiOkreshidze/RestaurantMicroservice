using Amazon.DynamoDBv2.DataModel;

namespace Restaurant.Domain.Entities;

[DynamoDBTable("RestaurantTables")]
public class RestaurantTable
{
    [DynamoDBProperty("id")]
    public required string Id { get; set; }

    [DynamoDBProperty("tableNumber")]
    public required string TableNumber { get; set; }

    [DynamoDBProperty("capacity")]
    public required string Capacity { get; set; }

    [DynamoDBProperty("locationId")]
    public required string LocationId { get; set; }

    [DynamoDBProperty("locationAddress")]
    public required string LocationAddress { get; set; }
}