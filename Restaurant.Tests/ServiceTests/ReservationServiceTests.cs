using System.Globalization;
using Amazon.DynamoDBv2.Model;
using AutoMapper;
using FluentValidation;
using Moq;
using NUnit.Framework;
using Restaurant.Application.DTOs.Reservations;
using Restaurant.Application.DTOs.Tables;
using Restaurant.Application.DTOs.Users;
using Restaurant.Application.Exceptions;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Entities.Enums;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Tests.ServiceTests;

public class ReservationServiceTests
{
    private Mock<IReservationRepository> _reservationRepositoryMock = null!;
    private Mock<ILocationRepository> _locationRepositoryMock = null!;
    private Mock<IUserRepository> _userRepositoryMock = null!;
    private Mock<ITableRepository> _tableRepositoryMock = null!;
    private Mock<IWaiterRepository> _waiterRepositoryMock = null!;
    private IReservationService _reservationService = null!;
    private Mock<IValidator<FilterParameters>> _validatorFilterMock = null!;
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
        _validatorFilterMock = new Mock<IValidator<FilterParameters>>();
        
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Reservation, ReservationDto>().ReverseMap();
            cfg.CreateMap<RestaurantTable, RestaurantTableDto>().ReverseMap();
            cfg.CreateMap<Reservation, ClientReservationResponse>().ReverseMap();
            cfg.CreateMap<User, UserDto>().ReverseMap();
        });
        _mapper = config.CreateMapper();

        _reservationService = new ReservationService(
            _reservationRepositoryMock.Object,
            _locationRepositoryMock.Object,
            _userRepositoryMock.Object,
            _tableRepositoryMock.Object,
            _waiterRepositoryMock.Object,
            _validatorFilterMock.Object,
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
            Capacity = 4,
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

    #region PostReservation
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
    public void UpsertReservationAsync_InvalidTimeSlot_ThrowsConflictException()
    {
        // Arrange
        _request.TimeFrom = "01:00"; // Outside of predefined slot range
        _request.TimeTo = "02:00";

        // Act & Assert
        var ex = Assert.ThrowsAsync<ConflictException>(() =>
            _reservationService.UpsertReservationAsync(_request, _userId));
        Assert.That(ex?.Message, Does.Contain("Reservation must be within restaurant working hours."));
    }

    [Test]
    public void UpsertReservationAsync_TableTooSmall_ThrowsConflictException()
    {
        // Arrange
        _request.GuestsNumber = "100"; // Invalid number of guests
        _locationRepositoryMock.Setup(r => r.GetLocationByIdAsync(_request.LocationId)).ReturnsAsync(_location);
        _tableRepositoryMock.Setup(r => r.GetTableById(_request.TableId)).ReturnsAsync(_table);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ConflictException>(() =>
            _reservationService.UpsertReservationAsync(_request, "user-id"));
        Assert.That(ex?.Message,
            Does.Contain("Table with ID table-1 cannot accommodate 100 guests. Maximum capacity: 4."));
    }

    [Test]
    public void UpsertReservationAsync_ConflictingReservationDifferentUser_ThrowsConflictException()
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
        var ex = Assert.ThrowsAsync<ConflictException>(() =>
            _reservationService.UpsertReservationAsync(_request, _userId));
        Assert.That(ex?.Message, Is.EqualTo(
            $"Reservation #{_request.Id} at location {_location.Address} is already booked during the requested time period."));
    }

    [Test]
    public void UpsertReservationAsync_ConflictingReservationSameUser_ThrowsConflictException()
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
        var ex = Assert.ThrowsAsync<ConflictException>(() =>
            _reservationService.UpsertReservationAsync(_request, _userId));
        Assert.That(ex?.Message, Is.EqualTo(
            $"Reservation #{_request.Id} at location {_location.Address} is already booked during the requested time period."));
    }

    [Test]
    public void UpsertReservationAsync_NoWaitersAvailable_ThrowsNotFoundException()
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
        var ex = Assert.ThrowsAsync<NotFoundException>(() =>
            _reservationService.UpsertReservationAsync(_request, _userId));
        Assert.That(ex?.Message,
            Is.EqualTo($"No waiters available for location ID: {_request.LocationId} after counting reservations"));
    }
    
   [Test]
    public async Task ProcessWaiterReservation_CustomerReservation_SuccessfullyReturnsResponse()
    {
        // Arrange
        var request = new WaiterReservationRequest
        {
            ClientType = ClientType.CUSTOMER,
            CustomerId = "customer-1",
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            TimeFrom = "13:30",
            TimeTo = "15:00",
            TableId = "table-1",
            LocationId = "loc-1",
            GuestsNumber = "1"
        };
        
        _reservation.TimeFrom = request.TimeFrom;
        _reservation.TimeTo = request.TimeTo;
        _reservation.TableId = request.TableId;
        
        var waiter = new User
        {
            Id = "waiter-1",
            Email = "waiter@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            LocationId = "loc-1",
            ImgUrl = "null",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };
        var customer = new User
        {
            Id = "customer-1",
            Email = "customer@example.com",
            FirstName = "John",
            LastName = "Smith",            
            ImgUrl = "null",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            
        };
        var waiterId = "waiter-1";
        var locationId = "loc-1";

        _userRepositoryMock.Setup(r => r.GetUserByIdAsync(waiterId)).ReturnsAsync(waiter);
        _userRepositoryMock.Setup(r => r.GetUserByIdAsync(request.CustomerId)).ReturnsAsync(customer);
        _reservationRepositoryMock.Setup(r => r.ReservationExistsAsync(_reservation.Id)).ReturnsAsync(false);
        _reservationRepositoryMock.Setup(r => r.GetReservationsByDateLocationTable(
            request.Date, _reservation.LocationAddress, request.TableId))
            .ReturnsAsync(new List<Reservation>());
        
        // Use reflection to access private method
        var methodInfo = typeof(ReservationService).GetMethod("ProcessWaiterReservation", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = (Task<ClientReservationResponse>)methodInfo.Invoke(_reservationService,
            [request, _reservation, waiterId, locationId]);

        // Act
        var result = await task;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserEmail, Is.EqualTo(customer.Email));
        Assert.That(result.UserInfo, Is.EqualTo($"Customer {customer.FirstName} {customer.LastName}"));
        Assert.That(result.WaiterId, Is.EqualTo(waiterId));
        Assert.That(result.ClientType, Is.EqualTo(ClientType.CUSTOMER));
    }
    
    [Test]
    public async Task ProcessWaiterReservation_VisitorReservation_SuccessfullyReturnsResponse()
    {
        // Arrange
        var request = new WaiterReservationRequest
        {
            ClientType = ClientType.VISITOR,
            CustomerId = null,
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            TimeFrom = "13:30",
            TimeTo = "15:00",
            TableId = "table-1",
            LocationId = "loc-1",
            GuestsNumber = "1"
        };

        var waiter = new User
        {
            Id = "waiter-1",
            Email = "waiter@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            LocationId = "loc-1",
            ImgUrl = "null",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };
        var waiterId = "waiter-1";
        var locationId = "loc-1";

        _userRepositoryMock.Setup(r => r.GetUserByIdAsync(waiterId)).ReturnsAsync(waiter);
        _reservationRepositoryMock.Setup(r => r.ReservationExistsAsync(_reservation.Id)).ReturnsAsync(false);
        _reservationRepositoryMock.Setup(r => r.GetReservationsByDateLocationTable(
            request.Date, _reservation.LocationAddress, request.TableId))
            .ReturnsAsync(new List<Reservation>());

        // Use reflection to access private method
        var methodInfo = typeof(ReservationService).GetMethod("ProcessWaiterReservation", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = (Task<ClientReservationResponse>)methodInfo.Invoke(_reservationService,
            [request, _reservation, waiterId, locationId]);

        // Act
        var result = await task;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserEmail, Is.EqualTo(waiter.Email));
        Assert.That(result.UserInfo, Is.EqualTo($"Waiter {waiter.FirstName} {waiter.LastName} (Visitor)"));
        Assert.That(result.WaiterId, Is.EqualTo(waiterId));
        Assert.That(result.ClientType, Is.EqualTo(ClientType.VISITOR));
    }

    #endregion

    #region GetAvailableTables
    [Test]
    public async Task GetAvailableTablesAsync_ValidParameters_ReturnsAvailableTables()
    {
        // Arrange
        var filterParameters = new FilterParameters
        {
            LocationId = "loc-1",
            Date = "2025-04-16",
            Guests = 2
        };

        var validationResult = new FluentValidation.Results.ValidationResult();
        _validatorFilterMock.Setup(v => v.ValidateAsync(filterParameters, default))
            .ReturnsAsync(validationResult);

        _locationRepositoryMock.Setup(r => r.GetLocationByIdAsync(filterParameters.LocationId))
            .ReturnsAsync(_location);

        var tables = new List<RestaurantTable> { _table };
        _tableRepositoryMock.Setup(r => r.GetTablesForLocationAsync(_location.Id, filterParameters.Guests))
            .ReturnsAsync(tables);

        var reservations = new List<Reservation>();
        _reservationRepositoryMock.Setup(r => r.GetReservationsForDateAndLocation(filterParameters.Date, _location.Id))
            .ReturnsAsync(reservations);

        // Act
        var result = await _reservationService.GetAvailableTablesAsync(filterParameters);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(1));
        var tableDto = result.First();
        Assert.That(tableDto.TableId, Is.EqualTo(_table.Id));
        Assert.That(tableDto.AvailableSlots.Count, Is.GreaterThan(0));
    }

    [Test]
    public void GetAvailableTablesAsync_InvalidParameters_ThrowsBadRequestException()
    {
        // Arrange
        var filterParameters = new FilterParameters
        {
            LocationId = "loc-1",
            Date = "invalid-date", // Invalid date format
            Guests = 2
        };

        var validationFailures = new List<FluentValidation.Results.ValidationFailure>
    {
        new("Date", "Date must be in format yyyy-MM-dd")
    };
        var validationResult = new FluentValidation.Results.ValidationResult(validationFailures);

        _validatorFilterMock.Setup(v => v.ValidateAsync(filterParameters, default))
            .ReturnsAsync(validationResult);

        // Act & Assert
        var exception = Assert.ThrowsAsync<BadRequestException>(async () =>
            await _reservationService.GetAvailableTablesAsync(filterParameters));

        Assert.That(exception!.Message, Is.EqualTo("Invalid Request"));
    }

    [Test]
    public async Task GetAvailableTablesAsync_WithConflictingReservations_FiltersOutUnavailableSlots()
    {
        // Arrange
        var filterParameters = new FilterParameters
        {
            LocationId = "loc-1",
            Date = "2025-04-16",
            Guests = 2
        };

        var validationResult = new FluentValidation.Results.ValidationResult();
        _validatorFilterMock.Setup(v => v.ValidateAsync(filterParameters, default))
            .ReturnsAsync(validationResult);

        _locationRepositoryMock.Setup(r => r.GetLocationByIdAsync(filterParameters.LocationId))
            .ReturnsAsync(_location);

        var tables = new List<RestaurantTable> { _table };
        _tableRepositoryMock.Setup(r => r.GetTablesForLocationAsync(_location.Id, filterParameters.Guests))
            .ReturnsAsync(tables);

        // Set up a reservation that conflicts with one of the time slots
        var reservationInfo = new Reservation
        {
            Id = "res-1",
            PreOrder = "not implemented",
            Status = "Reserved",
            ClientType = Domain.Entities.Enums.ClientType.CUSTOMER,
            TableId = _table.Id,
            Date = filterParameters.Date,
            TimeFrom = "14:00",
            TimeTo = "16:00",
            GuestsNumber = "2",
            LocationId = _location.Id,
            LocationAddress = _location.Address,
            TableCapacity = _table.Capacity.ToString(),
            TableNumber = _table.TableNumber,
            TimeSlot = "14:00 - 16:00",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),

        };

        var reservations = new List<Reservation> { reservationInfo };
        _reservationRepositoryMock.Setup(r => r.GetReservationsForDateAndLocation(filterParameters.Date, _location.Id))
            .ReturnsAsync(reservations);

        // Act
        var result = await _reservationService.GetAvailableTablesAsync(filterParameters);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(1));

        // Verify that slots conflicting with the reservation are filtered out
        var tableDto = result.First();
        Assert.That(tableDto.AvailableSlots, Is.Not.Empty);
        Assert.That(tableDto.AvailableSlots.Any(s =>
            TimeSpan.ParseExact(s.Start, "hh\\:mm", CultureInfo.InvariantCulture) >=
            TimeSpan.ParseExact("14:00", "hh\\:mm", CultureInfo.InvariantCulture) &&
            TimeSpan.ParseExact(s.End, "hh\\:mm", CultureInfo.InvariantCulture) <=
            TimeSpan.ParseExact("16:00", "hh\\:mm", CultureInfo.InvariantCulture)),
            Is.False);
    }

    [Test]
    public async Task GetAvailableTablesAsync_WithSpecificTimeRequest_ReturnsMatchingSlot()
    {
        // Arrange
        var filterParameters = new FilterParameters
        {
            LocationId = "loc-1",
            Date = "2025-04-16",
            Guests = 2,
            Time = "13:00" // Specific time request
        };

        var validationResult = new FluentValidation.Results.ValidationResult();
        _validatorFilterMock.Setup(v => v.ValidateAsync(filterParameters, default))
            .ReturnsAsync(validationResult);

        _locationRepositoryMock.Setup(r => r.GetLocationByIdAsync(filterParameters.LocationId))
            .ReturnsAsync(_location);

        var tables = new List<RestaurantTable> { _table };
        _tableRepositoryMock.Setup(r => r.GetTablesForLocationAsync(_location.Id, filterParameters.Guests))
            .ReturnsAsync(tables);

        var reservations = new List<Reservation>();
        _reservationRepositoryMock.Setup(r => r.GetReservationsForDateAndLocation(filterParameters.Date, _location.Id))
            .ReturnsAsync(reservations);

        // Act
        var result = await _reservationService.GetAvailableTablesAsync(filterParameters);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(1));

        var tableDto = result.First();
        Assert.That(tableDto.AvailableSlots, Is.Not.Empty);
        Assert.That(tableDto.AvailableSlots.Count, Is.EqualTo(1)); // Only one slot should be returned

        var slot = tableDto.AvailableSlots.First();
        // Either the slot should start at exactly the requested time,
        // or the requested time should be within the slot's duration
        var requestedTime = TimeSpan.ParseExact("13:00", "hh\\:mm", CultureInfo.InvariantCulture);
        var slotStart = TimeSpan.ParseExact(slot.Start, "hh\\:mm", CultureInfo.InvariantCulture);
        var slotEnd = TimeSpan.ParseExact(slot.End, "hh\\:mm", CultureInfo.InvariantCulture);

        Assert.That(slotStart <= requestedTime && requestedTime <= slotEnd, Is.True);
    }

    [Test]
    public async Task GetAvailableTablesAsync_NoTablesWithSufficientCapacity_ReturnsEmptyCollection()
    {
        // Arrange
        var filterParameters = new FilterParameters
        {
            LocationId = "loc-1",
            Date = "2025-04-16",
            Guests = 10 // More guests than available capacity
        };

        var validationResult = new FluentValidation.Results.ValidationResult();
        _validatorFilterMock.Setup(v => v.ValidateAsync(filterParameters, default))
            .ReturnsAsync(validationResult);

        _locationRepositoryMock.Setup(r => r.GetLocationByIdAsync(filterParameters.LocationId))
            .ReturnsAsync(_location);

        // No tables with sufficient capacity
        var tables = new List<RestaurantTable>();
        _tableRepositoryMock.Setup(r => r.GetTablesForLocationAsync(_location.Id, filterParameters.Guests))
            .ReturnsAsync(tables);

        // Act
        var result = await _reservationService.GetAvailableTablesAsync(filterParameters);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }
    #endregion
}