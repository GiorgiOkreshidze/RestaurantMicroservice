using Restaurant.Domain.DTOs;
using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Interfaces;

public interface IDishRepository
{
    Task<IEnumerable<Dish>> GetSpecialtyDishesByLocationAsync(string locationId);

    Task<IEnumerable<Dish>> GetPopularDishesAsync();
    
    Task<Dish?> GetDishByIdAsync(string id);
    
    Task<IEnumerable<Dish>> GetAllDishesAsync(DishFilterDto filter);
}