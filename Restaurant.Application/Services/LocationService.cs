using AutoMapper;
using Restaurant.Application.DTOs.Locations;
using Restaurant.Application.Exceptions;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Application.Services;

public class LocationService(ILocationRepository locationRepository, IMapper mapper) : ILocationService
{
    public async Task<IEnumerable<LocationDto>> GetAllLocationsAsync()
    {
        var locations = await locationRepository.GetAllLocationsAsync();
        return mapper.Map<IEnumerable<LocationDto>>(locations);
    }

    public async Task<IEnumerable<LocationSelectOptionDto>> GetAllLocationsForDropDownAsync()
    {
        var locations = await locationRepository.GetAllLocationsAsync();
        return mapper.Map<IEnumerable<LocationSelectOptionDto>>(locations);
    }
    
    public async Task<LocationDto?> GetLocationByIdAsync(string id)
    {
        var location = await locationRepository.GetLocationByIdAsync(id);
        if (location == null)
        {
            throw new NotFoundException("Location", id);
        }

        return mapper.Map<LocationDto>(location);
    }
}