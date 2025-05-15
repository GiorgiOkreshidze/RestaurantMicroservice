
namespace Restaurant.Application.DTOs.PerOrders.Request
{
    public class UpsertPreOrderRequest
    {
        public string? Id { get; set; } //Optional for updates, null for new orders
        
        public string ReservationId { get; set; } = string.Empty;
        
        public string Status { get; set; } = string.Empty;
        
        public List<DishItemRequest> DishItems { get; set; } = [];
    }
}
