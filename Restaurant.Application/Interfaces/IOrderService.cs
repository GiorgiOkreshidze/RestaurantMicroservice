using Restaurant.Domain.Entities;

namespace Restaurant.Application.Interfaces;

public interface IOrderService
{
    Task AddDishToOrderAsync(string reservationId, string dishId);
    
    Task DeleteDishFromOrderAsync(string reservationId, string dishId);

    Task<List<Dish>> GetDishesInOrderAsync(string reservationId);
}