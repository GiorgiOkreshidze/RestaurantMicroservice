using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Entities.Enums;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories;

public class WaiterRepository(IDynamoDBContext context) : IWaiterRepository
{
    public async Task<List<User>> GetWaitersByLocationAsync(string locationId)
    {
        var scanCondition = new List<ScanCondition>
        {
            new("LocationId", ScanOperator.Equal, locationId),
            new("RoleString", ScanOperator.Equal, Role.Waiter.ToString())
        };

        var users = await context.ScanAsync<User>(scanCondition, new DynamoDBOperationConfig
        {
            Conversion = DynamoDBEntryConversion.V2
        }).GetRemainingAsync();

        return users;
    }
}