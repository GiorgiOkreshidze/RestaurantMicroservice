using Amazon.DynamoDBv2.DataModel;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories
{
    public class DishRepository : IDishRepository
    {
        private readonly IDynamoDBContext _context;

        public DishRepository(IDynamoDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Dish>> GetSpecialtyDishesByLocationAsync(string locationId)
        {
            var query = _context.QueryAsync<Dish>(locationId, new DynamoDBOperationConfig
            {
                IndexName = "GSI1"
            });

            var dishes = await query.GetRemainingAsync();
            return dishes.Where(d => d.IsPopular);
        }
    }
}
