namespace Restaurant.Application.DTOs.PerOrders.Request;

public class UpdatePreOrderDishesStatusRequest
{
    public required string PreOrderId { get; set; }
    
    public required string DishId { get; set; }
    
    public required string DishStatus { get; set; } // Cancelled, Confirmed
}