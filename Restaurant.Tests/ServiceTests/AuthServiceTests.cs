using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Moq;
using NUnit.Framework;
using Restaurant.Application.DTOs.Auth;
using Restaurant.Application.Exceptions;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Entities.Enums;
using Restaurant.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace Restaurant.Tests
{
    public class AuthServiceTests
    {

        private Mock<IUserRepository> _userRepositoryMock = null!;
        private Mock<IEmployeeRepository> _employeeRepoMock = null!;
        private Mock<IPasswordHasher<User>> _passwordHasherMock = null!;
        private Mock<IValidator<RegisterDto>> _validatorMock = null!;
        private Mock<IValidator<LoginDto>> _validatorLoginMock = null!;
        private Mock<ITokenService> _tokenServiceMock = null!;
        private Mock<ILogger<AuthService>> _loggerMock = null!;
        private AuthService _authService = null!;

        [SetUp]
        public void SetUp()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _employeeRepoMock = new Mock<IEmployeeRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher<User>>();
            _validatorMock = new Mock<IValidator<RegisterDto>>();
            _validatorLoginMock = new Mock<IValidator<LoginDto>>();
            _tokenServiceMock = new Mock<ITokenService>();
            _loggerMock = new Mock<ILogger<AuthService>>();

            _authService = new AuthService(
                _userRepositoryMock.Object,
                _employeeRepoMock.Object,
                _passwordHasherMock.Object,
                _validatorMock.Object,
                _validatorLoginMock.Object,
                _tokenServiceMock.Object,
                _loggerMock.Object
            );
        }

        #region RegisterUserTests
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
        #endregion

        #region LoginTests
        [Test]
        public async Task LoginAsync_ValidCredentials_ReturnsTokenResponseDto()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@example.com", Password = "Password123!" };
            var user = new User
            {
                Id = "user123",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                PasswordHash = "hashedPassword",
                Role = Role.Customer,
                ImgUrl = "https://example.com/image.jpg",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            };
            var validationResult = new ValidationResult();
            var tokenResponse = new TokenResponseDto
            {
                AccessToken = "access_token",
                RefreshToken = "refresh_token",
                ExpiresIn = 3600
            };

            _validatorLoginMock.Setup(v => v.ValidateAsync(loginDto, default))
                .ReturnsAsync(validationResult);
            _userRepositoryMock.Setup(r => r.GetUserByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);
            _passwordHasherMock.Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password))
                .Returns(PasswordVerificationResult.Success);
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(user))
                .Returns("access_token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken(user.Id!))
                .Returns(("refresh_token", new RefreshToken
                {
                    Id = "token123",
                    UserId = user.Id!,
                    Token = "hashed_refresh_token",
                    ExpiresAt = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    IsRevoked = false,
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }));
            _tokenServiceMock.Setup(t => t.GetAccessTokenExpiryInSeconds())
                .Returns(3600);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.AccessToken, Is.EqualTo("access_token"));
            Assert.That(result.RefreshToken, Is.EqualTo("refresh_token"));
            Assert.That(result.ExpiresIn, Is.EqualTo(3600));

            // Verify interactions
            _validatorLoginMock.Verify(v => v.ValidateAsync(loginDto, default), Times.Once);
            _userRepositoryMock.Verify(r => r.GetUserByEmailAsync(loginDto.Email), Times.Once);
            _passwordHasherMock.Verify(h => h.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password), Times.Once);
            _tokenServiceMock.Verify(t => t.GenerateAccessToken(user), Times.Once);
            _tokenServiceMock.Verify(t => t.GenerateRefreshToken(user.Id!), Times.Once);
            _tokenServiceMock.Verify(t => t.SaveRefreshTokenAsync(It.IsAny<RefreshToken>()), Times.Once);
        }


        [Test]
        public void LoginAsync_InvalidRequest_ThrowsBadRequestException()
        {
            // Arrange  
            var loginDto = new LoginDto { Email = "invalid", Password = "" };
            var validationFailures = new ValidationResult(new[]
            {
               new ValidationFailure("Email", "Invalid email format"),
               new ValidationFailure("Password", "Password is required")
           });

            _validatorLoginMock.Setup(v => v.ValidateAsync(loginDto, default))
                .ReturnsAsync(validationFailures);

            // Act & Assert  
            var exception = Assert.ThrowsAsync<BadRequestException>(async () =>
                await _authService.LoginAsync(loginDto));

            Assert.That(exception!.Message, Is.EqualTo("Invalid Request"));
            Assert.That(exception?.ValidationErrors?.SelectMany(e => e.Value),
                Is.EquivalentTo(validationFailures.Errors.Select(e => e.ErrorMessage)));

            _validatorLoginMock.Verify(v => v.ValidateAsync(loginDto, default), Times.Once);
            _userRepositoryMock.Verify(r => r.GetUserByEmailAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void LoginAsync_UserNotFound_ThrowsUnauthorizedException()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "nonexistent@example.com", Password = "Password123!" };
            var validationResult = new ValidationResult();

            _validatorLoginMock.Setup(v => v.ValidateAsync(loginDto, default))
                .ReturnsAsync(validationResult);
            _userRepositoryMock.Setup(r => r.GetUserByEmailAsync(loginDto.Email))
                .ReturnsAsync((User?)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<UnauthorizedException>(async () =>
                await _authService.LoginAsync(loginDto));


            Assert.That(exception!.Message, Is.EqualTo("Invalid email or password."));

            _validatorLoginMock.Verify(v => v.ValidateAsync(loginDto, default), Times.Once);
            _userRepositoryMock.Verify(r => r.GetUserByEmailAsync(loginDto.Email), Times.Once);
            _passwordHasherMock.Verify(h => h.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void LoginAsync_IncorrectPassword_ThrowsUnauthorizedException()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@example.com", Password = "WrongPassword" };
            var user = new User
            {
                Id = "user123",
                FirstName = "UserFirstName",
                LastName = "UserLastName",
                ImgUrl = "https://example.com/image.jpg",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Email = "test@example.com",
                PasswordHash = "hashedPassword"
            };
            var validationResult = new ValidationResult();

            _validatorLoginMock.Setup(v => v.ValidateAsync(loginDto, default))
                .ReturnsAsync(validationResult);
            _userRepositoryMock.Setup(r => r.GetUserByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);
            _passwordHasherMock.Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password))
                .Returns(PasswordVerificationResult.Failed);

            // Act & Assert
            var exception = Assert.ThrowsAsync<UnauthorizedException>(async () =>
                await _authService.LoginAsync(loginDto));

            Assert.That(exception!.Message, Is.EqualTo("Invalid email or password."));

            _validatorLoginMock.Verify(v => v.ValidateAsync(loginDto, default), Times.Once);
            _userRepositoryMock.Verify(r => r.GetUserByEmailAsync(loginDto.Email), Times.Once);
            _passwordHasherMock.Verify(h => h.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password), Times.Once);
            _tokenServiceMock.Verify(t => t.GenerateAccessToken(It.IsAny<User>()), Times.Never);
        }
        #endregion

        #region RefreshTokenTests
        [Test]
        public async Task RefreshTokenAsync_ValidToken_ReturnsNewTokens()
        {
            // Arrange
            var refreshToken = "valid_refresh_token";
            var hashedToken = HashToken(refreshToken);
            var storedRefreshToken = new RefreshToken
            {
                Id = "token123",
                UserId = "user123",
                Token = hashedToken,
                ExpiresAt = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow.AddDays(-2).ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
            var user = new User
            {
                Id = "user123",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Role = Role.Customer,
                ImgUrl = "https://example.com/image.jpg",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            };
            var tokenResponse = new TokenResponseDto
            {
                AccessToken = "new_access_token",
                RefreshToken = "new_refresh_token",
                ExpiresIn = 3600
            };

            // Mock token service behavior
            _tokenServiceMock.Setup(t => t.GetRefreshTokenAsync(hashedToken))
                .ReturnsAsync(storedRefreshToken);
            _userRepositoryMock.Setup(r => r.GetUserByIdAsync(storedRefreshToken.UserId))
                .ReturnsAsync(user);
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(user))
                .Returns("new_access_token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken(user.Id!))
                .Returns(("new_refresh_token", new RefreshToken
                {
                    Id = "newtoken123",
                    UserId = user.Id!,
                    Token = "hashed_new_refresh_token",
                    ExpiresAt = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    IsRevoked = false,
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }));
            _tokenServiceMock.Setup(t => t.GetAccessTokenExpiryInSeconds())
                .Returns(3600);

            // Act
            var result = await _authService.RefreshTokenAsync(refreshToken);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.AccessToken, Is.EqualTo("new_access_token"));
            Assert.That(result.RefreshToken, Is.EqualTo("new_refresh_token"));
            Assert.That(result.ExpiresIn, Is.EqualTo(3600));

            // Verify interactions
            _tokenServiceMock.Verify(t => t.GetRefreshTokenAsync(hashedToken), Times.Once);
            _userRepositoryMock.Verify(r => r.GetUserByIdAsync(storedRefreshToken.UserId), Times.Once);
            _tokenServiceMock.Verify(t => t.RevokeRefreshTokenAsync(hashedToken), Times.Once);
            _tokenServiceMock.Verify(t => t.GenerateAccessToken(user), Times.Once);
            _tokenServiceMock.Verify(t => t.GenerateRefreshToken(user.Id!), Times.Once);
            _tokenServiceMock.Verify(t => t.SaveRefreshTokenAsync(It.IsAny<RefreshToken>()), Times.Once);
        }

        [Test]
        public void RefreshTokenAsync_EmptyToken_ThrowsBadRequestException()
        {
            // Act & Assert
            var exception = Assert.ThrowsAsync<BadRequestException>(async () =>
                await _authService.RefreshTokenAsync(string.Empty));

            Assert.That(exception!.Message, Is.EqualTo("Refresh token is required."));

            _tokenServiceMock.Verify(t => t.GetRefreshTokenAsync(It.IsAny<string>()), Times.Never);
        }

        public void RefreshTokenAsync_InvalidToken_ThrowsUnauthorizedException()
        {
            // Arrange
            var refreshToken = "invalid_refresh_token";
            var hashedToken = HashToken(refreshToken);

            _tokenServiceMock.Setup(t => t.GetRefreshTokenAsync(hashedToken))
                .ReturnsAsync((RefreshToken?)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<UnauthorizedException>(async () =>
                await _authService.RefreshTokenAsync(refreshToken));


            Assert.That(exception!.Message, Is.EqualTo("Invalid refresh token."));

            _tokenServiceMock.Verify(t => t.GetRefreshTokenAsync(hashedToken), Times.Once);
            _userRepositoryMock.Verify(r => r.GetUserByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void RefreshTokenAsync_RevokedToken_ThrowsUnauthorizedException()
        {
            // Arrange
            var refreshToken = "revoked_refresh_token";
            var hashedToken = HashToken(refreshToken);
            var storedRefreshToken = new RefreshToken
            {
                Id = "token123",
                UserId = "user123",
                Token = hashedToken,
                ExpiresAt = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                IsRevoked = true,
                CreatedAt = DateTime.UtcNow.AddDays(-2).ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            _tokenServiceMock.Setup(t => t.GetRefreshTokenAsync(hashedToken))
                .ReturnsAsync(storedRefreshToken);

            // Act & Assert
            var exception = Assert.ThrowsAsync<UnauthorizedException>(async () =>
                await _authService.RefreshTokenAsync(refreshToken));


            Assert.That(exception!.Message, Is.EqualTo("Refresh token has been revoked."));

            _tokenServiceMock.Verify(t => t.GetRefreshTokenAsync(hashedToken), Times.Once);
            _userRepositoryMock.Verify(r => r.GetUserByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void RefreshTokenAsync_ExpiredToken_ThrowsUnauthorizedException()
        {
            // Arrange
            var refreshToken = "expired_refresh_token";
            var hashedToken = HashToken(refreshToken);
            var storedRefreshToken = new RefreshToken
            {
                Id = "token123",
                UserId = "user123",
                Token = hashedToken,
                ExpiresAt = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow.AddDays(-8).ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            _tokenServiceMock.Setup(t => t.GetRefreshTokenAsync(hashedToken))
                .ReturnsAsync(storedRefreshToken);

            // Act & Assert
            var exception = Assert.ThrowsAsync<UnauthorizedException>(async () =>
                await _authService.RefreshTokenAsync(refreshToken));

            Assert.That(exception!.Message, Is.EqualTo("Refresh token has expired."));

            _tokenServiceMock.Verify(t => t.GetRefreshTokenAsync(hashedToken), Times.Once);
            _userRepositoryMock.Verify(r => r.GetUserByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void RefreshTokenAsync_UserNotFound_ThrowsUnauthorizedException()
        {
            // Arrange
            var refreshToken = "valid_refresh_token";
            var hashedToken = HashToken(refreshToken);
            var storedRefreshToken = new RefreshToken
            {
                Id = "token123",
                UserId = "user123",
                Token = hashedToken,
                ExpiresAt = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow.AddDays(-2).ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            _tokenServiceMock.Setup(t => t.GetRefreshTokenAsync(hashedToken))
                .ReturnsAsync(storedRefreshToken);
            // Update the following line to explicitly handle nullability differences
             _userRepositoryMock.Setup(r => r.GetUserByIdAsync(storedRefreshToken.UserId)).ReturnsAsync((User?)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<UnauthorizedException>(async () =>
                await _authService.RefreshTokenAsync(refreshToken));

            Assert.That(exception!.Message, Is.EqualTo("User not found."));

            _tokenServiceMock.Verify(t => t.GetRefreshTokenAsync(hashedToken), Times.Once);
            _userRepositoryMock.Verify(r => r.GetUserByIdAsync(storedRefreshToken.UserId), Times.Once);
            _tokenServiceMock.Verify(t => t.RevokeRefreshTokenAsync(It.IsAny<string>()), Times.Never);
        }
        #endregion

        #region SignOutTests
         [Test]
        public async Task SignOutAsync_WithValidRefreshToken_RevokesToken()
        {
            // Arrange
            var refreshToken = "valid_refresh_token";
            var hashedToken = HashToken(refreshToken);
            var storedRefreshToken = new RefreshToken
            {
                Id = "token123",
                UserId = "user123",
                Token = hashedToken,
                ExpiresAt = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow.AddDays(-2).ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            _tokenServiceMock.Setup(t => t.GetRefreshTokenAsync(hashedToken))
                .ReturnsAsync(storedRefreshToken);

            _tokenServiceMock.Setup(t => t.RevokeRefreshTokenAsync(hashedToken))
                .Returns(Task.CompletedTask);

            // Act
            await _authService.SignOutAsync(refreshToken);

            // Assert
            _tokenServiceMock.Verify(t => t.GetRefreshTokenAsync(hashedToken), Times.Once);
            _tokenServiceMock.Verify(t => t.RevokeRefreshTokenAsync(hashedToken), Times.Once);
            VerifyLogInformation("User refresh token successfully revoked");
        }

        [Test]
        public void SignOutAsync_WithEmptyRefreshToken_ThrowsBadRequestException()
        {
            // Arrange
            string emptyToken = string.Empty;

            // Act & Assert
            var exception = Assert.ThrowsAsync<BadRequestException>(async () =>
                await _authService.SignOutAsync(emptyToken));

            Assert.That(exception!.Message, Is.EqualTo("Refresh token is required."));
            _tokenServiceMock.Verify(t => t.GetRefreshTokenAsync(It.IsAny<string>()), Times.Never);
            _tokenServiceMock.Verify(t => t.RevokeRefreshTokenAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task SignOutAsync_WithNonExistentRefreshToken_LogsAndDoesNotThrow()
        {
            // Arrange
            var refreshToken = "non_existent_refresh_token";
            var hashedToken = HashToken(refreshToken);

            _tokenServiceMock.Setup(t => t.GetRefreshTokenAsync(hashedToken))
                .ReturnsAsync((RefreshToken?)null);

            // Act - This should not throw
            await _authService.SignOutAsync(refreshToken);

            // Assert
            _tokenServiceMock.Verify(t => t.GetRefreshTokenAsync(hashedToken), Times.Once);
            _tokenServiceMock.Verify(t => t.RevokeRefreshTokenAsync(It.IsAny<string>()), Times.Never);
            VerifyLogInformation("SignOut called with non-existent refresh token");
        }

        [Test]
        public async Task SignOutAsync_WithAlreadyRevokedToken_StillCallsRevokeMethod()
        {
            // Arrange
            var refreshToken = "already_revoked_token";
            var hashedToken = HashToken(refreshToken);
            var storedRefreshToken = new RefreshToken
            {
                Id = "token123",
                UserId = "user123",
                Token = hashedToken,
                ExpiresAt = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                IsRevoked = true, // Already revoked
                CreatedAt = DateTime.UtcNow.AddDays(-2).ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            _tokenServiceMock.Setup(t => t.GetRefreshTokenAsync(hashedToken))
                .ReturnsAsync(storedRefreshToken);

            _tokenServiceMock.Setup(t => t.RevokeRefreshTokenAsync(hashedToken))
                .Returns(Task.CompletedTask);

            // Act
            await _authService.SignOutAsync(refreshToken);

            // Assert
            _tokenServiceMock.Verify(t => t.GetRefreshTokenAsync(hashedToken), Times.Once);
            _tokenServiceMock.Verify(t => t.RevokeRefreshTokenAsync(hashedToken), Times.Once);
            VerifyLogInformation("User refresh token successfully revoked");
        }

        [Test]
        public async Task SignOutAsync_WithExpiredToken_StillRevokesToken()
        {
            // Arrange
            var refreshToken = "expired_token";
            var hashedToken = HashToken(refreshToken);
            var storedRefreshToken = new RefreshToken
            {
                Id = "token123",
                UserId = "user123",
                Token = hashedToken,
                ExpiresAt = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ"), // Expired
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow.AddDays(-8).ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            _tokenServiceMock.Setup(t => t.GetRefreshTokenAsync(hashedToken))
                .ReturnsAsync(storedRefreshToken);

            _tokenServiceMock.Setup(t => t.RevokeRefreshTokenAsync(hashedToken))
                .Returns(Task.CompletedTask);

            // Act
            await _authService.SignOutAsync(refreshToken);

            // Assert
            _tokenServiceMock.Verify(t => t.GetRefreshTokenAsync(hashedToken), Times.Once);
            _tokenServiceMock.Verify(t => t.RevokeRefreshTokenAsync(hashedToken), Times.Once);
            VerifyLogInformation("User refresh token successfully revoked");
        }

        private void VerifyLogInformation(string expectedMessage)
        {
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        #endregion

        private static string HashToken(string token)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(token);
            var hash = System.Security.Cryptography.SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }
        
    }
}
