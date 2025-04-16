using System.Text.Json.Serialization;

namespace Restaurant.Domain.Entities.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReservationStatus
{
    Reserved,
    InProgress,
    Finished,
    Cancelled,
}