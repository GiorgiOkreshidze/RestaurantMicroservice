using Restaurant.Application.DTOs.PerOrders;
using Restaurant.Application.DTOs.PerOrders.Request;

namespace Restaurant.Application.Interfaces
{
    public interface IPreOrderService
    {
        public Task<CartDto> GetUserCart(string userId);

        public Task<CartDto> UpsertPreOrder(string userId, UpsertPreOrderRequest request);
    }
}
