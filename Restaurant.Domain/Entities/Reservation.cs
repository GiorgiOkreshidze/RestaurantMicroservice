using Amazon.DynamoDBv2.DataModel;
using MongoDB.Bson.Serialization.Attributes;
using Restaurant.Domain.Entities.Enums;

namespace Restaurant.Domain.Entities;

[DynamoDBTable("Reservations")]
public class Reservation
{
    [BsonId]
    public required string Id { get; set; }
    
    public required string Date { get; set; }
    
    public required string GuestsNumber { get; set; }
    
    public required string LocationId { get; set; }
    
    public required string LocationAddress { get; set; }

    public required string PreOrder { get; set; }
    
    public string? Order { get; set; } = string.Empty;

    public required string Status { get; set; }

    public required string TableId { get; set; }
    
    public required string TableCapacity { get; set; }

    public required string TableNumber { get; set; }

    public required string TimeFrom { get; set; }

    public required string TimeTo { get; set; }
    
    public required string TimeSlot { get; set; }
    
    public string? UserInfo { get; set; }
    
    public string? UserEmail { get; set; }
    
    public string? WaiterId { get; set; }

    public required string CreatedAt { get; set; }

    public string FeedbackToken { get; set; } = string.Empty;

    [BsonIgnore] 
    public ClientType? ClientType { get; set; } = Enums.ClientType.CUSTOMER;
    
    [BsonElement("ClientType")]
    public string ClientTypeString
    {
        get => ClientType.ToString()!;
        set => ClientType = Enum.TryParse<ClientType>(value, out var role) ? role : Enums.ClientType.CUSTOMER;
    }
}