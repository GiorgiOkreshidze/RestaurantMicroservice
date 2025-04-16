using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
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

    public async Task<IEnumerable<RestaurantTable>> GetTablesForLocationAsync(string locationId, int guests)
    {
        // Since we don't have the appropriate GSI yet, we'll use a scan operation with filters
        var scanConditions = new List<ScanCondition>
    {
        new ScanCondition("LocationId", ScanOperator.Equal, locationId),
        new ScanCondition("Capacity", ScanOperator.GreaterThanOrEqual, guests)
    };

        // Execute the scan operation
        var tables = await context.ScanAsync<RestaurantTable>(scanConditions, new DynamoDBOperationConfig
        {
            Conversion = DynamoDBEntryConversion.V2
        }).GetRemainingAsync();

        return tables;
    }
}