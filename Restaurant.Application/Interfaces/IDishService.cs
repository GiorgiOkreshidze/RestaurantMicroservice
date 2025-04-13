using Restaurant.Application.DTOs.Dishes;

namespace Restaurant.Application.Interfaces;

public interface IDishService
{
    Task<IEnumerable<DishDto>> GetSpecialtyDishesByLocationAsync(string locationId);

    Task<IEnumerable<DishDto>> GetPopularDishesAsync();
}