using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Restaurant.Domain.Entities.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReservationStatus
{
    
    [Description("Reserved")]
    Reserved,
    
    [Description("In Progress")]
    InProgress,
    
    [Description("Finished")]
    Finished,
    
    [Description("Canceled")]
    Canceled,
}