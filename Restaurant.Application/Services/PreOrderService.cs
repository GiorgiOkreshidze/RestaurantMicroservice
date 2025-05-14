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
        await ValidateReservation(request.ReservationId);

        if (request.DishItems.Count != 0)
        {
            await ValidateDishItems(request.DishItems);
        }

        var existingPreOrder = await GetAndValidateExistingPreOrder(userId, request);
        var totalPrice = Utils.CalculateTotalPrice(
            request.DishItems,
            d => d.DishPrice,
            d => d.DishQuantity);

        var preOrder = CreatePreOrderEntity(userId, request, existingPreOrder, totalPrice);
        PopulatePreOrderItems(preOrder, request.DishItems);

        if (existingPreOrder != null)
        {
            await preOrderRepository.UpdatePreOrderAsync(preOrder);
        }
        else
        {
            await preOrderRepository.CreatePreOrderAsync(preOrder);
        }

        // Send confirmation email for submitted orders
        await SendConfirmationEmailIfSubmitted(userId, preOrder.Id!, request);

        // Return updated cart
        return await GetUserCart(userId);
    }

    public async Task<PreOrderDishesDto> GetPreOrderDishes(string preOrderId)
    {
        if (string.IsNullOrEmpty(preOrderId))
            throw new BadRequestException("PreOrder ID cannot be null or empty");

        await ValidatePreOrderId(preOrderId);

        var preOrderItems = await preOrderRepository.GetPreOrderItemsAsync(preOrderId);

        var result = mapper.Map<PreOrderDishesDto>(preOrderItems);
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
    
        var validStatuses = new[] { "New", "Submitted", "Cancelled", "Finished" };
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

    private async Task ValidateReservation(string reservationId)
    {
        bool reservationExists = await reservationRepository.ReservationExistsAsync(reservationId);
        if (!reservationExists)
            throw new NotFoundException($"Reservation with ID {reservationId} does not exist");
    }

    private async Task ValidateDishItems(List<DishItemDto> dishItems)
    {
        var emptyDishIds = dishItems.Where(item => string.IsNullOrEmpty(item.DishId)).ToList();
        if (emptyDishIds.Any())
            throw new BadRequestException("DishId should not be empty");
        
        var dishIds = dishItems.Select(item => item.DishId).ToList();
        var existingDishes = await dishRepository.GetDishesByIdsAsync(dishIds);

        var invalidDishIds = dishIds.Except(existingDishes.Select(d => d.Id)).ToList();
        if (invalidDishIds.Any())
            throw new NotFoundException(
                $"The following dish IDs do not exist: {string.Join(", ", invalidDishIds)}");
    }

    private PreOrder CreatePreOrderEntity(string userId, UpsertPreOrderRequest request, PreOrder? existingPreOrder,
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
                TimeSlot = request.TimeSlot,
                Address = request.Address,
                ReservationDate = request.ReservationDate,
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
            TimeSlot = request.TimeSlot,
            Address = request.Address,
            ReservationDate = request.ReservationDate,
            CreateDate = DateTime.UtcNow,
            TotalPrice = totalPrice,
            Items = new List<PreOrderItem>()
        };
    }

    private void PopulatePreOrderItems(PreOrder preOrder, List<DishItemDto> dishItems)
    {
        foreach (var item in dishItems)
        {
            var preOrderItem = new PreOrderItem
            {
                Id = Guid.NewGuid()
                    .ToString("N"),
                DishId = item.DishId,
                DishName = item.DishName,
                DishImageUrl = item.DishImageUrl,
                Price = item.DishPrice,
                Quantity = item.DishQuantity,
            };
            preOrder.Items.Add(preOrderItem);
        }
    }

    private async Task SendConfirmationEmailIfSubmitted(string userId, string preOrderId, UpsertPreOrderRequest request)
    {
        if (request.Status == "Submitted")
        {
            var preOrder = await preOrderRepository.GetPreOrderByIdAsync(userId, preOrderId);
            if (preOrder is not null)
            {
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
}