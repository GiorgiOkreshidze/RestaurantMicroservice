using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Interfaces;

public interface IDishRepository
{
    Task<IEnumerable<Dish>> GetSpecialtyDishesByLocationAsync(string locationId);

    Task<IEnumerable<Dish>> GetPopularDishesAsync();
}