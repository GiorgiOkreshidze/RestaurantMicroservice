using Amazon.DynamoDBv2.Model;
using AutoMapper;
using Moq;
using NUnit.Framework;
using Restaurant.Application.DTOs.Reservations;
using Restaurant.Application.DTOs.Tables;
using Restaurant.Application.DTOs.Users;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Tests.ServiceTests;

public class ReservationServiceTests
{
    private Mock<IReservationRepository> _reservationRepositoryMock = null!;
    private Mock<ILocationRepository> _locationRepositoryMock;
    private Mock<IUserRepository> _userRepositoryMock;
    private Mock<ITableRepository> _tableRepositoryMock;
    private Mock<IWaiterRepository> _waiterRepositoryMock;
    private IReservationService _reservationService = null!;
    private IMapper _mapper = null!;
    
    private ClientReservationRequest _request = null!;
    private Location _location = null!;
    private RestaurantTable _table = null!;
    private Reservation _reservation = null!;
    private User _user = null!;
    private string _userId = null!;

    [SetUp]
    public void SetUp()
    {
        _reservationRepositoryMock = new Mock<IReservationRepository>();
        _locationRepositoryMock = new Mock<ILocationRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _tableRepositoryMock = new Mock<ITableRepository>();
        _waiterRepositoryMock = new Mock<IWaiterRepository>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Reservation, ReservationDto>().ReverseMap();
            cfg.CreateMap<RestaurantTable, RestaurantTableDto>().ReverseMap();
            cfg.CreateMap<User, UserDto>().ReverseMap();
        });
        _mapper = config.CreateMapper();

        _reservationService = new ReservationService(
            _reservationRepositoryMock.Object,
            _locationRepositoryMock.Object,
            _userRepositoryMock.Object,
            _tableRepositoryMock.Object,
            _waiterRepositoryMock.Object,
            _mapper);
        
        _request = new ClientReservationRequest
        {
            Id = null,
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            GuestsNumber = "2",
            LocationId = "loc-1",
            TableId = "table-1",
            TimeFrom = "13:30",
            TimeTo = "15:00"
        };
        
        _location = new Location
        {
            Id = "loc-1",
            Address = "Main Street 123",
            ImageUrl = "some url",
            Description = "desc"
        };
        
        _table = new RestaurantTable
        {
            Id = "table-1",
            Capacity = "4",
            TableNumber = "1",
            LocationId = "loc-1",
            LocationAddress = "Main Street 123",
        };

        _reservation = new Reservation
        {
            Id = "res-1",
            Date = _request.Date,
            TimeFrom = "13:30",
            TimeTo = "15:00",
            TableId = _request.TableId,
            LocationAddress = _location.Address,
            UserEmail = "otheruser@example.com",
            GuestsNumber = "1",
            LocationId = _location.Id,
            PreOrder = "not implemented",
            Status = "Reserved",
            TableCapacity = "3",
            TableNumber = "5",
            TimeSlot = "13:30 - 15:00",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), // Different user
        };
        
        _userId = "user-1";
        
        _user = new User
        {
            Id = _userId,
            Email = "user@example.com",
            FirstName = "John",
            LastName = "Doe",
            ImgUrl = "some url",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };
    }

    [Test]
    public async Task UpsertReservationAsync_ValidClientReservation_ReturnsReservationDto()
    {
        // Arrange
        _locationRepositoryMock.Setup(r => r.GetLocationByIdAsync(_request.LocationId)).ReturnsAsync(_location);
        _tableRepositoryMock.Setup(r => r.GetTableById(_request.TableId)).ReturnsAsync(_table);
        _userRepositoryMock.Setup(r => r.GetUserByIdAsync(_userId)).ReturnsAsync(_user);
        _reservationRepositoryMock.Setup(r =>
                r.GetReservationsByDateLocationTable(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Reservation>());
        _reservationRepositoryMock.Setup(r => r.ReservationExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _waiterRepositoryMock.Setup(r => r.GetWaitersByLocationAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<User>
            {
                new()
                {
                    Id = "waiter-1",
                    Email = "email",
                    FirstName = "johny",
                    LastName = "depp",
                    ImgUrl = "url",
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                }
            });
        _reservationRepositoryMock.Setup(r => r.GetWaiterReservationCountAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(0);

        // Act
        var result = await _reservationService.UpsertReservationAsync(_request, _userId);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void UpsertReservationAsync_InvalidTimeSlot_ThrowsArgumentException()
    {
        // Arrange
        _request.TimeFrom = "01:00"; // Outside of predefined slot range
        _request.TimeTo = "02:00";

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(() =>
            _reservationService.UpsertReservationAsync(_request, _userId));
        Assert.That(ex.Message, Does.Contain("Reservation must be within restaurant working hours."));
    }

    [Test]
    public void UpsertReservationAsync_TableTooSmall_ThrowsArgumentException()
    {
        // Arrange
        _request.GuestsNumber = "100"; // Invalid number of guests
        _tableRepositoryMock.Setup(r => r.GetTableById(_request.TableId)).ReturnsAsync(_table);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(() =>
            _reservationService.UpsertReservationAsync(_request, "user-id"));
        Assert.That(ex.Message,
            Does.Contain("Table with ID table-1 cannot accommodate 100 guests. Maximum capacity: 4."));
    }

    [Test]
    public async Task UpsertReservationAsync_ConflictingReservationDifferentUser_ThrowsArgumentException()
    {
        // Arrange
        _locationRepositoryMock.Setup(r => r.GetLocationByIdAsync(_request.LocationId)).ReturnsAsync(_location);
        _tableRepositoryMock.Setup(r => r.GetTableById(_request.TableId)).ReturnsAsync(_table);
        _userRepositoryMock.Setup(r => r.GetUserByIdAsync(_userId)).ReturnsAsync(_user);
        _reservationRepositoryMock.Setup(r =>
                r.GetReservationsByDateLocationTable(_request.Date, _location.Address, _request.TableId))
            .ReturnsAsync(new List<Reservation> { _reservation });
        _reservationRepositoryMock.Setup(r => r.ReservationExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _waiterRepositoryMock.Setup(r => r.GetWaitersByLocationAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<User>
            {
                new()
                {
                    Id = "waiter-1",
                    Email = "email",
                    FirstName = "johny",
                    LastName = "depp",
                    ImgUrl = "url",
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                }
            });
        _reservationRepositoryMock.Setup(r => r.GetWaiterReservationCountAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(0);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(() =>
            _reservationService.UpsertReservationAsync(_request, _userId));
        Assert.That(ex.Message, Is.EqualTo(
            $"Reservation #{_request.Id} at location {_location.Address} is already booked during the requested time period."));
    }

    [Test]
    public async Task UpsertReservationAsync_ConflictingReservationSameUser_ThrowsArgumentException()
    {
        // Arrange
        _locationRepositoryMock.Setup(r => r.GetLocationByIdAsync(_request.LocationId)).ReturnsAsync(_location);
        _tableRepositoryMock.Setup(r => r.GetTableById(_request.TableId)).ReturnsAsync(_table);
        _userRepositoryMock.Setup(r => r.GetUserByIdAsync(_userId)).ReturnsAsync(_user);
        _reservationRepositoryMock.Setup(r =>
                r.GetReservationsByDateLocationTable(_request.Date, _location.Address, _request.TableId))
            .ReturnsAsync(new List<Reservation> { _reservation });
        _reservationRepositoryMock.Setup(r => r.ReservationExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _waiterRepositoryMock.Setup(r => r.GetWaitersByLocationAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<User>
            {
                new()
                {
                    Id = "waiter-1",
                    Email = "email",
                    FirstName = "johny",
                    LastName = "depp",
                    ImgUrl = "url",
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                }
            });
        _reservationRepositoryMock.Setup(r => r.GetWaiterReservationCountAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(0);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(() =>
            _reservationService.UpsertReservationAsync(_request, _userId));
        Assert.That(ex.Message, Is.EqualTo(
            $"Reservation #{_request.Id} at location {_location.Address} is already booked during the requested time period."));
    }

    [Test]
    public async Task UpsertReservationAsync_NoWaitersAvailable_ThrowsResourceNotFoundException()
    {
        // Arrange
        _locationRepositoryMock.Setup(r => r.GetLocationByIdAsync(_request.LocationId)).ReturnsAsync(_location);
        _tableRepositoryMock.Setup(r => r.GetTableById(_request.TableId)).ReturnsAsync(_table);
        _userRepositoryMock.Setup(r => r.GetUserByIdAsync(_userId)).ReturnsAsync(_user);
        _reservationRepositoryMock.Setup(r =>
                r.GetReservationsByDateLocationTable(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Reservation>());
        _reservationRepositoryMock.Setup(r => r.ReservationExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        // Setup no waiters available
        _waiterRepositoryMock.Setup(r => r.GetWaitersByLocationAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<User>());

        // Act & Assert
        var ex = Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _reservationService.UpsertReservationAsync(_request, _userId));
        Assert.That(ex.Message,
            Is.EqualTo($"No waiters available for location ID: {_request.LocationId} after counting reservations"));
    }
}