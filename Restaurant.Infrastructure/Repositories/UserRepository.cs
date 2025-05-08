using MongoDB.Driver;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Entities.Enums;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IMongoCollection<User> _collection;
        
        public UserRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<User>("Users");
            
            // Create email index
            var indexKeysDefinition = Builders<User>.IndexKeys.Ascending(u => u.Email);
            var indexOptions = new CreateIndexOptions { Name = "Email_Index", Unique = true };
            var indexModel = new CreateIndexModel<User>(indexKeysDefinition, indexOptions);
            _collection.Indexes.CreateOne(indexModel);
        }
        
        public async Task<User?> GetUserByIdAsync(string id)
        {
            return await _collection.Find(u => u.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _collection.Find(u => u.RoleString == Role.Customer.ToString()).ToListAsync();
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _collection.Find(u => u.Email == email).FirstOrDefaultAsync();
        }

        public async Task<string> SignupAsync(User user)
        {
            user.Id = Guid.NewGuid().ToString();
            await _collection.InsertOneAsync(user);
            return user.Email;
        }

        public async Task<bool> DoesEmailExistAsync(string email)
        {
            return await _collection.Find(u => u.Email == email).AnyAsync();
        }

        public async Task UpdatePasswordAsync(string userId, string newPasswordHash)
        {
            var update = Builders<User>.Update.Set(u => u.PasswordHash, newPasswordHash);
            await _collection.UpdateOneAsync(u => u.Id == userId, update);
        }
        
        public async Task UpdateProfileAsync(User user)
        {
            await _collection.ReplaceOneAsync(u => u.Id == user.Id, user);
        }
    }
}
