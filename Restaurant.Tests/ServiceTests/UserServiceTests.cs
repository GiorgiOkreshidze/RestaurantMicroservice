using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Moq;
using NUnit.Framework;
using Restaurant.Application.DTOs.Users;
using Restaurant.Application.Exceptions;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;
using Restaurant.Infrastructure.Services;

namespace Restaurant.Tests.ServiceTests;

public class UserServiceTests
{
    private Mock<IUserRepository> _userRepositoryMock = null!;
    private Mock<IImageService> _imageServiceMock = null!;
    private Mock<IValidator<UpdatePasswordRequest>> _updatePasswordValidatorMock = null!;
    private Mock<IValidator<UpdateProfileRequest>> _updateProfileValidatorMock = null!;
    private Mock<IPasswordHasher<User>> _passwordHasherMock = null!;
    private IUserService _userService = null!;
    private IMapper _mapper = null!;
    private User _user = null!;
    private List<User> _users = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _imageServiceMock = new Mock<IImageService>();
        _updatePasswordValidatorMock = new Mock<IValidator<UpdatePasswordRequest>>();
        _updateProfileValidatorMock = new Mock<IValidator<UpdateProfileRequest>>();
        _passwordHasherMock = new Mock<IPasswordHasher<User>>();

        var config = new MapperConfiguration(cfg => { cfg.CreateMap<User, UserDto>().ReverseMap(); });
        _mapper = config.CreateMapper();

        _userService = new UserService(
            _userRepositoryMock.Object, 
            _imageServiceMock.Object,
            _mapper, 
            _updatePasswordValidatorMock.Object,
            _updateProfileValidatorMock.Object,
            _passwordHasherMock.Object);

        _user = new User
        {
            Id = "test-id-1",
            Email = "example@example.com",
            FirstName = "John",
            LastName = "Doe",
            ImgUrl = "http://example.com/image.jpg",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            PasswordHash = "hashed_password_123"
        };

