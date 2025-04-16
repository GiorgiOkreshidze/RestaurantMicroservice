using Restaurant.Application.DTOs.Reservations;
using Restaurant.Application.DTOs.Tables;

namespace Restaurant.Application.Interfaces;

public interface IReservationService
{
    Task<ClientReservationResponse> UpsertReservationAsync(BaseReservationRequest reservationRequest, string userId);

    Task<IEnumerable<AvailableTableDto>> GetAvailableTablesAsync(FilterParameters filterParameters);
}