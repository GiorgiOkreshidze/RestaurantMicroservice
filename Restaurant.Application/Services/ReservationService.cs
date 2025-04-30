using System.Globalization;
using AutoMapper;
using FluentValidation;
using Restaurant.Application.DTOs.Reservations;
using Restaurant.Application.DTOs.Tables;
using Restaurant.Application.DTOs.Users;
using Restaurant.Application.Exceptions;
using Restaurant.Application.Interfaces;
using Restaurant.Domain;
using Restaurant.Domain.DTOs;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Entities.Enums;
using Restaurant.Infrastructure.Interfaces;
using Amazon.SQS.Model;
using Amazon.SQS;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Restaurant.Application.DTOs.Aws;
using Restaurant.Infrastructure.Repositories;
using Amazon.DynamoDBv2.Model;
using QRCoder;
using Restaurant.Application.DTOs.Reports;

namespace Restaurant.Application.Services;

public class ReservationService(
    IReservationRepository reservationRepository,
    ILocationRepository locationRepository,
    IUserRepository userRepository,
    ITableRepository tableRepository,
    IWaiterRepository waiterRepository,
    IFeedbackRepository feedbackRepository,
    IValidator<FilterParameters> filterValidator,
    ITokenService tokenService,
    IAmazonSQS amazonSqsClient,
    IOptions<AwsSettings> awsSettings,
    IMapper mapper) : IReservationService
{
    public async Task<IEnumerable<AvailableTableDto>> GetAvailableTablesAsync(FilterParameters filterParameters)
    {
        var validationResult = await filterValidator.ValidateAsync(filterParameters);
        if (!validationResult.IsValid)
        {
            throw new BadRequestException("Invalid Request", validationResult);
        }

        var location = await locationRepository.GetLocationByIdAsync(filterParameters.LocationId) ?? throw new NotFoundException("Location", filterParameters.LocationId);
        var tables = await tableRepository.GetTablesForLocationAsync(location.Id, filterParameters.Guests);
        var reservations = await reservationRepository.GetReservationsForDateAndLocation(filterParameters.Date, location.Id);
        var result = CalculateAvailableSlots(tables, reservations, filterParameters.Time);
        var tablesWithSlots = result.Where(t => t.AvailableSlots.Count != 0).ToList();

        return tablesWithSlots;
    }

    public async Task<ClientReservationResponse> UpsertReservationAsync(BaseReservationRequest reservationRequest, string userId)
    {
        ValidateTimeSlot(reservationRequest);
        var location = await locationRepository.GetLocationByIdAsync(reservationRequest.LocationId) ?? throw new NotFoundException("Location", reservationRequest.LocationId);
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
            WaiterReservationRequest waiterRequest => await ProcessWaiterReservation(waiterRequest, reservationDto, userId, location.Id),
            ClientReservationRequest clientRequest => await ProcessClientReservation(clientRequest, reservationDto, userId),
            _ => throw new ArgumentOutOfRangeException(nameof(reservationRequest), reservationRequest, null)
        };
    }

    public async Task<IEnumerable<ReservationResponseDto>> GetReservationsAsync(ReservationsQueryParameters queryParams, string userId, string email, string role)
    {
        IEnumerable<Reservation> reservations;
        // Compare strings directly instead of converting to enum
        if (role.Equals("Customer", StringComparison.OrdinalIgnoreCase))
        {
            reservations = await reservationRepository.GetCustomerReservationsAsync(email);
        }
        else
        {
            var queryParamsDto = mapper.Map<ReservationsQueryParametersDto>(queryParams);
            reservations = await reservationRepository.GetWaiterReservationsAsync(queryParamsDto, userId);
        }

        // Map domain entities to response DTOs
        return mapper.Map<IEnumerable<ReservationResponseDto>>(reservations);
    }

    public async Task<ReservationResponseDto> CancelReservationAsync(string reservationId, string userId, string role)
    {
        var reservation = await reservationRepository.GetReservationByIdAsync(reservationId)
            ?? throw new NotFoundException("Reservation", reservationId);

        // Check if reservation is already completed or in progress
        if (reservation.Status == ReservationStatus.Finished.ToString())
        {
            throw new ConflictException("Cannot cancel a completed reservation");
        }

        if (reservation.Status == Utils.GetEnumDescription(ReservationStatus.InProgress))
        {
            throw new ConflictException("Cannot cancel a reservation that is currently in progress");
        }

        // Check if user has permissions to cancel
        if (role.Equals("Customer", StringComparison.OrdinalIgnoreCase))
        {
            var user = await ValidateUser(userId);
            if (user.Email != reservation.UserEmail)
            {
                throw new UnauthorizedException("You can only cancel your own reservations");
            }
        }
        else if (!role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            // If waiter, check if they are assigned to this reservation or location
            if (userId != reservation.WaiterId)
            {
                var waiter = await ValidateUser(userId);
                if (waiter.LocationId != reservation.LocationId)
                {
                    throw new UnauthorizedException("You can only cancel reservations at your assigned location");
                }
            }
        }

        // Cancel the reservation
        var canceledReservation = await reservationRepository.CancelReservationAsync(reservationId);

        return mapper.Map<ReservationResponseDto>(canceledReservation);
    }
    
    public async Task<QrCodeResponse> CompleteReservationAsync(string reservationId)
    {
        var reservation = await GetAndCompleteReservation(reservationId)
                          ?? throw new NotFoundException("Reservation", reservationId);

        var report = await BuildReservationReport(reservation);
        await SendEventToSqs("reservation", report);

        if (reservation.ClientType == ClientType.VISITOR)
        {
            var feedbackToken = tokenService.GenerateAnonymousFeedbackToken(reservationId);
        
            reservation.FeedbackToken = feedbackToken;
            await reservationRepository.UpsertReservationAsync(reservation);
        
            // Todo: change with real url
            var feedbackUrl = $"https://frontend-run7team2-api-handler-dev.development.krci-dev.cloudmentor.academy/api/anonymous-feedback/validate-token?token=${feedbackToken}";
            var qrCodeBase64 = GenerateQrCodeAsync(feedbackUrl);
        
            return new QrCodeResponse
            {
                QrCodeImageBase64 = qrCodeBase64,
                FeedbackUrl = feedbackUrl
            };
        }
    
        // Return empty response for non-VISITOR clients
        return new QrCodeResponse
        {
            QrCodeImageBase64 = string.Empty,
            FeedbackUrl = string.Empty
        };
    }

    private static string GenerateQrCodeAsync(string feedbackUrl)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(feedbackUrl, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(20);

        return Convert.ToBase64String(qrCodeBytes);
    }

    private async Task<Reservation> GetAndCompleteReservation(string reservationId)
    {
        var reservation = await reservationRepository.GetReservationByIdAsync(reservationId);
        if (reservation == null)
        {
            throw new NotFoundException("Reservation", reservationId);
        }
        
        if (reservation.Status == ReservationStatus.Finished.ToString())
        {
            throw new ConflictException("The reservation has already been completed.");
        }

        reservation.Status = ReservationStatus.Finished.ToString();
        return await reservationRepository.UpsertReservationAsync(reservation);
    }
    
    private async Task<ReportDto> BuildReservationReport(Reservation reservation)
    {
        // Calculate hours worked
        var hoursWorked = CalculateHoursWorked(reservation.TimeFrom, reservation.TimeTo);
    
        // Get waiter information
        var waiter = await GetWaiterInfo(reservation.WaiterId!);
    
        // Build the report
        return new ReportDto
        {
            Date = reservation.Date,
            Location = reservation.LocationAddress,
            Waiter = waiter.FullName,
            WaiterEmail = waiter.Email,
            HoursWorked = hoursWorked,
            AverageServiceFeedback = await GetAverageServiceFeedback(reservation.Id),
            MinimumServiceFeedback = await GetMinimumServiceFeedback(reservation.Id)
        };
    }
    
    private static decimal CalculateHoursWorked(string timeFrom, string timeTo)
    {
        var fromTime = TimeSpan.Parse(timeFrom);
        var toTime = TimeSpan.Parse(timeTo);
        return (decimal)(toTime - fromTime).TotalHours;
    }
    
    private async Task<(string Email, string FullName)> GetWaiterInfo(string waiterId)
    {
        var user = await userRepository.GetUserByIdAsync(waiterId) 
                   ?? throw new NotFoundException("Waiter", waiterId);
    
        return (user.Email, $"{user.FirstName} {user.LastName}");
    }
    
    private async Task<int> GetMinimumServiceFeedback(string id)
    {
        var feedbacks = await feedbackRepository.GetServiceFeedbacks(id);

        if (feedbacks == null || !feedbacks.Any())
        {
            return 0;
        }

        return feedbacks!.Min(f => f.Rate);
    }

    private async Task<double> GetAverageServiceFeedback(string id)
    {
        var feedbacks = await feedbackRepository.GetServiceFeedbacks(id);
        if (feedbacks == null || !feedbacks.Any())
        {
            return 0;
        }

        return feedbacks!.Average(f => f.Rate);
    }

    #region Helper Methods For Reservation
    private async Task<ClientReservationResponse> ProcessWaiterReservation(
        WaiterReservationRequest request,
        Reservation reservation,
        string waiterId, string locationId)
    {
        reservation.ClientType = request.ClientType;
        reservation.WaiterId = waiterId;

        var waiter = await ValidateUser(waiterId);
   
        if (waiter.LocationId != locationId)
        {
            throw new UnauthorizedException("Waiter cannot create reservations for a different location.");
        }

        var reservationExists = await reservationRepository.ReservationExistsAsync(reservation.Id);

        if (reservationExists)
        {
            await ValidateModificationPermissionsForWaiter(reservation, waiterId);
        }

        if (request.ClientType == ClientType.CUSTOMER && request.CustomerId != null)
        {
            var customer = await ValidateUser(request.CustomerId);
            reservation.UserEmail = customer.Email;
            reservation.UserInfo = $"Customer {customer.FirstName} {customer.LastName}";

            await CheckForConflictingReservations(request, reservation.LocationAddress, customer.Email);
        }
        else
        {
            reservation.UserEmail = waiter.Email;
            reservation.UserInfo = $"Waiter {waiter.GetFullName()} (Visitor)";

            await CheckForConflictingReservations(request, reservation.LocationAddress, waiter.Email);
        }

        await reservationRepository.UpsertReservationAsync(reservation);
        
        return mapper.Map<ClientReservationResponse>(reservation);
    }
        
    private async Task<ClientReservationResponse> ProcessClientReservation(ClientReservationRequest request, Reservation reservation, string userId)
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
        
        return mapper.Map<ClientReservationResponse>(reservation);
    }

    private async Task ValidateModificationPermissionsForWaiter(Reservation newReservation, string waiterId)
    {
        var existingReservation = await reservationRepository.GetReservationByIdAsync(newReservation.Id);

        if (waiterId != existingReservation?.WaiterId)
        {
            throw new UnauthorizedException("Only assigned waiter can modify this reservation");
        }
        
        ValidateReservationTimeLock(mapper.Map<ReservationDto>(newReservation));
    }
    
    private async Task ValidateModificationPermissionsForClient(Reservation newReservation, UserDto user)
    {
        var existingReservation = await reservationRepository.GetReservationByIdAsync(newReservation.Id);

        if (user.Email != existingReservation?.UserEmail)
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
            throw new ConflictException("Reservations cannot be modified within 30 minutes of start time");
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
            throw new ConflictException("Reservation must be within restaurant working hours.");
        }

        bool isValidSlot = predefinedSlots.Any(slot =>
            TimeSpan.Parse(slot.Start) == newTimeFrom &&
            TimeSpan.Parse(slot.End) == newTimeTo);

        if (!isValidSlot)
        {
            throw new ConflictException("Reservation must exactly match one of the predefined time slots.");
        }
    }

    private async Task<RestaurantTableDto> GetAndValidateTable(string tableId, string guestsNumber)
    {
        var table = await tableRepository.GetTableById(tableId);

        if (table is null)
        {
            throw new NotFoundException($"Table with ID {tableId} not found.");
        }
    
        if (!int.TryParse(guestsNumber, out int guests))
        {
            throw new ConflictException("Invalid number format for GuestsNumber.");
        }
        
        int capacity = table.Capacity;
    
        if (capacity < guests)
        {
            throw new ConflictException(
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
        var waiters = await waiterRepository.GetWaitersByLocationAsync(locationId) ?? throw new NotFoundException("Waiters for location", locationId);

        var reservationCounts = new Dictionary<string, int>();

        foreach (var waiter in waiters)
        {
            var count = await reservationRepository.GetWaiterReservationCountAsync(waiter.Id!, date);
            reservationCounts[waiter.Id!] = count;
        }

        return reservationCounts
            .OrderBy(x => x.Value)
            .FirstOrDefault().Key ?? throw new NotFoundException($"No waiters available for location ID: {locationId} after counting reservations");
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
                    throw new ConflictException(
                        $"You already have reservation booked at location " +
                        $"{locationAddress} during the requested time period.");
                }

                throw new ConflictException(
                    $"Reservation #{request.Id} at location " +
                    $"{locationAddress} is already booked during the requested time period.");
            }
        }
    }
    
    private async Task SendEventToSqs<T>(string eventType, T payload)
    {
        try
        {
            var messageBody = JsonSerializer.Serialize(new
            {
                eventType,
                payload
            });
    
            var sendMessageRequest = new SendMessageRequest
            {
                QueueUrl = awsSettings.Value.SqsQueueUrl,
                MessageBody = messageBody
            };
    
            await amazonSqsClient.SendMessageAsync(sendMessageRequest);
        }
        catch (AmazonSQSException ex)
        {
            throw new ConflictException($"Error communicating with SQS: {ex.Message}");
        }
    } 
    #endregion

    #region Helper Methods For Available Tables
    private IEnumerable<AvailableTableDto> CalculateAvailableSlots(
    IEnumerable<RestaurantTable> tables,
    IEnumerable<Reservation> reservations,
    string? requestedTime)
    {
        var result = new List<AvailableTableDto>();

        foreach (var table in tables)
        {
            var allTimeSlots = Utils.GeneratePredefinedTimeSlots();
            var tableReservations = reservations
                .Where(r => r.TableId == table.Id)
                .ToList();
            var availableSlots = FilterAvailableTimeSlots(allTimeSlots, tableReservations);

            if (!string.IsNullOrEmpty(requestedTime))
            {
                availableSlots = FilterSlotsByRequestedTime(availableSlots, requestedTime);

                if (!availableSlots.Any())
                {
                    continue;
                }
            }

            result.Add(new AvailableTableDto
            {
                TableId = table.Id,
                TableNumber = table.TableNumber,
                Capacity = table.Capacity.ToString(),
                LocationId = table.LocationId,
                LocationAddress = table.LocationAddress,
                AvailableSlots = availableSlots
            });
        }

        return result;
    }

    private List<TimeSlot> FilterAvailableTimeSlots(List<TimeSlot> allSlots, IEnumerable<Reservation> reservations)
    {
        var availableSlots = new List<TimeSlot>();

        foreach (var slot in allSlots)
        {
            var slotStartTime = TimeSpan.ParseExact(slot.Start, "hh\\:mm", CultureInfo.InvariantCulture);
            var slotEndTime = TimeSpan.ParseExact(slot.End, "hh\\:mm", CultureInfo.InvariantCulture);

            var isAvailable = true;

            foreach (var reservation in reservations)
            {
                var reservationStart =
                    TimeSpan.ParseExact(reservation.TimeFrom, "hh\\:mm", CultureInfo.InvariantCulture);
                var reservationEnd = TimeSpan.ParseExact(reservation.TimeTo, "hh\\:mm", CultureInfo.InvariantCulture);

                if (slotStartTime >= reservationEnd || slotEndTime <= reservationStart) continue;
                isAvailable = false;
                break;
            }

            if (isAvailable)
            {
                availableSlots.Add(slot);
            }
        }

        return availableSlots;
    }

    private List<TimeSlot> FilterSlotsByRequestedTime(List<TimeSlot> availableSlots, string requestedTime)
    {
        var requestedTimeSpan = TimeSpan.ParseExact(requestedTime, "hh\\:mm", CultureInfo.InvariantCulture);

        foreach (var slot in availableSlots)
        {
            var slotTime = TimeSpan.ParseExact(slot.Start, "hh\\:mm", CultureInfo.InvariantCulture);
            var slotEndTime = TimeSpan.ParseExact(slot.End, "hh\\:mm", CultureInfo.InvariantCulture);

            // If the requested time falls within this slot's duration
            if (requestedTimeSpan >= slotTime && requestedTimeSpan <= slotEndTime)
            {
                // Return only this slot
                return [slot];
            }
        }

        // If no exact match is found, check for the nearest slot within a 15-minute window
        var nearestSlot = availableSlots
            .Where(slot =>
            {
                var slotTime = TimeSpan.ParseExact(slot.Start, "hh\\:mm", CultureInfo.InvariantCulture);
                var timeDifference = Math.Abs((slotTime - requestedTimeSpan).TotalMinutes);
                return timeDifference <= 15; // Check if the slot is within 15 minutes of the requested time
            }).MinBy(slot =>
                Math.Abs((TimeSpan.ParseExact(slot.Start, "hh\\:mm", CultureInfo.InvariantCulture) - requestedTimeSpan)
                    .Ticks));

        // If the nearest slot is found, return it, otherwise return empty list
        return nearestSlot != null ? [nearestSlot] : [];
    }
    #endregion
}
