namespace Restaurant.Application.DTOs.PerOrders.Request;

public class DishItemRequest
{
    public required string DishId { get; set; }
    
    public int DishQuantity { get; set; }
}