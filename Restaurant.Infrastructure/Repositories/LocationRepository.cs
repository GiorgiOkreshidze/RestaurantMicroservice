using MongoDB.Driver;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories;

public class LocationRepository(IMongoDatabase database) : ILocationRepository
{
    private readonly IMongoCollection<Location> _collection = database.GetCollection<Location>("Locations");

    public async Task<IEnumerable<Location>> GetAllLocationsAsync()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }

    public async Task<Location?> GetLocationByIdAsync(string id)
    {
        return await _collection.Find(l => l.Id == id).FirstOrDefaultAsync();
    }
    
    public async Task<bool> LocationExistsAsync(string id)
    {
        return await _collection.Find(l => l.Id == id).AnyAsync();
    }
}
