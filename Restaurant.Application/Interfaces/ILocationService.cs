using Restaurant.Application.DTOs.Locations;

namespace Restaurant.Application.Interfaces
{
    public interface ILocationService
    {
        Task<IEnumerable<LocationDto>> GetAllLocationsAsync();

        Task<LocationDto?> GetLocationByIdAsync(string id);

        Task<IEnumerable<LocationSelectOptionDto>> GetAllLocationsForDropDownAsync();
    }
}