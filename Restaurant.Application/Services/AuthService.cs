using Microsoft.AspNetCore.Identity;
using Restaurant.Application.DTOs.Auth;
using Restaurant.Application.Exceptions;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;
using FluentValidation;
using Restaurant.Domain.Entities.Enums;

namespace Restaurant.Application.Services
{
    public class AuthService(
            IUserRepository userRepository,
            IEmployeeRepository employeeInfoRepository,
            IPasswordHasher<User> passwordHasher,
            IValidator<RegisterDto> registerValidator) : IAuthService
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
    }
}
