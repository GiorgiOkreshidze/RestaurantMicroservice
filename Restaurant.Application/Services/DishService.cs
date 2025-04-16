using AutoMapper;
using Restaurant.Application.DTOs.Dishes;
using Restaurant.Application.Exceptions;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.DTOs;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Application.Services
{
    public class DishService(IDishRepository dishRepository,ILocationRepository locationRepository, IMapper mapper)
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

        public async Task<DishDetailsDto?> GetDishByIdAsync(string id)
        {
            var dish = await dishRepository.GetDishByIdAsync(id);
            
            if (dish == null)
            {
                throw new NotFoundException("Dish", id);
            }

            return mapper.Map<DishDetailsDto>(dish);
        }

        public async Task<IEnumerable<DishDto>> GetAllDishesAsync(DishFilterDto filter)
        {
            var dishes = await dishRepository.GetAllDishesAsync(filter);
            return dishes.Select(d => mapper.Map<DishDto>(d));
        }
    }
}
