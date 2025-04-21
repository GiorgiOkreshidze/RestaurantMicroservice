using Restaurant.Domain.Entities.Enums;

namespace Restaurant.Application.DTOs.Reservations;

public class WaiterReservationRequest : BaseReservationRequest
{
    public required ClientType ClientType { get; set; }
    
    public string? CustomerId { get; set; }
}