using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Interfaces;

public interface ITableRepository
{
    Task<RestaurantTable?> GetTableById(string tableId);

    Task<IEnumerable<RestaurantTable>> GetTablesForLocationAsync(string locationId, int guests);
}