using Amazon.DynamoDBv2.DataModel;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories
{
    public class RefreshTokenRepository(IDynamoDBContext context) : IRefreshTokenRepository
    {
        public async Task<RefreshToken?> GetByTokenAsync(string hashedToken)
        {
            var tokens = await context.QueryAsync<RefreshToken>(hashedToken,
                new DynamoDBOperationConfig
                {
                    IndexName = "TokenIndex"
                }).GetRemainingAsync();

            return tokens.FirstOrDefault();
        }

        public async Task RevokeTokenAsync(string token)
        {
            var existingToken = await GetByTokenAsync(token);
            if (existingToken != null)
            {
                existingToken.IsRevoked = true;
                await context.SaveAsync(existingToken);
            }
        }

        public async Task SaveTokenAsync(RefreshToken refreshToken)
        {
            await context.SaveAsync(refreshToken);
        }
    }
}
