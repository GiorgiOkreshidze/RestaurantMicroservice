using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Entities.Enums;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories
{
    public class UserRepository(IDynamoDBContext context) : IUserRepository
    {

        public async Task<User?> GetUserByIdAsync(string id)
        {
            return await context.LoadAsync<User>(id);
        }

        public Task<List<User>> GetAllUsersAsync()
        {
            var scanConditions = new List<ScanCondition>
            {
                new("RoleString", ScanOperator.Equal, Role.Customer.ToString())
            };
            
            var users = context.ScanAsync<User>(scanConditions, new DynamoDBOperationConfig
            {
                Conversion = DynamoDBEntryConversion.V2
            }).GetRemainingAsync();
            
            return users;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            var users = await context.QueryAsync<User>(email,
                new DynamoDBOperationConfig
                {
                    IndexName = "GSI1"
                }).GetRemainingAsync();

            return users.FirstOrDefault();
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

        public async Task UpdatePasswordAsync(string userId, string newPasswordHash)
        {
            var user = await GetUserByIdAsync(userId);
            if (user != null)
            {
                user.PasswordHash = newPasswordHash;
                await context.SaveAsync(user);
            }
        }
        
        public async Task UpdateProfileAsync(User user)
        {
            await context.SaveAsync(user);
        }
    }
}
