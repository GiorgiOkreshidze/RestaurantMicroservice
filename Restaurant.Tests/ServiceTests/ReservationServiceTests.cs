using System.Globalization;
using Amazon.SQS;
using Amazon.SQS.Model;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Restaurant.Application.DTOs.Aws;
using Restaurant.Application.DTOs.RabbitMq;
using Restaurant.Application.DTOs.Reservations;
using Restaurant.Application.DTOs.Tables;
using Restaurant.Application.DTOs.Users;
using Restaurant.Application.Exceptions;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.DTOs;
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
    private Mock<IFeedbackRepository> _feedbackRepository = null!;
    private IReservationService _reservationService = null!;
    private Mock<ITokenService> _tokenService = null!;
    private Mock<IValidator<FilterParameters>> _validatorFilterMock = null!;
    private Mock<IOptions<RabbitMqSettings>> _rabbitMqOptionsMock = null!;
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
        _feedbackRepository = new Mock<IFeedbackRepository>();
        _tokenService = new Mock<ITokenService>();
        _validatorFilterMock = new Mock<IValidator<FilterParameters>>();
        _rabbitMqOptionsMock = new Mock<IOptions<RabbitMqSettings>>();
        
        
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Reservation, ReservationDto>().ReverseMap();
            cfg.CreateMap<RestaurantTable, RestaurantTableDto>().ReverseMap();
            cfg.CreateMap<Reservation, ClientReservationResponse>().ReverseMap();
            cfg.CreateMap<User, UserDto>().ReverseMap();
            cfg.CreateMap<Reservation, ReservationResponseDto>()
                      .ForMember(dest => dest.ClientType, opt => opt.MapFrom(src => src.ClientTypeString));
            cfg.CreateMap<ReservationsQueryParameters, ReservationsQueryParametersDto>();
        });
        _mapper = config.CreateMapper();

        _reservationService = new ReservationService(
            _reservationRepositoryMock.Object,
            _locationRepositoryMock.Object,
            _userRepositoryMock.Object,
            _tableRepositoryMock.Object,
            _waiterRepositoryMock.Object,
            _feedbackRepository.Object,
            _validatorFilterMock.Object,
            _tokenService.Object,
            _rabbitMqOptionsMock.Object,
            _mapper);
        
        _request = new ClientReservationRequest
        {
            Id = null,
            Date = "2026-05-01",
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
        var result = await task!;

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
        var result = await task!;

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

    #region GetReservations
    [Test]
    public async Task GetReservationsAsync_CustomerRole_ReturnsCustomerReservations()
    {
        // Arrange
        var queryParams = new ReservationsQueryParameters();
        var userId = "user-123";
        var email = "customer@example.com";
        var role = "Customer";

        var customerReservations = new List<Reservation>
    {
        new Reservation
        {
            Id = "res-1",
            Date = "2023-05-01",
            GuestsNumber = "2",
            LocationId = "loc-1",
            LocationAddress = "123 Main St",
            PreOrder = "Not implemented",
            Status = "Reserved",
            TableId = "table-1",
            TableCapacity = "4",
            TableNumber = "T1",
            TimeFrom = "12:00",
            TimeTo = "14:00",
            TimeSlot = "12:00 - 14:00",
            UserEmail = email,
            UserInfo = "John Doe",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ClientTypeString = "CUSTOMER"
        },
        new Reservation
        {
            Id = "res-2",
            Date = "2023-05-02",
            GuestsNumber = "3",
            LocationId = "loc-1",
            LocationAddress = "123 Main St",
            PreOrder = "Not implemented",
            Status = "Reserved",
            TableId = "table-2",
            TableCapacity = "6",
            TableNumber = "T2",
            TimeFrom = "18:00",
            TimeTo = "20:00",
            TimeSlot = "18:00 - 20:00",
            UserEmail = email,
            UserInfo = "John Doe",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ClientTypeString = "CUSTOMER"
        }
    };

        _reservationRepositoryMock.Setup(r => r.GetCustomerReservationsAsync(email))
            .ReturnsAsync(customerReservations);

        // Act
        var result = await _reservationService.GetReservationsAsync(queryParams, userId, email, role);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
        _reservationRepositoryMock.Verify(r => r.GetCustomerReservationsAsync(email), Times.Once);
        _reservationRepositoryMock.Verify(r => r.GetWaiterReservationsAsync(It.IsAny<ReservationsQueryParametersDto>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task GetReservationsAsync_WaiterRole_ReturnsWaiterReservations()
    {
        // Arrange
        var queryParams = new ReservationsQueryParameters
        {
            Date = "2023-05-01",
            TableId = "table-1"
        };
        var userId = "waiter-123";
        var email = "waiter@example.com";
        var role = "Waiter";

        var waiterReservations = new List<Reservation>
    {
        new Reservation
        {
            Id = "res-3",
            Date = "2023-05-01",
            GuestsNumber = "4",
            LocationId = "loc-1",
            LocationAddress = "123 Main St",
            PreOrder = "Not implemented",
            Status = "Reserved",
            TableId = "table-1",
            TableCapacity = "4",
            TableNumber = "T1",
            TimeFrom = "16:00",
            TimeTo = "18:00",
            TimeSlot = "16:00 - 18:00",
            UserEmail = "customer1@example.com",
            UserInfo = "Customer One",
            WaiterId = userId,
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ClientTypeString = "CUSTOMER"
        },
        new Reservation
        {
            Id = "res-4",
            Date = "2023-05-01",
            GuestsNumber = "2",
            LocationId = "loc-1",
            LocationAddress = "123 Main St",
            PreOrder = "Not implemented",
            Status = "Reserved",
            TableId = "table-1",
            TableCapacity = "4",
            TableNumber = "T1",
            TimeFrom = "19:00",
            TimeTo = "21:00",
            TimeSlot = "19:00 - 21:00",
            UserEmail = "customer2@example.com",
            UserInfo = "Customer Two",
            WaiterId = userId,
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ClientTypeString = "VISITOR"
        }
    };

        _reservationRepositoryMock.Setup(r => r.GetWaiterReservationsAsync(
            It.IsAny<ReservationsQueryParametersDto>(), userId))
            .ReturnsAsync(waiterReservations);

        // Act
        var result = await _reservationService.GetReservationsAsync(queryParams, userId, email, role);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
        _reservationRepositoryMock.Verify(r => r.GetCustomerReservationsAsync(It.IsAny<string>()), Times.Never);
        _reservationRepositoryMock.Verify(r => r.GetWaiterReservationsAsync(It.IsAny<ReservationsQueryParametersDto>(), userId), Times.Once);
    }

    [Test]
    public async Task GetReservationsAsync_QueryParametersMapping_MapsCorrectly()
    {
        // Arrange
        var queryParams = new ReservationsQueryParameters
        {
            Date = "2023-05-01",
            TimeFrom = "14:00",
            TableId = "table-1"
        };
        var userId = "waiter-123";
        var email = "waiter@example.com";
        var role = "Waiter";

        var mappedDto = new ReservationsQueryParametersDto
        {
            Date = "2023-05-01",
            TimeFrom = "14:00",
            TableId = "table-1"
        };

        var waiterReservations = new List<Reservation>
    {
        new Reservation
        {
            Id = "res-3",
            Date = "2023-05-01",
            GuestsNumber = "4",
            LocationId = "loc-1",
            LocationAddress = "123 Main St",
            PreOrder = "Not implemented",
            Status = "Reserved",
            TableId = "table-1",
            TableCapacity = "4",
            TableNumber = "T1",
            TimeFrom = "14:00",
            TimeTo = "16:00",
            TimeSlot = "14:00 - 16:00",
            UserEmail = "customer@example.com",
            UserInfo = "Customer Name",
            WaiterId = userId,
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ClientTypeString = "CUSTOMER"
        }
    };

        _reservationRepositoryMock.Setup(r => r.GetWaiterReservationsAsync(
            It.Is<ReservationsQueryParametersDto>(dto =>
                dto.Date == mappedDto.Date &&
                dto.TimeFrom == mappedDto.TimeFrom &&
                dto.TableId == mappedDto.TableId),
            userId))
            .ReturnsAsync(waiterReservations);

        // Act
        await _reservationService.GetReservationsAsync(queryParams, userId, email, role);

        // Assert
        _reservationRepositoryMock.Verify(r => r.GetWaiterReservationsAsync(
            It.Is<ReservationsQueryParametersDto>(dto =>
                dto.Date == mappedDto.Date &&
                dto.TimeFrom == mappedDto.TimeFrom &&
                dto.TableId == mappedDto.TableId),
            userId), Times.Once);
    }

    [Test]
    public async Task GetReservationsAsync_ClientTypeMapping_MapsCorrectly()
    {
        // Arrange
        var queryParams = new ReservationsQueryParameters();
        var userId = "user-123";
        var email = "customer@example.com";
        var role = "Customer";

        var customerReservations = new List<Reservation>
    {
        new Reservation
        {
            Id = "res-1",
            Date = "2023-05-01",
            GuestsNumber = "2",
            LocationId = "loc-1",
            LocationAddress = "123 Main St",
            PreOrder = "Not implemented",
            Status = "Reserved",
            TableId = "table-1",
            TableCapacity = "4",
            TableNumber = "T1",
            TimeFrom = "12:00",
            TimeTo = "14:00",
            TimeSlot = "12:00 - 14:00",
            UserEmail = email,
            UserInfo = "John Doe",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ClientTypeString = "CUSTOMER"
        },
        new Reservation
        {
            Id = "res-2",
            Date = "2023-05-02",
            GuestsNumber = "3",
            LocationId = "loc-1",
            LocationAddress = "123 Main St",
            PreOrder = "Not implemented",
            Status = "Reserved",
            TableId = "table-2",
            TableCapacity = "6",
            TableNumber = "T2",
            TimeFrom = "18:00",
            TimeTo = "20:00",
            TimeSlot = "18:00 - 20:00",
            UserEmail = email,
            UserInfo = "John Doe",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ClientTypeString = "VISITOR"
        }
    };

        _reservationRepositoryMock.Setup(r => r.GetCustomerReservationsAsync(email))
            .ReturnsAsync(customerReservations);

        // Configure mapper mock to properly map ClientTypeString
        var config = new MapperConfiguration(cfg => {
            cfg.CreateMap<Reservation, ReservationResponseDto>()
                .ForMember(dest => dest.ClientType, opt => opt.MapFrom(src => src.ClientTypeString));
        });
        var mapper = config.CreateMapper();

        var service = new ReservationService(
            _reservationRepositoryMock.Object,
            _locationRepositoryMock.Object,
            _userRepositoryMock.Object,
            _tableRepositoryMock.Object,
            _waiterRepositoryMock.Object,
            _feedbackRepository.Object,
            _validatorFilterMock.Object,
            _tokenService.Object,
            _rabbitMqOptionsMock.Object,
            mapper);

        // Act
        var result = await service.GetReservationsAsync(queryParams, userId, email, role);

        // Assert
        Assert.That(result, Is.Not.Null);
        var resultList = result.ToList();
        Assert.That(resultList[0].ClientType, Is.EqualTo("CUSTOMER"));
        Assert.That(resultList[1].ClientType, Is.EqualTo("VISITOR"));
    }

    [Test]
    public async Task GetReservationsAsync_EmptyReservations_ReturnsEmptyCollection()
    {
        // Arrange
        var queryParams = new ReservationsQueryParameters();
        var userId = "user-123";
        var email = "customer@example.com";
        var role = "Customer";

        _reservationRepositoryMock.Setup(r => r.GetCustomerReservationsAsync(email))
            .ReturnsAsync(new List<Reservation>());

        // Act
        var result = await _reservationService.GetReservationsAsync(queryParams, userId, email, role);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }
    #endregion

    #region DeleteReservation
    [Test]
    public async Task CancelReservationAsync_CustomerCancellingOwnReservation_ReturnsCanceledReservation()
    {
        // Arrange
        var reservationId = "res-1";
        var userId = "user-123";
        var role = "Customer";

        var reservation = new Reservation
        {
            Id = reservationId,
            Date = "2023-05-01",
            GuestsNumber = "2",
            LocationId = "loc-1",
            LocationAddress = "123 Main St",
            PreOrder = "Not implemented",
            Status = "Reserved",
            TableId = "table-1",
            TableCapacity = "4",
            TableNumber = "T1",
            TimeFrom = "12:00",
            TimeTo = "14:00",
            TimeSlot = "12:00 - 14:00",
            UserEmail = "user@example.com",
            UserInfo = "John Doe",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ClientTypeString = "CUSTOMER"
        };

        var user = new User
        {
            Id = userId,
            Email = "user@example.com",
            FirstName = "John",
            LastName = "Doe",
            ImgUrl = "some user",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };

        _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync(reservationId))
            .ReturnsAsync(reservation);
        _userRepositoryMock.Setup(r => r.GetUserByIdAsync(userId))
            .ReturnsAsync(user);
        _reservationRepositoryMock.Setup(r => r.CancelReservationAsync(reservationId))
            .ReturnsAsync(reservation);

        // Act
        var result = await _reservationService.CancelReservationAsync(reservationId, userId, role);

        // Assert
        Assert.That(result, Is.Not.Null);
        _reservationRepositoryMock.Verify(r => r.CancelReservationAsync(reservationId), Times.Once);
    }

    [Test]
    public async Task CancelReservationAsync_AdminCancellingAnyReservation_ReturnsCanceledReservation()
    {
        // Arrange
        var reservationId = "res-1";
        var adminId = "admin-123";
        var role = "Admin";

        var reservation = new Reservation
        {
            Id = reservationId,
            Date = "2023-05-01",
            GuestsNumber = "2",
            LocationId = "loc-1",
            LocationAddress = "123 Main St",
            PreOrder = "Not implemented",
            Status = "Reserved",
            TableId = "table-1",
            TableCapacity = "4",
            TableNumber = "T1",
            TimeFrom = "12:00",
            TimeTo = "14:00",
            TimeSlot = "12:00 - 14:00",
            UserEmail = "user@example.com",
            UserInfo = "John Doe",
            WaiterId = "waiter-456",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ClientTypeString = "CUSTOMER"
        };

        _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync(reservationId))
            .ReturnsAsync(reservation);
        _reservationRepositoryMock.Setup(r => r.CancelReservationAsync(reservationId))
            .ReturnsAsync(reservation);

        // Act
        var result = await _reservationService.CancelReservationAsync(reservationId, adminId, role);

        // Assert
        Assert.That(result, Is.Not.Null);
        _reservationRepositoryMock.Verify(r => r.CancelReservationAsync(reservationId), Times.Once);
    }

    [Test]
    public void CancelReservationAsync_ReservationNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var reservationId = "non-existent-id";
        var userId = "user-123";
        var role = "Customer";

        _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync(reservationId))
            .ReturnsAsync((Reservation?)null);

        // Act & Assert
        var exception = Assert.ThrowsAsync<NotFoundException>(async () =>
            await _reservationService.CancelReservationAsync(reservationId, userId, role));

        Assert.That(exception?.Message, Does.Contain(reservationId));
        _reservationRepositoryMock.Verify(r => r.CancelReservationAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void CancelReservationAsync_CompletedReservation_ThrowsConflictException()
    {
        // Arrange
        var reservationId = "res-1";
        var userId = "user-123";
        var role = "Customer";

        var reservation = new Reservation
        {
            Id = reservationId,
            Date = "2023-05-01",
            GuestsNumber = "2",
            Status = "Finished",  // Completed reservation
            UserEmail = "user@example.com",
            TableId = "table-1",
            TableCapacity = "4",
            TableNumber = "T1",
            LocationId = "loc-1",
            LocationAddress = "123 Main St",
            PreOrder = "Not implemented",
            TimeFrom = "12:00",
            TimeTo = "14:00",
            TimeSlot = "12:00 - 14:00",
            UserInfo = "John Doe",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ClientTypeString = "CUSTOMER"
        };

        _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync(reservationId))
            .ReturnsAsync(reservation);

        // Act & Assert
        var exception = Assert.ThrowsAsync<ConflictException>(async () =>
            await _reservationService.CancelReservationAsync(reservationId, userId, role));

        Assert.That(exception?.Message, Does.Contain("Cannot cancel a completed reservation"));
        _reservationRepositoryMock.Verify(r => r.CancelReservationAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void CancelReservationAsync_CustomerCancellingOtherCustomerReservation_ThrowsUnauthorizedException()
    {
        // Arrange
        var reservationId = "res-1";
        var userId = "user-123";
        var role = "Customer";

        var reservation = new Reservation
        {
            Id = reservationId,
            Date = "2023-05-01",
            GuestsNumber = "2",
            LocationId = "loc-1",
            LocationAddress = "123 Main St",
            PreOrder = "Not implemented",
            Status = "Reserved",
            TableId = "table-1",
            TableCapacity = "4",
            TableNumber = "T1",
            TimeFrom = "12:00",
            TimeTo = "14:00",
            TimeSlot = "12:00 - 14:00",
            UserEmail = "other@example.com",  // Different user email
            UserInfo = "Other User",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ClientTypeString = "CUSTOMER"
        };

        var user = _user;

        _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync(reservationId))
            .ReturnsAsync(reservation);
        _userRepositoryMock.Setup(r => r.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = Assert.ThrowsAsync<UnauthorizedException>(async () =>
            await _reservationService.CancelReservationAsync(reservationId, userId, role));

        Assert.That(exception?.Message, Does.Contain("You can only cancel your own reservations"));
        _reservationRepositoryMock.Verify(r => r.CancelReservationAsync(It.IsAny<string>()), Times.Never);
    }
    #endregion

    #region CompleteReservation

    [Test]
    public async Task CompleteReservationAsync_ReservationIdCompletedSuccessfully_ReturnsQrCodeResponse()
    {
        // Arrange
        var reservation = new Reservation
        {
            Id = "res-1",
            Date = "2023-05-01",
            TimeFrom = "13:30",
            TimeTo = "15:00",
            WaiterId = "waiter-1",
            Status = "Reserved",
            LocationAddress = "Main Street 123",
            GuestsNumber = "3",
            LocationId = "loc-1",
            PreOrder = "Not implemented",
            TableId = "table-2",
            TableCapacity = "6",
            TableNumber = "T2",
            TimeSlot = "18:00 - 20:00",
            UserEmail = "userEmail",
            UserInfo = "John Doe",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ClientTypeString = "VISITOR"
        };

        var waiter = new User
        {
            Id = "waiter-1",
            Email = "waiter@example.com",
            FirstName = "John",
            LastName = "Doe",
            LocationId = "loc-1",
            ImgUrl = "null",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };

        var feedbacks = new List<Feedback>
        {
            new Feedback
            {
                Id = "feed-1",
                LocationId = "loc-1",
                Type = "SERVICE_QUALITY",
                TypeDate = "SERVICE_QUALITY#2025-04-23T12:00:00Z",
                Rate = 3,
                Comment = "EXCELLENT!",
                UserName = "Joe Smith",
                UserAvatarUrl = "https://example.com/avatar2.jpg",
                Date = "2025-04-23T12:00:00Z",
                ReservationId = "res-456",
                LocationIdType = $"loc-1#SERVICE_QUALITY",
                ReservationIdType = "res-1#SERVICE_QUALITY"
            },
            new Feedback
            {
                Id = "feed-2",
                LocationId = "loc-1",
                Type = "SERVICE_QUALITY",
                TypeDate = "SERVICE_QUALITY#2025-04-23T12:00:00Z",
                Rate = 4,
                Comment = "EXCELLENT!",
                UserName = "Jane Smith",
                UserAvatarUrl = "https://example.com/avatar2.jpg",
                Date = "2025-04-23T12:00:00Z",
                ReservationId = "res-456",
                LocationIdType = $"loc-1#SERVICE_QUALITY",
                ReservationIdType = "res-1#SERVICE_QUALITY"
            }
        };

        const string mockToken = "mock-jwt-token";

        _reservationRepositoryMock
            .Setup(r => r.GetReservationByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        _reservationRepositoryMock
            .Setup(r => r.UpsertReservationAsync(It.IsAny<Reservation>()))
            .ReturnsAsync(reservation);

        _userRepositoryMock
            .Setup(r => r.GetUserByIdAsync(reservation.WaiterId!))
            .ReturnsAsync(waiter);

        _feedbackRepository
            .Setup(r => r.GetServiceFeedbacks(reservation.Id))
            .ReturnsAsync(feedbacks);

        _tokenService
            .Setup(t => t.GenerateAnonymousFeedbackToken(reservation.Id))
            .Returns(mockToken);

        var rabbitMqSettings = new RabbitMqSettings { HostName = "localhost", Port = 5672, UserName = "guest", Password = "guest" };
        _rabbitMqOptionsMock.Setup(x => x.Value).Returns(rabbitMqSettings);

        // Act
        var result = await _reservationService.CompleteReservationAsync(reservation.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.QrCodeImageBase64, Is.Not.Null);
        Assert.That(result.FeedbackUrl, Is.Not.Null);
        Assert.That(result.FeedbackUrl, Does.Contain(mockToken));

        _reservationRepositoryMock.Verify(r => r.GetReservationByIdAsync(reservation.Id), Times.Once);
        _reservationRepositoryMock.Verify(r => r.UpsertReservationAsync(It.Is<Reservation>(r =>
            r.Status == ReservationStatus.Finished.ToString() &&
            r.FeedbackToken == mockToken)), Times.AtLeastOnce);
        _userRepositoryMock.Verify(r => r.GetUserByIdAsync(reservation.WaiterId!), Times.Once);
        _feedbackRepository.Verify(r => r.GetServiceFeedbacks(reservation.Id), Times.AtLeastOnce);
        _tokenService.Verify(t => t.GenerateAnonymousFeedbackToken(reservation.Id), Times.Once);
    }
    
    [Test]
    public void CompleteReservation_ReservationIsNull_ThrowsNotFoundException()
    {
        // Arrange
        var invalidReservationId = "invalid-reservation-id";

        _reservationRepositoryMock
            .Setup(r => r.GetReservationByIdAsync(invalidReservationId))
            .ReturnsAsync((Reservation?)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () =>
            await _reservationService.CompleteReservationAsync(invalidReservationId));

        Assert.That(ex?.Message, Is.EqualTo($"The Reservation with the key '{invalidReservationId}' was not found."));
    }

    [Test]
    public void CompleteReservation_AlreadyFinishedReservation_ThrowsConflictException()
    {
        // Arrange
        var reservation = new Reservation
        {
            Id = "res-finished",
            Date = "2023-05-01",
            TimeFrom = "13:30",
            TimeTo = "15:00",
            WaiterId = "waiter-1",
            Status = ReservationStatus.Finished.ToString(), // Already finished
            LocationAddress = "Main Street 123",
            GuestsNumber = "3",
            LocationId = "loc-1",
            PreOrder = "Not implemented",
            TableId = "table-2",
            TableCapacity = "6",
            TableNumber = "T2",
            TimeSlot = "13:30 - 15:00",
            UserEmail = "userEmail",
            UserInfo = "John Doe",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ClientTypeString = "VISITOR"
        };

        _reservationRepositoryMock
            .Setup(r => r.GetReservationByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await _reservationService.CompleteReservationAsync(reservation.Id));

        Assert.That(ex?.Message, Is.EqualTo("The reservation has already been completed."));
        
        // Verify that UpsertReservationAsync was not called
        _reservationRepositoryMock.Verify(r => r.UpsertReservationAsync(It.IsAny<Reservation>()), Times.Never);
        // Verify that SendMessageAsync was not called
    }

    #endregion
}
