using Amazon.DynamoDBv2.DataModel;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace Restaurant.Infrastructure.Repositories;

public class LocationRepository(IDynamoDBContext context, ILogger<LocationRepository> logger)
    : ILocationRepository
{
    private readonly ILogger<LocationRepository> _logger = logger;

        public async Task<IEnumerable<Location>> GetAllLocationsAsync()
        {
            var conditions = new List<ScanCondition>();
            if (conditions == null) throw new ArgumentNullException(nameof(conditions));

            return await context.ScanAsync<Location>(conditions).GetRemainingAsync();
        }

        public async Task<Location?> GetLocationByIdAsync(string id)
        {
            var location = await context.LoadAsync<Location>(id);
            return location;
        }
}