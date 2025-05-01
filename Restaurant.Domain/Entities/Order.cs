using Amazon.DynamoDBv2.DataModel;

namespace Restaurant.Domain.Entities;

[DynamoDBTable("Orders")]
public class Order
{
    [DynamoDBHashKey("id")] 
    public required string Id { get; set; }

    [DynamoDBGlobalSecondaryIndexHashKey("ReservationIdIndex")]
    [DynamoDBProperty("reservationId")] 
    public required string ReservationId { get; set; }

    [DynamoDBProperty("dishes")] 
    public required List<Dish> Dishes { get; set; } = new();
    
    [DynamoDBProperty("totalPrice")]
    public decimal TotalPrice { get; set; }

    [DynamoDBProperty("createdAt")] 
    public required string CreatedAt { get; set; }
}