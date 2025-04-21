using AutoMapper;
using Moq;
using NUnit.Framework;
using Restaurant.Application.DTOs.Users;
using Restaurant.Application.Exceptions;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Tests.ServiceTests;

public class UserServiceTests
{
    private Mock<IUserRepository> _userRepositoryMock = null!;
    private IUserService _userService = null!;
    private IMapper _mapper = null!;
    private User _user = null!;
    private List<User> _users = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepositoryMock = new Mock<IUserRepository>();

        var config = new MapperConfiguration(cfg => { cfg.CreateMap<User, UserDto>().ReverseMap(); });
        _mapper = config.CreateMapper();

        _userService = new UserService(_userRepositoryMock.Object, _mapper);

        _user = new User
        {
            Email = "example@example.com",
            FirstName = "John",
            LastName = "Doe",
            ImgUrl = "http://example.com/image.jpg",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };

        _users = new List<User>
        {
            new()
            {
                Email = "example@example.com",
                FirstName = "John",
                LastName = "Doe",
                ImgUrl = "http://example.com/image.jpg",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            },
            new()
            {
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
}