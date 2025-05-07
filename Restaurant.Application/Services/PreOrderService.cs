using System.Globalization;
using AutoMapper;
using Restaurant.Application.DTOs.PerOrders;
using Restaurant.Application.DTOs.PerOrders.Request;
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
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
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
        await ValidateDishItems(request.DishItems);
        
        var existingPreOrder = await GetAndValidateExistingPreOrder(userId, request);
        var totalPrice = Utils.CalculateTotalPrice(
            request.DishItems, 
            d => d.DishPrice,
            d => d.DishQuantity);

        var preOrder = CreatePreOrderEntity(userId, request, existingPreOrder, totalPrice);
        PopulatePreOrderItems(userId, preOrder, request.DishItems);
        
        if (existingPreOrder != null)
        {
            await preOrderRepository.UpdatePreOrderAsync(preOrder);
        }
        else
        {
            await preOrderRepository.CreatePreOrderAsync(preOrder);
        }
        
        // Send confirmation email for submitted orders
        await SendConfirmationEmailIfSubmitted(userId, preOrder.PreOrderId!, request);
    
        // Return updated cart
        return await GetUserCart(userId);
    }
    
    private static void ValidateInputParameters(string userId, UpsertPreOrderRequest request)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
    
        if (string.IsNullOrEmpty(request.ReservationId))
            throw new ArgumentException("Reservation ID cannot be null or empty");
    
        if (request.DishItems == null || !request.DishItems.Any())
            throw new ArgumentException("Order must contain at least one dish item");
    }
    
    private async Task<PreOrder?> GetAndValidateExistingPreOrder(string userId, UpsertPreOrderRequest request)
    {
        if (string.IsNullOrEmpty(request.Id))
            return null;
        
        var existingPreOrder = await preOrderRepository.GetPreOrderByIdAsync(userId, request.Id);
    
        if (existingPreOrder == null)
            throw new InvalidOperationException($"Pre-order with ID {request.Id} does not exist");

        if (IsWithinCutoffTime(existingPreOrder.TimeSlot, existingPreOrder.ReservationDate))
            throw new InvalidOperationException("Cannot modify pre-order within 30 minutes of reservation time");
        
        return existingPreOrder;
    }
    
    private async Task ValidateReservation(string reservationId)
    {
        bool reservationExists = await reservationRepository.ReservationExistsAsync(reservationId);
        if (!reservationExists)
            throw new InvalidOperationException($"Reservation with ID {reservationId} does not exist");
    }
    
    private async Task ValidateDishItems(List<DishItemDto> dishItems)
    {
        var dishIds = dishItems.Select(item => item.DishId).ToList();
        var existingDishes = await dishRepository.GetDishesByIdsAsync(dishIds);
    
        var invalidDishIds = dishIds.Except(existingDishes.Select(d => d.Id)).ToList();
        if (invalidDishIds.Any())
            throw new InvalidOperationException($"The following dish IDs do not exist: {string.Join(", ", invalidDishIds)}");
    }
    
    private PreOrder CreatePreOrderEntity(string userId, UpsertPreOrderRequest request, PreOrder? existingPreOrder, decimal totalPrice)
    {
        if (existingPreOrder != null)
        {
            return new PreOrder
            {
                UserId = userId,
                SortKey = existingPreOrder.SortKey,
                ReservationId = request.ReservationId,
                Status = request.Status,
                TimeSlot = request.TimeSlot,
                Address = request.Address,
                ReservationDate = request.ReservationDate,
                PreOrderId = existingPreOrder.PreOrderId,
                CreateDate = existingPreOrder.CreateDate,
                TotalPrice = totalPrice,
                Items = new List<PreOrderItem>()
            };
        }
    
        var preOrderId = Guid.NewGuid().ToString("N");
        return new PreOrder
        {
            UserId = userId,
            PreOrderId = preOrderId,
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
    
    private void PopulatePreOrderItems(string userId, PreOrder preOrder, List<DishItemDto> dishItems)
    {
        foreach (var item in dishItems)
        {
            var preOrderItem = new PreOrderItem
            {
                UserId = userId,
                SortKey = $"PreOrder#{preOrder.PreOrderId}#{Guid.NewGuid()}",
                DishId = item.DishId,
                DishName = item.DishName,
                DishImageUrl = item.DishImageUrl,
                Price = item.DishPrice,
                Quantity = item.DishQuantity
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
                throw new InvalidOperationException($"PreOrder with ID {preOrderId} does not exist");
            }
        }
    }
    
    private static bool IsWithinCutoffTime(string timeSlot, string reservationDate)
    {
        if (!DateTime.TryParse(reservationDate, CultureInfo.InvariantCulture,  out var date))
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
