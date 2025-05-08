using MongoDB.Driver;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Entities.Enums;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories;

public class WaiterRepository(IMongoDatabase database) : IWaiterRepository
{
    private readonly IMongoCollection<User> _collection = database.GetCollection<User>("Users");
    
    public async Task<List<User>> GetWaitersByLocationAsync(string locationId)
    {
        var filter = Builders<User>.Filter.And(
            Builders<User>.Filter.Eq(u => u.LocationId, locationId),
            Builders<User>.Filter.Eq(u => u.RoleString, Role.Waiter.ToString())
        );

        return await _collection.Find(filter).ToListAsync();
    }
}