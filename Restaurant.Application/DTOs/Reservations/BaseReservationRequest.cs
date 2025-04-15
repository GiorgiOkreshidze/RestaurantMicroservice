namespace Restaurant.Application.DTOs.Reservations;

public class BaseReservationRequest
{
    public string? Id { get; set; }

    public required string LocationId { get; set; }

    public required string TableId { get; set; }

    public required string Date { get; set; }

    public required string GuestsNumber { get; set; }

    public required string TimeFrom { get; set; }

    public required string TimeTo { get; set; }
}