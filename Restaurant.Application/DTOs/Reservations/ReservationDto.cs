using Restaurant.Domain.Entities.Enums;

namespace Restaurant.Application.DTOs.Reservations;

public class ReservationDto : BaseReservationRequest
{
    public required string LocationAddress { get; set; }

    public required string PreOrder { get; set; }
    
    public required string Status { get; set; }
    
    public required string TableCapacity { get; set; }

    public required string TableNumber { get; set; }
    
    public required string TimeSlot { get; set; }
    
    public string? UserInfo { get; set; }
    
    public string? UserEmail { get; set; }
    
    public string? WaiterId { get; set; }

    public required string CreatedAt { get; set; }
    
    public ClientType? ClientType { get; set; }
}