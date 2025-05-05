using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Interfaces
{
    public interface ILocationRepository
    {
        Task<IEnumerable<Location>> GetAllLocationsAsync();

        Task<Location?> GetLocationByIdAsync(string id);

        Task<bool> LocationExistsAsync(string id);
    }
}