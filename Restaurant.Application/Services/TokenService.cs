using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Restaurant.Application.DTOs.Auth;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Application.Services
{
    public class TokenService(IOptions<JwtSettings> jwtSettings, IRefreshTokenRepository refreshTokenRepository) : ITokenService
    {
        public string GenerateAccessToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtSettings.Value.Key);
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id!),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(ClaimTypes.Role, user.RoleString),
                new("firstName", user.FirstName),
                new("lastName", user.LastName)
            };

            if (!string.IsNullOrEmpty(user.LocationId))
            {
                claims.Add(new Claim("locationId", user.LocationId));
            }

            var signingCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(jwtSettings.Value.AccessTokenExpiryMinutes),
                Issuer = jwtSettings.Value.Issuer,
                Audience = jwtSettings.Value.Audience,
                SigningCredentials = signingCredentials
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public (string PlainToken, RefreshToken RefreshTokenEntity) GenerateRefreshToken(string userId)
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            var plainToken = Convert.ToBase64String(randomBytes);
            var hashedToken = HashToken(plainToken);

            var expiresAt = DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpiryInDays);

            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Token = hashedToken, // Store the hashed token
                ExpiresAt = expiresAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            return (plainToken, refreshTokenEntity);
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            return await refreshTokenRepository.GetByTokenAsync(token);
        }

        public int GetAccessTokenExpiryInSeconds()
        {
            return jwtSettings.Value.AccessTokenExpiryMinutes * 60;
        }
        public string GetUserIdFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
        }

        public async Task RevokeRefreshTokenAsync(string token)
        {
            await refreshTokenRepository.RevokeTokenAsync(token);
        }

        public async Task SaveRefreshTokenAsync(RefreshToken refreshToken)
        {
            await refreshTokenRepository.SaveTokenAsync(refreshToken);
        }

        private static string HashToken(string token)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(token);
            var hash = System.Security.Cryptography.SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
