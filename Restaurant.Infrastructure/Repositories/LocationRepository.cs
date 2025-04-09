using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Restaurant.Infrastructure.Repositories
{
    public class LocationRepository : ILocationRepository
    {
        private readonly IDynamoDBContext _context;

        public LocationRepository(IDynamoDBContext context, IConfiguration configuration)
        {
            _context = context;
        }

        public async Task<IEnumerable<Location>> GetAllLocationsAsync()
        {
            var conditions = new List<ScanCondition>();
            return await _context.ScanAsync<Location>(conditions).GetRemainingAsync();
        }
    }
}