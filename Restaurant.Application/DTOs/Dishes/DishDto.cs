namespace Restaurant.Application.DTOs.Dishes;

public class DishDto
{
    public required string Id { get; set; }
    
    public required string Name { get; set; }
    
    public required string Price { get; set; }
    
    public required string Weight { get; set; }
    
    public required string ImageUrl { get; set; }
    
    public string? DishType { get; set; }
    
    public string? State { get; set; }
    
    public bool IsPopular { get; set; }
}

