using Restaurant.Application.Exceptions;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Entities.Enums;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Application.Services;

public class OrderService(
    IDishRepository dishRepository,
    IOrderRepository orderRepository,
    IReservationRepository reservationRepository) : IOrderService
{
    public async Task AddDishToOrderAsync(string reservationId, string dishId, string userId)
    {
        var reservation = await ValidateReservationForDishModification(reservationId, "add", userId);
        var dish = await GetDishOrThrowAsync(dishId);
        var order = await GetOrCreateOrderAsync(reservation.Id);

        AddOrUpdateDish(order, dish);
        order.TotalPrice = Utils.CalculateTotalPrice(
            order.Dishes,
            d => d.Price,
            d => d.Quantity);

        await orderRepository.SaveAsync(order);
    }

    public async Task DeleteDishFromOrderAsync(string reservationId, string dishId, string userId)
    {
        var reservation = await ValidateReservationForDishModification(reservationId, "delete", userId);
        var dish = await GetDishOrThrowAsync(dishId);
        var order = await GetOrCreateOrderAsync(reservation.Id);
        
        if (RemoveOrDecrementDish(order, dish.Id))
        {
            order.TotalPrice = Utils.CalculateTotalPrice(
                order.Dishes,
                d => d.Price,
                d => d.Quantity);

            await orderRepository.SaveAsync(order);
        }
    }

    public async Task<List<Dish>> GetDishesInOrderAsync(string reservationId)
    {
        var reservation = await reservationRepository.GetReservationByIdAsync(reservationId);
        if (reservation == null)
            throw new NotFoundException("Reservation", reservationId);

        var order = await orderRepository.GetOrderByReservationIdAsync(reservationId);
        return order?.Dishes ?? new List<Dish>();
    }

    private async Task<Order> GetOrCreateOrderAsync(string reservationId)
    {
        var order = await orderRepository.GetOrderByReservationIdAsync(reservationId);
        if (order != null) return order;

        return new Order
        {
            Id = Guid.NewGuid().ToString(),
            ReservationId = reservationId,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            Dishes = new List<Dish>()
        };
    }

    private static void AddOrUpdateDish(Order order, Dish dish)
    {
        var existingDish = order.Dishes.FirstOrDefault(d => d.Id == dish.Id);
        if (existingDish != null)
        {
            existingDish.Quantity++;
            return;
        }

        order.Dishes.Add(new Dish
        {
            Id = dish.Id,
            Name = dish.Name,
            Price = dish.Price,
            IsPopular = dish.IsPopular,
            Weight = dish.Weight,
            ImageUrl = dish.ImageUrl,
            Quantity = 1
        });
    }

    private async Task<Dish> GetDishOrThrowAsync(string dishId)
    {
        var dish = await dishRepository.GetDishByIdAsync(dishId);
        if (dish == null)
            throw new NotFoundException("Dish", dishId);
        return dish;
    }
    
    private static bool RemoveOrDecrementDish(Order order, string dishId)
    {
        var existingDish = order.Dishes.FirstOrDefault(d => d.Id == dishId);
        if (existingDish == null)
            return false;

        if (existingDish.Quantity > 1)
        {
            existingDish.Quantity--;
        }
        else
        {
            order.Dishes.Remove(existingDish);
        }

        return true;
    }
    
    private async Task<Domain.Entities.Reservation> ValidateReservationForDishModification(string reservationId, string operation, string userId)
    {
        var reservation = await reservationRepository.GetReservationByIdAsync(reservationId)
                        ?? throw new NotFoundException("Reservation", reservationId);
        
        if (reservation.WaiterId != userId)
        {
            throw new UnauthorizedException("Only the assigned waiter can modify the order");
        }
        
        if (reservation.Status == ReservationStatus.Finished.ToString())
        {
            throw new ConflictException($"Cannot {operation} a dish on a completed reservation");
        }

        if (reservation.Status == Utils.GetEnumDescription(ReservationStatus.Canceled))
        {
            throw new ConflictException($"Cannot {operation} a dish on a reservation that is canceled");
        }
        return reservation;
    }
}
