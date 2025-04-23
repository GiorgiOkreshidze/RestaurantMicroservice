using AutoMapper;
using Restaurant.Application.DTOs.PerOrders;
using Restaurant.Application.Interfaces;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Application.Services;

public class PreOrderService(IPreOrderRepository preOrderRepository, IMapper mapper) : IPreOrderService
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
}
