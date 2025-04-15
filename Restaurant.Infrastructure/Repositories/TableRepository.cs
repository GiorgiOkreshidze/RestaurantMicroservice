using Amazon.DynamoDBv2.DataModel;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories;

public class TableRepository(IDynamoDBContext context) : ITableRepository
{
    public async Task<RestaurantTable?> GetTableById(string id)
    {
        var table = await context.LoadAsync<RestaurantTable>(id);
        return table ?? null;
    }
}