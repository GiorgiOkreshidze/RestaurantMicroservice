
namespace Restaurant.Application.DTOs.PerOrders.Request
{
    public class UpsertPreOrderRequest
    {
        public string? Id { get; set; } //Optional for updates, null for new orders
        
        public string ReservationId { get; set; } = string.Empty;
        
        public string Address { get; set; } = string.Empty;
        
        public string Status { get; set; } = string.Empty;
        
        public string ReservationDate { get; set; } = string.Empty;
        
        public string TimeSlot { get; set; } = string.Empty;
        
        public List<DishItemDto> DishItems { get; set; } = [];
    }
}
