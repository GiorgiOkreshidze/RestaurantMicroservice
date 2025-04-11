using AutoMapper;
using Restaurant.Application.DTOs.Locations;
using Restaurant.Application.Interfaces;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Application.Services
{
    public class LocationService : ILocationService
    {
        private readonly ILocationRepository _locationRepository;
        private readonly IMapper _mapper;

        public LocationService(ILocationRepository locationRepository, IMapper mapper)
        {
            _locationRepository = locationRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<LocationDto>> GetAllLocationsAsync()
        {
            var locations = await _locationRepository.GetAllLocationsAsync();
            return _mapper.Map<IEnumerable<LocationDto>>(locations);
        }

        public async Task<IEnumerable<LocationSelectOptionDto>> GetAllLocationsForDropDownAsync()
        {
            var locations = await _locationRepository.GetAllLocationsAsync();
            return locations.Select(location => new LocationSelectOptionDto
            {
                Id = location.Id,
                Address = location.Address
            });
        }

        /// <summary>
        /// Retrieves a location by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the location to retrieve.</param>
        /// <returns>A LocationDto object if found; null if no location exists with the specified id.</returns>
        public async Task<LocationDto?> GetLocationByIdAsync(string id)
        {
            var location = await _locationRepository.GetLocationByIdAsync(id);
            if (location == null)
            {
                return null;
            }

            return _mapper.Map<LocationDto>(location);
        }
    }
}