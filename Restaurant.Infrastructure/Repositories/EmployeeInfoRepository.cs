using MongoDB.Driver;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories
{
    public class EmployeeRepository(IMongoDatabase database) : IEmployeeRepository
    {
        private readonly IMongoCollection<EmployeeInfo> _collection = database.GetCollection<EmployeeInfo>("EmployeeInfo");

        public async Task<EmployeeInfo?> GetWaiterByEmailAsync(string email)
        {
            return await _collection.Find(e => e.Email == email).FirstOrDefaultAsync();
        }
    }
}