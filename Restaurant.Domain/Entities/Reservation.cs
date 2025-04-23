using Amazon.DynamoDBv2.DataModel;
using Restaurant.Domain.Entities.Enums;

namespace Restaurant.Domain.Entities;

[DynamoDBTable("Reservations")]
public class Reservation
{
    [DynamoDBHashKey("id")]
    public required string Id { get; set; }
    
    [DynamoDBProperty("date")]
    public required string Date { get; set; }
    
    [DynamoDBProperty("guestsNumber")]
    public required string GuestsNumber { get; set; }
    
    [DynamoDBProperty("locationId")]
    public required string LocationId { get; set; }
    
    [DynamoDBProperty("locationAddress")]
    public required string LocationAddress { get; set; }

    [DynamoDBProperty("preOrder")]
    public required string PreOrder { get; set; }

    [DynamoDBProperty("status")]
    public required string Status { get; set; }

    [DynamoDBProperty("tableId")]
    public required string TableId { get; set; }
    
    [DynamoDBProperty("tableCapacity")]
    public required string TableCapacity { get; set; }

    [DynamoDBProperty("tableNumber")]
    public required string TableNumber { get; set; }

    [DynamoDBProperty("timeFrom")]
    public required string TimeFrom { get; set; }

    [DynamoDBProperty("timeTo")]
    public required string TimeTo { get; set; }
    
    [DynamoDBProperty("timeSlot")]
    public required string TimeSlot { get; set; }
    
    [DynamoDBProperty("userInfo")]
    public string? UserInfo { get; set; }
    
    [DynamoDBProperty("userEmail")]
    public string? UserEmail { get; set; }
    
    [DynamoDBProperty("waiterId")]
    public string? WaiterId { get; set; }

    [DynamoDBProperty("createdAt")]
    public required string CreatedAt { get; set; }

    [DynamoDBIgnore] 
    public ClientType? ClientType { get; set; } = Enums.ClientType.CUSTOMER;
    
    [DynamoDBProperty("clientType")]
    public string ClientTypeString
    {
        get => ClientType.ToString()!;
        set => ClientType = Enum.TryParse<ClientType>(value, out var role) ? role : Enums.ClientType.CUSTOMER;
    }
}