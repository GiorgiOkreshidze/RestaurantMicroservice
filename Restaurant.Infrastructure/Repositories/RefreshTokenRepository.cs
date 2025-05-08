using MongoDB.Driver;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly IMongoCollection<RefreshToken> _collection;

        public RefreshTokenRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<RefreshToken>("RefreshTokens");
            
            var tokenIndex = Builders<RefreshToken>.IndexKeys.Ascending(t => t.Token);
            var userIdIndex = Builders<RefreshToken>.IndexKeys.Ascending(t => t.UserId);
            
            _collection.Indexes.CreateOne(new CreateIndexModel<RefreshToken>(
                tokenIndex, new CreateIndexOptions { Name = "Token_Index", Unique = true }));
            _collection.Indexes.CreateOne(new CreateIndexModel<RefreshToken>(
                userIdIndex, new CreateIndexOptions { Name = "UserId_Index" }));
        }
        
        public async Task<RefreshToken?> GetByTokenAsync(string hashedToken)
        {
            return await _collection.Find(t => t.Token == hashedToken).FirstOrDefaultAsync();
        }

        public async Task RevokeTokenAsync(string token)
        {
            var update = Builders<RefreshToken>.Update.Set(t => t.IsRevoked, true);
            await _collection.UpdateOneAsync(t => t.Token == token, update);
        }

        public async Task SaveTokenAsync(RefreshToken refreshToken)
        {
            await _collection.InsertOneAsync(refreshToken);
        }
    }
}
