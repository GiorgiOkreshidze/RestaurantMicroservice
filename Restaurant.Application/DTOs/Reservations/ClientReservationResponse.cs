using Restaurant.Domain.Entities.Enums;

namespace Restaurant.Application.DTOs.Reservations;

public class ClientReservationResponse
{
    public string? Id { get; set; }
    
    public required string Date { get; set; }
    
    public required string GuestsNumber { get; set; }

    public required string LocationId { get; set; }
    
    public required string LocationAddress { get; set; }
    
    public required string PreOrder { get; set; }
    
    public required string Status { get; set; }
    
    public required string TableId { get; set; }
    
    public required string TableNumber { get; set; }

    public required string TimeFrom { get; set; }

    public required string TimeTo { get; set; }
    
    public required string TimeSlot { get; set; }
    
    public string? UserInfo { get; set; }
    
    public string? UserEmail { get; set; }
    
    public string? WaiterId { get; set; }

    public required string CreatedAt { get; set; }
    
    public ClientType? ClientType { get; set; }
}