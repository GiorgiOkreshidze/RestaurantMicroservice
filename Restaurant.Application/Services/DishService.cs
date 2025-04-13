using AutoMapper;
using Restaurant.Application.DTOs.Dishes;
using Restaurant.Application.Interfaces;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Application.Services
{
    public class DishService(IDishRepository dishRepository, ILocationRepository locationRepository, IMapper mapper)
        : IDishService
    {
        public async Task<IEnumerable<DishDto>> GetSpecialtyDishesByLocationAsync(string locationId)
        {
            var dishes = await dishRepository.GetSpecialtyDishesByLocationAsync(locationId);
            return mapper.Map<IEnumerable<DishDto>>(dishes);
        }

        public async Task<IEnumerable<DishDto>> GetPopularDishesAsync()
        {
            var dishes = await dishRepository.GetPopularDishesAsync();
            return mapper.Map<IEnumerable<DishDto>>(dishes);
        }
    }
}
