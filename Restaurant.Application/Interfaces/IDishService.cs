using Restaurant.Application.DTOs.Locations;

namespace Restaurant.Application.Interfaces
{
    public interface IDishService
    {
        Task<IEnumerable<LocationDishResponseDto>> GetSpecialtyDishesByLocationAsync(string locationId);
    }
}
