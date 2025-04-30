using Restaurant.Application.DTOs.Reservations;
using Restaurant.Application.DTOs.Tables;

namespace Restaurant.Application.Interfaces;

public interface IReservationService
{
    Task<ClientReservationResponse> UpsertReservationAsync(BaseReservationRequest reservationRequest, string userId);

    Task<IEnumerable<AvailableTableDto>> GetAvailableTablesAsync(FilterParameters filterParameters);

    Task<IEnumerable<ReservationResponseDto>> GetReservationsAsync(
       ReservationsQueryParameters queryParams,
       string userId,
       string email,
       string role);

    Task<ReservationResponseDto> CancelReservationAsync(string reservationId, string userId, string role);

    Task<QrCodeResponse> CompleteReservationAsync(string reservationId);
}