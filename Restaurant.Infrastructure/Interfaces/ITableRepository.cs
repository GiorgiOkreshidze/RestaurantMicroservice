using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Interfaces;

public interface ITableRepository
{
    Task<RestaurantTable?> GetTableById(string tableId);
}