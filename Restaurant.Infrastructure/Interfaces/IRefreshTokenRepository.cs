using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task SaveTokenAsync(RefreshToken refreshToken);

        Task<RefreshToken?> GetByTokenAsync(string hashedToken);

        Task RevokeTokenAsync(string token);
    }
}
