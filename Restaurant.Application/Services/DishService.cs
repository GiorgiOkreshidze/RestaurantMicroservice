using AutoMapper;
using Restaurant.Application.DTOs.Locations;
using Restaurant.Application.Exceptions;
using Restaurant.Application.Interfaces;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Application.Services
{
    public class DishService : IDishService
    {
        private readonly IDishRepository _dishRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IMapper _mapper;

        public DishService(IDishRepository dishRepository, ILocationRepository locationRepository, IMapper mapper)
        {
            _dishRepository = dishRepository;
            _locationRepository = locationRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<LocationDishResponseDto>> GetSpecialtyDishesByLocationAsync(string locationId)
        {
            var location = await _locationRepository.GetLocationByIdAsync(locationId);

            if (location == null)
            {
                throw new NotFoundException("Location", locationId);
            }

            var dishes = await _dishRepository.GetSpecialtyDishesByLocationAsync(locationId);
            return _mapper.Map<IEnumerable<LocationDishResponseDto>>(dishes);
        }
    }
}
