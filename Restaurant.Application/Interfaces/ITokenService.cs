using Restaurant.Domain.Entities;

namespace Restaurant.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);

        int GetAccessTokenExpiryInSeconds();

        (string PlainToken, RefreshToken RefreshTokenEntity) GenerateRefreshToken(string userId);

        string GetUserIdFromToken(string token);

        Task<RefreshToken?> GetRefreshTokenAsync(string token);

        Task RevokeRefreshTokenAsync(string token);

        Task SaveRefreshTokenAsync(RefreshToken refreshToken);
        
        string GenerateAnonymousFeedbackToken(string reservationId);
        
        bool ValidateAnonymousFeedbackToken(string token, out string? reservationId);
    }
}
