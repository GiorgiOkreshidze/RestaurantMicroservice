using MongoDB.Bson.Serialization.Attributes;

namespace Restaurant.Domain.Entities;

public class Feedback
{
    [BsonId]
    public required string Id { get; set; }

    public required string LocationId { get; set; }

    public required string Type { get; set; }

    public required string TypeDate { get; set; }

    public required int Rate { get; set; }

    public required string Comment { get; set; }

    public required string UserName { get; set; }

    public required string UserAvatarUrl { get; set; }

    public required string Date { get; set; }

    public string ReservationId { get; set; } = null!;

    public required string LocationIdType { get; set; }

    public required string ReservationIdType { get; set; }

    public bool IsAnonymous { get; set; } = false;
}