using System.Globalization;
using Amazon.DynamoDBv2.Model;
using AutoMapper;
using Restaurant.Application.DTOs.Reservations;
using Restaurant.Application.DTOs.Tables;
using Restaurant.Application.DTOs.Users;
using Restaurant.Application.Exceptions;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Entities.Enums;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Application.Services;

public class ReservationService(
    IReservationRepository reservationRepository, 
    ILocationRepository locationRepository, 
    IUserRepository userRepository,
    ITableRepository tableRepository,
    IWaiterRepository waiterRepository,
    IMapper mapper) : IReservationService
{
    public async Task<ReservationDto> UpsertReservationAsync(BaseReservationRequest reservationRequest, string userId)
    {
        ValidateTimeSlot(reservationRequest);
        var location = await locationRepository.GetLocationByIdAsync(reservationRequest.LocationId);
        var table = await GetAndValidateTable(reservationRequest.TableId, reservationRequest.GuestsNumber);

        var reservationDto = new Reservation
        {
            Id = reservationRequest.Id ?? Guid.NewGuid().ToString(),
            Date = reservationRequest.Date,
            GuestsNumber = reservationRequest.GuestsNumber,
            LocationAddress = location.Address,
            LocationId = location.Id,
            PreOrder = "NOT IMPLEMENTED YET",
            TableCapacity = table.Capacity,
            Status = ReservationStatus.Reserved.ToString(),
            TableId = reservationRequest.TableId,
            TableNumber = table.TableNumber,
            TimeFrom = reservationRequest.TimeFrom,
            TimeTo = reservationRequest.TimeTo,
            TimeSlot = reservationRequest.TimeFrom + " - " + reservationRequest.TimeTo,
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };

        return reservationRequest switch
        {
            ClientReservationRequest clientRequest => await ProcessClientReservation(clientRequest, reservationDto, userId),
            _ => throw new ArgumentOutOfRangeException(nameof(reservationRequest), reservationRequest, null)
        };
    }
    
    private async Task<ReservationDto> ProcessClientReservation(ClientReservationRequest request, Reservation reservation, string userId)
    {
        var user = await ValidateUser(userId);
        HandleClientReservation(reservation, user);

        await CheckForConflictingReservations(request, reservation.LocationAddress, user.Email);
        var isNewReservation = !await reservationRepository.ReservationExistsAsync(reservation.Id);

        if (isNewReservation)
        {
            reservation.WaiterId ??= await GetLeastBusyWaiter(reservation.LocationId, reservation.Date);
        }
        else
        {
            await ValidateModificationPermissionsForClient(reservation, user);
        }
        
        await reservationRepository.UpsertReservationAsync(reservation);
        
        return mapper.Map<ReservationDto>(reservation);
    }
    
    private async Task ValidateModificationPermissionsForClient(Reservation newReservation, UserDto user)
    {
        var existingReservation = await reservationRepository.GetReservationByIdAsync(newReservation.Id);

        if (user.Email != existingReservation.UserEmail)
        {
            throw new UnauthorizedException("Only the customer or assigned waiter can modify this reservation");
        }
        newReservation.WaiterId = existingReservation.WaiterId;
        ValidateReservationTimeLock(mapper.Map<ReservationDto>(existingReservation));
    }
    
    
    private void ValidateReservationTimeLock(ReservationDto reservation)
    {
        var reservationDateTime = DateTime.ParseExact(
            reservation.Date,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture
        ).Add(TimeSpan.Parse(reservation.TimeFrom));

        if (DateTime.UtcNow > reservationDateTime.AddMinutes(-30))
        {
            throw new ArgumentException("Reservations cannot be modified within 30 minutes of start time");
        }
    }
    
    private async Task<UserDto> ValidateUser(string userId)
    {
        var user = await userRepository.GetUserByIdAsync(userId);
        if (user is null)
        {
            throw new UnauthorizedException("User is not registered");
        }
        
        return mapper.Map<UserDto>(user);
    }
    
    private void ValidateTimeSlot(BaseReservationRequest request)
    {
        var predefinedSlots = Utils.GeneratePredefinedTimeSlots();
        var newTimeFrom = TimeSpan.Parse(request.TimeFrom);
        var newTimeTo = TimeSpan.Parse(request.TimeTo);
    
        var firstSlot = TimeSpan.Parse(predefinedSlots.First().Start);
        var lastSlot = TimeSpan.Parse(predefinedSlots.Last().End);
    
        if (newTimeFrom < firstSlot || newTimeTo > lastSlot)
        {
            throw new ArgumentException("Reservation must be within restaurant working hours.");
        }
    
        bool isValidSlot = predefinedSlots.Any(slot =>
            TimeSpan.Parse(slot.Start) == newTimeFrom &&
            TimeSpan.Parse(slot.End) == newTimeTo);
    
        if (!isValidSlot)
        {
            throw new ArgumentException("Reservation must exactly match one of the predefined time slots.");
        }
    }
    
    private async Task<RestaurantTableDto> GetAndValidateTable(string tableId, string guestsNumber)
    {
        var table = await tableRepository.GetTableById(tableId);
        if (table is null)
        {
            throw new ResourceNotFoundException($"Table with ID {tableId} not found.");
        }
    
        if (!int.TryParse(table.Capacity, out int capacity) ||
            !int.TryParse(guestsNumber, out int guests))
        {
            throw new ArgumentException("Invalid number format for Capacity or GuestsNumber.");
        }
    
        if (capacity < guests)
        {
            throw new ArgumentException(
                $"Table with ID {tableId} cannot accommodate {guestsNumber} guests. " +
                $"Maximum capacity: {table.Capacity}.");
        }
    
        return mapper.Map<RestaurantTableDto>(table);
    }
    
    private void HandleClientReservation(Reservation reservation, UserDto user)
    {
        reservation.UserEmail = user.Email;
        reservation.UserInfo = user.GetFullName();
        reservation.ClientType = ClientType.CUSTOMER;
    }
    
    private async Task<string> GetLeastBusyWaiter(string locationId, string date)
    {
        var waiters = await waiterRepository.GetWaitersByLocationAsync(locationId);
        
        var reservationCounts = new Dictionary<string, int>();

        foreach (var waiter in waiters)
        {
            var count = await reservationRepository.GetWaiterReservationCountAsync(waiter.Id, date);
            reservationCounts[waiter.Id] = count;
        }

        return reservationCounts
            .OrderBy(x => x.Value)
            .FirstOrDefault().Key ?? throw new ResourceNotFoundException($"No waiters available for location ID: {locationId} after counting reservations");
    }
    
    private async Task CheckForConflictingReservations(
        BaseReservationRequest request,
        string locationAddress,
        string userEmail)
    {
        var existingReservations = await reservationRepository.GetReservationsByDateLocationTable(
            request.Date,
            locationAddress,
            request.TableId);

        var newTimeFrom = TimeSpan.Parse(request.TimeFrom);
        var newTimeTo = TimeSpan.Parse(request.TimeTo);

        foreach (var existingReservation in existingReservations)
        {
            var existingTimeFrom = TimeSpan.Parse(existingReservation.TimeFrom);
            var existingTimeTo = TimeSpan.Parse(existingReservation.TimeTo);

            if (newTimeFrom <= existingTimeTo && newTimeTo >= existingTimeFrom && existingReservation.Id != request.Id)
            {
                if (existingReservation.UserEmail == userEmail)
                {
                    throw new ArgumentException(
                        $"You already have reservation booked at location " +
                        $"{locationAddress} during the requested time period.");
                }

                throw new ArgumentException(
                    $"Reservation #{request.Id} at location " +
                    $"{locationAddress} is already booked during the requested time period.");
            }
        }
    }
}