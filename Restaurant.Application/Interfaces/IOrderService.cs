using Restaurant.Domain.Entities;

namespace Restaurant.Application.Interfaces;

public interface IOrderService
{
    Task AddDishToOrderAsync(string reservationId, string dishId, string userId);
    
    Task DeleteDishFromOrderAsync(string reservationId, string dishId, string userId);

    Task<List<Dish>> GetDishesInOrderAsync(string reservationId);
}