using Restaurant.Application.DTOs.Reservations;

namespace Restaurant.Application.Interfaces;

public interface IReservationService
{
    Task<ReservationDto> UpsertReservationAsync(BaseReservationRequest reservationRequest, string userId);
}