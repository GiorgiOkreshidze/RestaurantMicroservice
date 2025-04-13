using Amazon.DynamoDBv2.DataModel;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories
{
    public class EmployeeRepository(IDynamoDBContext context) : IEmployeeRepository
    {
        public async Task<EmployeeInfo?> GetWaiterByEmailAsync(string email)
        {
            return await context.LoadAsync<EmployeeInfo>(email);
        }
    }
}