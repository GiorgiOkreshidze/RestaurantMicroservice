using Amazon.DynamoDBv2.DataModel;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories;

public class LocationRepository(IDynamoDBContext context)
    : ILocationRepository
{
    public async Task<IEnumerable<Location>> GetAllLocationsAsync()
    {
        return await context.ScanAsync<Location>(new List<ScanCondition>()).GetRemainingAsync();
    }

    public async Task<Location?> GetLocationByIdAsync(string id)
    {
        var location = await context.LoadAsync<Location>(id);
        return location ?? null;
    }
    
    public async Task<bool> LocationExistsAsync(string id)
    {
        var location = await GetLocationByIdAsync(id);
        return location != null;
    }
}
