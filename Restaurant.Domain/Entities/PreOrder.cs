using MongoDB.Bson.Serialization.Attributes;

namespace Restaurant.Domain.Entities;

public class  PreOrder
{
    [BsonId]
    public required string Id { get; set; }
    
    public required string UserId { get; set; }

    public required string ReservationId { get; set; }

    public required string Status { get; set; }

    public DateTime CreateDate { get; set; }
    
    public required string TimeSlot { get; set; }

    public required string ReservationDate { get; set; }

    public decimal TotalPrice { get; set; }

    public required string Address { get; set; }

    public List<PreOrderItem> Items { get; set; }
 }