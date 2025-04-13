using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories;

public class DishRepository(IDynamoDBContext context) : IDishRepository
{
    public async Task<IEnumerable<Dish>> GetSpecialtyDishesByLocationAsync(string locationId)
    {
        var query = context.QueryAsync<Dish>(locationId, new DynamoDBOperationConfig
        {
            IndexName = "GSI1"
        });

        var dishes = await query.GetRemainingAsync();
        return dishes.Where(d => d.IsPopular);
    }

    public async Task<IEnumerable<Dish>> GetPopularDishesAsync()
    {
        var condition = new ScanCondition("IsPopular", ScanOperator.Equal, true);
        var dishes = await context.ScanAsync<Dish>(
            new List<ScanCondition> { condition },
            new DynamoDBOperationConfig
            {
                Conversion = DynamoDBEntryConversion.V2
            }).GetRemainingAsync();
        
        return dishes;
    }
}