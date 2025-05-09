using MongoDB.Driver;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories;

public class TableRepository(IMongoDatabase database) : ITableRepository
{
    private readonly IMongoCollection<RestaurantTable> _tables = database.GetCollection<RestaurantTable>("RestaurantTables");

    public async Task<RestaurantTable?> GetTableById(string tableId)
    {
        var filter = Builders<RestaurantTable>.Filter.Eq(t => t.Id, tableId);
        return await _tables.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<RestaurantTable>> GetTablesForLocationAsync(string locationId, int guests)
    {
        var filter = Builders<RestaurantTable>.Filter.And(
            Builders<RestaurantTable>.Filter.Eq(t => t.LocationId, locationId),
            Builders<RestaurantTable>.Filter.Gte(t => t.Capacity, guests)
        );

        return await _tables.Find(filter).ToListAsync();
    }
}
