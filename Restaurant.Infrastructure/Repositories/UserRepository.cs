using Amazon.DynamoDBv2.DataModel;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories
{
    public class UserRepository(IDynamoDBContext context) : IUserRepository
    {

        public async Task<User> GetUserByIdAsync(string id)
        {
            return await context.LoadAsync<User>(id);
        }

        public async Task<string> SignupAsync(User user)
        {
            user.Id = Guid.NewGuid().ToString();

            await context.SaveAsync(user);
            return user.Email;
        }

        public async Task<bool> DoesEmailExistAsync(string email)
        {
            var queryResults = await context.QueryAsync<User>(email,
                new DynamoDBOperationConfig
                {
                    IndexName = "GSI1"
                }).GetRemainingAsync();

            return queryResults.Count > 0;
        }

    }
}
