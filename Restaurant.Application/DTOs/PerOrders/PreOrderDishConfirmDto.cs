namespace Restaurant.Application.DTOs.PerOrders;

public class PreOrderDishConfirmDto : PreOrderDto
{
    public required string CustomerName { get; set; } = string.Empty;
    
    public required string TableNumber { get; set; } = string.Empty;
}
