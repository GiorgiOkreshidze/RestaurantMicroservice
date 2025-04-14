using Restaurant.Application.DTOs.Dishes;
using Restaurant.Domain.DTOs;

namespace Restaurant.Application.Interfaces;

public interface IDishService
{
    Task<IEnumerable<DishDto>> GetSpecialtyDishesByLocationAsync(string locationId);

    Task<IEnumerable<DishDto>> GetPopularDishesAsync();
    
    Task<DishDetailsDto?> GetDishByIdAsync(string id);
    
    Task<IEnumerable<DishDto>> GetAllDishesAsync(DishFilterDto filter);
}