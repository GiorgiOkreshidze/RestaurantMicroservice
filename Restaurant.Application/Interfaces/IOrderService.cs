namespace Restaurant.Application.Interfaces;

public interface IOrderService
{
    Task AddDishToOrderAsync(string reservationId, string dishId);
}