        _users = new List<User>
        {
            new()
            {
                Id = "test-id-1",
                Email = "example@example.com",
                FirstName = "John",
                LastName = "Doe",
                ImgUrl = "http://example.com/image.jpg",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            },
            new()
            {
                Id = "test-id-2",
                Email = "example2@example.com",
                FirstName = "Johny",
                LastName = "Depp",
                ImgUrl = "http://example.com/image.jpg",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            },
        };
    }

    [Test]
    public async Task GetUserByIdAsync_ValidId_ReturnsUserDto()
    {
        // Arrange
        var userId = "test-id-1";

        _userRepositoryMock
            .Setup(repo => repo.GetUserByIdAsync(userId))
            .ReturnsAsync(_user);

        // Act
        var result = await _userService.GetUserByIdAsync(userId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(_user.Id));
        Assert.That(result.FirstName, Is.EqualTo(_user.FirstName));
        Assert.That(result.LastName, Is.EqualTo(_user.LastName));
        Assert.That(result.Email, Is.EqualTo(_user.Email));
        Assert.That(result.ImgUrl, Is.EqualTo(_user.ImgUrl));
    }
    
    [Test]
    public void GetUserByIdAsync_InvalidId_ReturnsNotFoundException()
    {
        // Arrange
        var invalidUserId = "invalid-id";

        _userRepositoryMock
            .Setup(repo => repo.GetUserByIdAsync(invalidUserId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () =>
            await _userService.GetUserByIdAsync(invalidUserId));

        Assert.That(ex?.Message, Is.EqualTo($"The User with the key '{invalidUserId}' was not found."));
    }
    
    [Test]
    public async Task GetAllUsersAsync_ReturnsListOfUsers()
    {
        // Arrange= 
        _userRepositoryMock.Setup(repo => repo.GetAllUsersAsync()).ReturnsAsync(_users);

        // Act
        var result = await _userService.GetAllUsersAsync();

        // Assert
        Assert.That(result.ToList(), Has.Count.EqualTo(2));
    }

    [Test]
    public async Task UpdatePasswordAsync_ValidRequest_UpdatesPasswordSuccessfully()
    {
        // Arrange
        var userId = "test-id-1";
        var updateRequest = new UpdatePasswordRequest
        {
            OldPassword = "oldPassword123",
            NewPassword = "newPassword456"
        };
        
        _userRepositoryMock.Setup(repo => repo.GetUserByIdAsync(userId)).ReturnsAsync(_user);
        _updatePasswordValidatorMock.Setup(v => v.ValidateAsync(updateRequest, default))
            .ReturnsAsync(new ValidationResult());
        _passwordHasherMock.Setup(h => h.VerifyHashedPassword(_user, _user.PasswordHash, updateRequest.OldPassword))
            .Returns(PasswordVerificationResult.Success);
        _passwordHasherMock.Setup(h => h.HashPassword(_user, updateRequest.NewPassword))
            .Returns("new_hashed_password_456");
        _userRepositoryMock.Setup(repo => repo.UpdatePasswordAsync(userId, "new_hashed_password_456"))
            .Returns(Task.CompletedTask);

        // Act
        await _userService.UpdatePasswordAsync(userId, updateRequest);

        // Assert
        _passwordHasherMock.Verify(h => h.HashPassword(_user, updateRequest.NewPassword), Times.Once);
        _userRepositoryMock.Verify(repo => repo.UpdatePasswordAsync(userId, "new_hashed_password_456"), Times.Once);
    }

    [Test]
    public void UpdatePasswordAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = "nonexistent-id";
        var updateRequest = new UpdatePasswordRequest
        {
            OldPassword = "oldPassword123",
            NewPassword = "newPassword456"
        };
        
        _userRepositoryMock.Setup(repo => repo.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);
        _updatePasswordValidatorMock.Setup(v => v.ValidateAsync(updateRequest, default))
            .ReturnsAsync(new ValidationResult());

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () => 
            await _userService.UpdatePasswordAsync(userId, updateRequest));
        
        Assert.That(ex?.Message, Is.EqualTo($"The User with the key '{userId}' was not found."));
    }

    [Test]
    public void UpdatePasswordAsync_InvalidRequest_ThrowsBadRequestException()
    {
        // Arrange
        var userId = "test-id-1";
        var updateRequest = new UpdatePasswordRequest
        {
            OldPassword = "short",
            NewPassword = "123"
        };
        
        var validationFailures = new List<ValidationFailure>
        {
            new("NewPassword", "Password must be at least 6 characters")
        };
        var validationResult = new ValidationResult(validationFailures);
        
        _updatePasswordValidatorMock.Setup(v => v.ValidateAsync(updateRequest, default))
            .ReturnsAsync(validationResult);

        // Act & Assert
        var ex = Assert.ThrowsAsync<BadRequestException>(async () => 
            await _userService.UpdatePasswordAsync(userId, updateRequest));
        
        Assert.That(ex?.Message, Is.EqualTo("Invalid Request"));
    }

    [Test]
    public void UpdatePasswordAsync_IncorrectOldPassword_ThrowsBadRequestException()
    {
        // Arrange
        var userId = "test-id-1";
        var updateRequest = new UpdatePasswordRequest
        {
            OldPassword = "wrongOldPassword",
            NewPassword = "newPassword456"
        };
        
        _userRepositoryMock.Setup(repo => repo.GetUserByIdAsync(userId)).ReturnsAsync(_user);
        _updatePasswordValidatorMock.Setup(v => v.ValidateAsync(updateRequest, default))
            .ReturnsAsync(new ValidationResult());
        _passwordHasherMock.Setup(h => h.VerifyHashedPassword(_user, _user.PasswordHash, updateRequest.OldPassword))
            .Returns(PasswordVerificationResult.Failed);

        // Act & Assert
        var ex = Assert.ThrowsAsync<BadRequestException>(async () => 
            await _userService.UpdatePasswordAsync(userId, updateRequest));
        
        Assert.That(ex?.Message, Is.EqualTo("Verification failed."));
    }

    [Test]
    public async Task UpdateProfileAsync_ValidRequest_UpdatesUserProfile()
    {
        // Arrange
        var userId = "test-id-1";
        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "UpdatedFirstName",
            LastName = "UpdatedLastName"
        };
        
        _userRepositoryMock.Setup(repo => repo.GetUserByIdAsync(userId)).ReturnsAsync(_user);
        _updateProfileValidatorMock.Setup(v => v.ValidateAsync(updateRequest, default))
            .ReturnsAsync(new ValidationResult());
        _userRepositoryMock.Setup(repo => repo.UpdateProfileAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        await _userService.UpdateProfileAsync(userId, updateRequest);

        // Assert
        _userRepositoryMock.Verify(repo => repo.UpdateProfileAsync(It.Is<User>(u => 
            u.FirstName == updateRequest.FirstName && 
            u.LastName == updateRequest.LastName)), 
            Times.Once);
    }

    [Test]
    public void UpdateProfileAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = "nonexistent-id";
        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "UpdatedFirstName",
            LastName = "UpdatedLastName"
        };
        
        _userRepositoryMock.Setup(repo => repo.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);
        _updateProfileValidatorMock.Setup(v => v.ValidateAsync(updateRequest, default))
            .ReturnsAsync(new ValidationResult());

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () => 
            await _userService.UpdateProfileAsync(userId, updateRequest));
        
        Assert.That(ex?.Message, Is.EqualTo($"The User with the key '{userId}' was not found."));
    }

    [Test]
    public void UpdateProfileAsync_InvalidRequest_ThrowsBadRequestException()
    {
        // Arrange
        var userId = "test-id-1";
        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "",
            LastName = "UpdatedLastName"
        };
        
        var validationFailures = new List<ValidationFailure>
        {
            new("FirstName", "First name is required.")
        };
        var validationResult = new ValidationResult(validationFailures);
        
        _updateProfileValidatorMock.Setup(v => v.ValidateAsync(updateRequest, default))
            .ReturnsAsync(validationResult);

        // Act & Assert
        var ex = Assert.ThrowsAsync<BadRequestException>(async () => 
            await _userService.UpdateProfileAsync(userId, updateRequest));
        
        Assert.That(ex?.Message, Is.EqualTo("Invalid Request"));
    }

    [Test]
    public void UpdateProfileAsync_MultipleValidationErrors_ThrowsBadRequestExceptionWithAllErrors()
    {
        // Arrange
        var userId = "test-id-1";
        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "",
            LastName = "123" // Invalid last name with numbers
        };
        
        var validationFailures = new List<ValidationFailure>
        {
            new("FirstName", "First name is required."),
            new("LastName", "Last name can contain only Latin letters, hyphens, and apostrophes.")
        };
        var validationResult = new ValidationResult(validationFailures);
        
        _updateProfileValidatorMock.Setup(v => v.ValidateAsync(updateRequest, default))
            .ReturnsAsync(validationResult);

        // Act & Assert
        var ex = Assert.ThrowsAsync<BadRequestException>(async () => 
            await _userService.UpdateProfileAsync(userId, updateRequest));
        
        Assert.That(ex?.Message, Is.EqualTo("Invalid Request"));
        Assert.That(ex?.ValidationErrors, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task UpdateProfileAsync_PreservesOriginalImageUrl_WhenImageNotProvided()
    {
        // Arrange
        var userId = "test-id-1";
        var originalImgUrl = "http://example.com/image.jpg";
        var user = new User
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Email = "example@example.com",
            ImgUrl = originalImgUrl,
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        
        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "UpdatedFirstName",
            LastName = "UpdatedLastName",
            Base64EncodedImage = null // No image provided
        };
        
        _userRepositoryMock.Setup(repo => repo.GetUserByIdAsync(userId)).ReturnsAsync(user);
        _updateProfileValidatorMock.Setup(v => v.ValidateAsync(updateRequest, default))
            .ReturnsAsync(new ValidationResult());
        
        User capturedUser = null;
        _userRepositoryMock.Setup(repo => repo.UpdateProfileAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .Returns(Task.CompletedTask);

        // Act
        await _userService.UpdateProfileAsync(userId, updateRequest);

        // Assert
        Assert.That(capturedUser, Is.Not.Null);
        Assert.That(capturedUser.FirstName, Is.EqualTo(updateRequest.FirstName));
        Assert.That(capturedUser.LastName, Is.EqualTo(updateRequest.LastName));
        Assert.That(capturedUser.ImgUrl, Is.EqualTo(originalImgUrl), "Original image URL should be preserved");
    }
}
