using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Moq;
using NUnit.Framework;
using Restaurant.Application.DTOs.Auth;
using Restaurant.Application.Exceptions;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Entities.Enums;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Tests
{
    public class AuthServiceTests
    {

        private Mock<IUserRepository> _userRepositoryMock = null!;
        private Mock<IEmployeeRepository> _employeeRepoMock = null!;
        private Mock<IPasswordHasher<User>> _passwordHasherMock = null!;
        private Mock<IValidator<RegisterDto>> _validatorMock = null!;
        private AuthService _authService = null!;

        [SetUp]
        public void SetUp()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _employeeRepoMock = new Mock<IEmployeeRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher<User>>();
            _validatorMock = new Mock<IValidator<RegisterDto>>();

            _authService = new AuthService(
                _userRepositoryMock.Object,
                _employeeRepoMock.Object,
                _passwordHasherMock.Object,
                _validatorMock.Object
            );
        }

        [Test]
        public void RegisterUserAsync_EmailAlreadyExists_ThrowsBadRequestException()
        {
            // Arrange
            var dto = GetValidDto();

            _validatorMock.Setup(v => v.ValidateAsync(dto, default))
                          .ReturnsAsync(new ValidationResult());

            _userRepositoryMock.Setup(r => r.DoesEmailExistAsync(dto.Email))
                               .ReturnsAsync(true);

            // Act & Assert
            Assert.ThrowsAsync<BadRequestException>(() => _authService.RegisterUserAsync(dto));
        }

        [Test]
        public async Task RegisterUserAsync_ValidCustomerInput_RegistersSuccessfully()
        {
            // Arrange
            var dto = GetValidDto();
            var email = "user@example.com";

            _validatorMock.Setup(v => v.ValidateAsync(dto, default))
                          .ReturnsAsync(new ValidationResult());

            _userRepositoryMock.Setup(r => r.DoesEmailExistAsync(dto.Email))
                               .ReturnsAsync(false);

            _employeeRepoMock.Setup(e => e.GetWaiterByEmailAsync(dto.Email))
                             .ReturnsAsync((EmployeeInfo?)null); // no employee, so it's a customer

            _passwordHasherMock.Setup(p => p.HashPassword(It.IsAny<User>(), dto.Password))
                               .Returns("hashed-password");

            _userRepositoryMock.Setup(r => r.SignupAsync(It.IsAny<User>()))
                               .ReturnsAsync(email);

            // Act
            var result = await _authService.RegisterUserAsync(dto);

            // Assert
            Assert.That(result, Is.EqualTo($"User with email {email} registered successfully."));
        }

        [Test]
        public async Task RegisterUserAsync_ValidWaiterInput_SetsWaiterRoleAndLocationId()
        {
            // Arrange
            var dto = GetValidDto();
            var employeeInfo = new EmployeeInfo
            {
                Email = dto.Email,
                LocationId = "loc-123"
            };

            _validatorMock.Setup(v => v.ValidateAsync(dto, default))
                          .ReturnsAsync(new ValidationResult());

            _userRepositoryMock.Setup(r => r.DoesEmailExistAsync(dto.Email))
                               .ReturnsAsync(false);

            _employeeRepoMock.Setup(e => e.GetWaiterByEmailAsync(dto.Email))
                             .ReturnsAsync(employeeInfo);

            _passwordHasherMock.Setup(p => p.HashPassword(It.IsAny<User>(), dto.Password))
                               .Returns("hashed-password");

            _userRepositoryMock.Setup(r => r.SignupAsync(It.IsAny<User>()))
                               .ReturnsAsync(dto.Email);

            // Act
            var result = await _authService.RegisterUserAsync(dto);

            // Assert
            Assert.That(result, Is.EqualTo($"User with email {dto.Email} registered successfully."));

            _userRepositoryMock.Verify(r => r.SignupAsync(It.Is<User>(u =>
                u.Role == Role.Waiter &&
                u.LocationId == "loc-123" &&
                u.PasswordHash == "hashed-password"
            )), Times.Once);
        }

        [Test]
        public void RegisterUserAsync_InvalidInput_ThrowsBadRequestException()
        {
            // Arrange
            var dto = GetValidDto();
            var validationResult = new ValidationResult(new[]
            {
                new ValidationFailure("Email", "Email is required")
            });

            _validatorMock.Setup(v => v.ValidateAsync(dto, default))
                          .ReturnsAsync(validationResult);

            // Act & Assert
            Assert.ThrowsAsync<BadRequestException>(() => _authService.RegisterUserAsync(dto));
        }

        private RegisterDto GetValidDto()
        {
            return new RegisterDto
            {
                Email = "user@example.com",
                FirstName = "John",
                LastName = "Doe",
                Password = "Password123!"
            };
        }
    }
}
