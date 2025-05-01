using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetOrderByReservationIdAsync(string reservationId);
    
    Task SaveAsync(Order order);
}