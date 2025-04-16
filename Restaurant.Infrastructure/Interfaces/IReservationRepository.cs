using Restaurant.Domain.DTOs;
using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Interfaces;

public interface IReservationRepository
{
    Task<Reservation> UpsertReservationAsync(Reservation reservation);
    
    Task<bool> ReservationExistsAsync(string reservationId);
    
    Task<int> GetWaiterReservationCountAsync(string waiterId, string date);
    
    Task<Reservation?> GetReservationByIdAsync(string reservationId);
    
    Task<List<Reservation>> GetReservationsByDateLocationTable(string date, string locationAddress, string tableId);

    Task<IEnumerable<Reservation>> GetReservationsForDateAndLocation(string date, string locationId);

    Task<IEnumerable<Reservation>> GetCustomerReservationsAsync(string email);

    Task<IEnumerable<Reservation>> GetWaiterReservationsAsync(ReservationsQueryParametersDto queryParams, string waiterId);
}