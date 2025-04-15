using Microsoft.AspNetCore.Identity;
using Restaurant.Application.DTOs.Auth;
using Restaurant.Application.Exceptions;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;
using FluentValidation;
using Restaurant.Domain.Entities.Enums;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Restaurant.Application.Services
{
    public class AuthService(
            IUserRepository userRepository,
            IEmployeeRepository employeeInfoRepository,
            IPasswordHasher<User> passwordHasher,
            IValidator<RegisterDto> registerValidator,
            IValidator<LoginDto> loginValidator,
            ITokenService tokenService,
            ILogger<AuthService> _logger) : IAuthService
    {
        private const string DefaultUserImageUrl = "https://team2-demo-bucket.s3.eu-west-2.amazonaws.com/Images/Users/default_user.jpg";

        public async Task<string> RegisterUserAsync(RegisterDto request)
        {
            await ValidateRequestAsync(request, registerValidator);

            if (await userRepository.DoesEmailExistAsync(request.Email))
            {
                throw new BadRequestException("A User with the same email already exists.");
            }

            var user = await CreateUserAsync(request);
            var registeredUserEmail = await userRepository.SignupAsync(user);

            return $"User with email {registeredUserEmail} registered successfully.";
        }

        public async Task<TokenResponseDto> LoginAsync(LoginDto request)
        {
            await ValidateRequestAsync(request, loginValidator);

            var user = await userRepository.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                throw new UnauthorizedException("Invalid email or password.");
            }

            var passwordVerificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                throw new UnauthorizedException("Invalid email or password.");
            }

            return await GenerateTokensAsync(user);
        }

        public async Task<TokenResponseDto> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                throw new BadRequestException("Refresh token is required.");
            }
            var hashedToken = HashToken(refreshToken); // Hash the incoming token
            var existingToken = await tokenService.GetRefreshTokenAsync(hashedToken);
            if (existingToken == null)
            {
                throw new UnauthorizedException("Invalid refresh token.");
            }

            if (existingToken.IsRevoked)
            {
                throw new UnauthorizedException("Refresh token has been revoked.");
            }
            var expirationDate = DateTime.Parse(existingToken.ExpiresAt);

            if (DateTime.Parse(existingToken.ExpiresAt) < DateTime.UtcNow)
            {
                Console.WriteLine($"Refresh token expired. Expiration Date: {expirationDate}, Current UTC Time: {DateTime.UtcNow}");
                throw new UnauthorizedException("Refresh token has expired.");
            }

            var user = await userRepository.GetUserByIdAsync(existingToken.UserId);
            if (user == null)
            {
                throw new UnauthorizedException("User not found.");
            }

            // Revoke the current refresh token
            await tokenService.RevokeRefreshTokenAsync(hashedToken);

            // Generate new tokens
            return await GenerateTokensAsync(user);
        }

        public async Task SignOutAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                throw new BadRequestException("Refresh token is required.");
            }

            var hashedToken = HashToken(refreshToken);
            var existingToken = await tokenService.GetRefreshTokenAsync(hashedToken);

            if (existingToken != null)
            {
                await tokenService.RevokeRefreshTokenAsync(hashedToken);
                _logger.LogInformation("User refresh token successfully revoked");
            }
            else
            {
                _logger.LogInformation("SignOut called with non-existent refresh token");
            }
        }

        private async Task<User> CreateUserAsync(RegisterDto request)
        {
            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                ImgUrl = DefaultUserImageUrl,
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            };

            var employee = await employeeInfoRepository.GetWaiterByEmailAsync(request.Email);

            user.Role = employee != null ? Role.Waiter : Role.Customer;
            user.LocationId = employee?.LocationId;
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

            return user;
        }

        private static async Task ValidateRequestAsync<T>(T request, IValidator<T> validator)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new BadRequestException("Invalid Request", validationResult);
            }
        }

        private async Task<TokenResponseDto> GenerateTokensAsync(User user)
        {
            var accessToken = tokenService.GenerateAccessToken(user);
            var (plainToken, refreshTokenEntity) = tokenService.GenerateRefreshToken(user.Id!);

            int expiresInSeconds = tokenService.GetAccessTokenExpiryInSeconds();

            await tokenService.SaveRefreshTokenAsync(refreshTokenEntity);

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = plainToken, // Return the plain token to the client
                ExpiresIn = expiresInSeconds
            };
        }

        private static string HashToken(string token)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(token);
            var hash = System.Security.Cryptography.SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
