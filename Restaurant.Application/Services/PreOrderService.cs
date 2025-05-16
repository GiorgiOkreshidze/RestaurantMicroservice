using System.Globalization;
using AutoMapper;
using Restaurant.Application.DTOs.PerOrders;
using Restaurant.Application.DTOs.PerOrders.Request;
using Restaurant.Application.Exceptions;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Application.Services;

public class PreOrderService(
    IPreOrderRepository preOrderRepository,
    IMapper mapper,
    IReservationRepository reservationRepository,
    IDishRepository dishRepository,
    IEmailService emailService) : IPreOrderService
{
    public async Task<CartDto> GetUserCart(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new BadRequestException("User ID cannot be null or empty");
        }

        var preOrders = await preOrderRepository.GetPreOrdersAsync(userId);

        // Handle null result from repository explicitly
        if (preOrders == null)
        {
            return new CartDto
            {
                Content = [],
                IsEmpty = true
            };
        }

        var result = mapper.Map<CartDto>(preOrders);

        return result;
    }

    public async Task<CartDto> UpsertPreOrder(string userId, UpsertPreOrderRequest request)
    {
        ValidateInputParameters(userId, request);
        var reservationId = request.ReservationId;
        var reservation = await reservationRepository.GetReservationByIdAsync(reservationId) ?? throw new NotFoundException($"Reservation with ID {reservationId} does not exist");
        if (string.IsNullOrEmpty(request.Id))
        {
            await ValidateNoExistingPreOrderForReservation(reservationId);
        }
       
        var existingPreOrder = await GetAndValidateExistingPreOrder(userId, request);
        var dishes =  await ValidateAndGetDishItems(request.DishItems);
        var totalPrice = ValidateAndCalculateTotalPrice(request.DishItems, dishes);
        var preOrder = CreatePreOrderEntity(userId, request, reservation, existingPreOrder, totalPrice);
        PopulatePreOrderItems(preOrder, request.DishItems, dishes);

        if (existingPreOrder != null)
        {
            await preOrderRepository.UpdatePreOrderAsync(preOrder);
        }
        else
        {
            await preOrderRepository.CreatePreOrderAsync(preOrder);
        }

        await SendConfirmationEmailIfSubmitted(userId, preOrder.Id!, request);
        
        var cart = await GetUserCart(userId);
        return cart;
    }
    
    public async Task<PreOrderDishConfirmDto> GetPreOrderDishes(string reservationId)
    {
        if (string.IsNullOrEmpty(reservationId))
            throw new BadRequestException("PreOrder ID cannot be null or empty");

        var reservation = await reservationRepository.GetReservationByIdAsync(reservationId) 
                          ?? throw new NotFoundException("Reservation", reservationId);
        var preOrder = await preOrderRepository.GetPreOrderByReservationIdAsync(reservationId)
                       ?? throw new NotFoundException("PreOrder for reservation", reservationId);
        
        if (preOrder.Status != "Submitted")
            throw new BadRequestException("Only submitted pre-orders can be viewed by waiters.");
        
        var result = mapper.Map<PreOrderDishConfirmDto>(preOrder);
        result.CustomerName = reservation.UserInfo;
        result.TableNumber = reservation.TableNumber;
        
        return result;
    }

    public async Task UpdatePreOrderDishesStatus(UpdatePreOrderDishesStatusRequest request)
    {
        await ValidatePreOrderId(request.PreOrderId);
        await ValidateDishId(request.DishId);
        ValidateDishStatus(request.DishStatus);

        if (string.IsNullOrEmpty(request.DishId))
            throw new BadRequestException("Dish ID cannot be null or empty");

        await preOrderRepository.UpdatePreOrderDishesStatusAsync(request.PreOrderId, request.DishId, request.DishStatus);
        await UpdatePreOrderTotalPrice(request.PreOrderId);
        await UpdateReservationPreOrderCount(request.PreOrderId);
    }

    private async Task UpdatePreOrderTotalPrice(string preOrderId)
    {
        var preOrder = await preOrderRepository.GetPreOrderOnlyByIdAsync(preOrderId) 
                       ?? throw new NotFoundException("PreOrder", preOrderId);
    
        // Calculate total price only for non-cancelled dishes
        decimal newTotalPrice = preOrder.Items
            .Where(item => item.DishStatus != "Cancelled")
            .Sum(item => item.Price * item.Quantity);
    
        // Update the PreOrder with new total price
        preOrder.TotalPrice = newTotalPrice;
    
        // Save the updated PreOrder
        await preOrderRepository.UpdatePreOrderAsync(preOrder);
    }

    private async Task ValidatePreOrderId(string preOrderId)
    {
        if (string.IsNullOrEmpty(preOrderId))
            throw new BadRequestException("PreOrder ID cannot be null or empty");

        var preOrder = await preOrderRepository.GetPreOrderOnlyByIdAsync(preOrderId);
        if (preOrder is null)
            throw new NotFoundException($"PreOrder with ID {preOrderId} does not exist");
    }
    
    private async Task ValidateDishId(string dishId)
    {
        if (string.IsNullOrEmpty(dishId))
            throw new BadRequestException("Dish ID cannot be null or empty");

        var dish = await dishRepository.GetDishByIdAsync(dishId);
        if (dish is null)
            throw new NotFoundException($"dish with ID {dishId} does not exist");
    }
    
    private static void ValidateDishStatus(string dishStatus)
    {
        var validStatuses = new[] { "Cancelled", "Confirmed" };
        if (string.IsNullOrEmpty(dishStatus))
            throw new BadRequestException("Dish status cannot be null or empty");
        if (!validStatuses.Contains(dishStatus))
            throw new BadRequestException($"Invalid dish status '{dishStatus}'. Status must be one of: {string.Join(", ", validStatuses)}");
    }

    private static void ValidateInputParameters(string userId, UpsertPreOrderRequest request)
    {
        if (string.IsNullOrEmpty(userId))
            throw new BadRequestException("User ID cannot be null or empty");

        if (string.IsNullOrEmpty(request.ReservationId))
            throw new BadRequestException("Reservation ID cannot be null or empty");
    
        var validStatuses = new[] {"New", "Submitted", "Cancelled" };
        if (string.IsNullOrEmpty(request.Status))
            throw new BadRequestException("Status cannot be null or empty");
        if (!validStatuses.Contains(request.Status))
            throw new BadRequestException($"Invalid status '{request.Status}'. Status must be one of: {string.Join(", ", validStatuses)}");
    }

    private async Task<PreOrder?> GetAndValidateExistingPreOrder(string userId, UpsertPreOrderRequest request)
    {
        if (string.IsNullOrEmpty(request.Id))
            return null;

        var existingPreOrder = await preOrderRepository.GetPreOrderByIdAsync(userId, request.Id);

        if (existingPreOrder == null)
            throw new NotFoundException($"Pre-order with ID {request.Id} does not exist");

        if (IsWithinCutoffTime(existingPreOrder.TimeSlot, existingPreOrder.ReservationDate))
            throw new BadRequestException("PreOrder can only be modified before 30 minutes of start time");

        return existingPreOrder;
    }

    private async Task<IEnumerable<Dish>> ValidateAndGetDishItems(List<DishItemRequest> dishItems)
    {
        var emptyDishIds = dishItems.Where(item => string.IsNullOrEmpty(item.DishId)).ToList();
        if (emptyDishIds.Any())
            throw new BadRequestException("DishId should not be empty");
    
        var dishIds = dishItems.Select(item => item.DishId).ToList();
        var existingDishes = await dishRepository.GetDishesByIdsAsync(dishIds);
    
        // Convert existingDishes to a list to prevent multiple enumeration
        var existingDishesList = existingDishes.ToList();

        var invalidDishIds = dishIds.Except(existingDishesList.Select(d => d.Id)).ToList();
        if (invalidDishIds.Any())
            throw new NotFoundException(
                $"The following dish IDs do not exist: {string.Join(", ", invalidDishIds)}");
    
        return existingDishesList; 
    }

    private PreOrder CreatePreOrderEntity(string userId, UpsertPreOrderRequest request, Reservation reservation, PreOrder? existingPreOrder,
        decimal totalPrice)
    {
        if (existingPreOrder != null)
        {
            return new PreOrder
            {
                UserId = userId,
                Id = existingPreOrder.Id,
                ReservationId = request.ReservationId,
                Status = request.Status,
                TimeSlot = reservation.TimeSlot,
                Address = reservation.LocationAddress,
                ReservationDate = reservation.Date,
                CreateDate = existingPreOrder.CreateDate,
                TotalPrice = totalPrice,
                Items = new List<PreOrderItem>()
            };
        }

        var preOrderId = Guid.NewGuid().ToString("N");
        return new PreOrder
        {
            UserId = userId,
            Id = preOrderId,
            ReservationId = request.ReservationId,
            Status = request.Status,
            TimeSlot = reservation.TimeSlot,
            Address = reservation.LocationAddress,
            ReservationDate = reservation.Date,
            CreateDate = DateTime.UtcNow,
            TotalPrice = totalPrice,
            Items = new List<PreOrderItem>()
        };
    }

    private void PopulatePreOrderItems(PreOrder preOrder, List<DishItemRequest> dishItems, IEnumerable<Dish> dishes)
    {
        var dishMap = dishes.ToDictionary(d => d.Id);

        foreach (var request in dishItems)
        {
            if (dishMap.TryGetValue(request.DishId, out var dish))
            {
                var preOrderItem = new PreOrderItem
                {
                    Id = Guid.NewGuid().ToString("N"),
                    DishId = dish.Id,
                    DishName = dish.Name,
                    DishImageUrl = dish.ImageUrl,
                    DishStatus = "New",
                    Price = dish.Price,
                    Quantity = request.DishQuantity
                };
            
                preOrder.Items.Add(preOrderItem);
            }
        }
    }

    private async Task SendConfirmationEmailIfSubmitted(string userId, string preOrderId, UpsertPreOrderRequest request)
    {
        if (request.Status == "Submitted")
        {
            var preOrder = await preOrderRepository.GetPreOrderByIdAsync(userId, preOrderId);
            if (preOrder is not null)
            {
                await UpdateReservationPreOrderCount(preOrder.Id!);
                await emailService.SendPreOrderConfirmationEmailAsync(userId, preOrder);
            }
            else
            {
                throw new NotFoundException($"PreOrder with ID {preOrderId} does not exist");
            }
        }
    }

    private static bool IsWithinCutoffTime(string timeSlot, string reservationDate)
    {
        if (!DateTime.TryParse(reservationDate, CultureInfo.InvariantCulture, out var date))
            return false;

        var startTimeStr = timeSlot.Split('-')[0].Trim();
        if (!TimeSpan.TryParse(startTimeStr, CultureInfo.InvariantCulture, out var startTime))
            return false;

        var reservationTime = date.Date.Add(startTime);

        var cutOffMinutes = 30;
        var cutoffTime = reservationTime.AddMinutes(-cutOffMinutes);

        // Check if current time is past the cutoff
        return DateTime.Now >= cutoffTime;
    }
    
    private async Task ValidateNoExistingPreOrderForReservation(string reservationId)
    {
        var existingPreOrderForReservation = await preOrderRepository.GetPreOrderByReservationIdAsync(reservationId);
        if (existingPreOrderForReservation != null)
        {
            throw new ConflictException($"A preorder with ID {existingPreOrderForReservation.Id} already exists for reservation ID {reservationId}. Please update the existing preorder instead of creating a new one.");
        }
    }
    
    private async Task UpdateReservationPreOrderCount(string preOrderId)
    {
        var preorder = await preOrderRepository.GetPreOrderOnlyByIdAsync(preOrderId) 
                       ?? throw new NotFoundException("PreOrder", preOrderId);
    
        var reservation = await reservationRepository.GetReservationByIdAsync(preorder.ReservationId) 
                          ?? throw new NotFoundException($"Reservation with ID {preorder.ReservationId} does not exist");
    
        reservation.PreOrder = preorder.Items
            .Where(i => i.DishStatus != "Cancelled")
            .Sum(i => i.Quantity)
            .ToString();
    
        await reservationRepository.UpsertReservationAsync(reservation);
    }
    
    private decimal ValidateAndCalculateTotalPrice(IEnumerable<DishItemRequest> dishItems, IEnumerable<Dish> dishes)
    {
        var dishPriceMap = dishes.ToDictionary(d => d.Id, d => d.Price);
    
        var invalidItems = dishItems.Where(item => item.DishQuantity <= 0).ToList();
        if (invalidItems.Any())
        {
            var invalidDishIds = string.Join(", ", invalidItems.Select(item => item.DishId));
            throw new BadRequestException($"Dish quantities must be greater than zero. Invalid quantities found for dishes: {invalidDishIds}");
        }
    
        return dishItems.Sum(item => 
            dishPriceMap.TryGetValue(item.DishId, out var price) 
                ? price * item.DishQuantity 
                : 0);
    }
}