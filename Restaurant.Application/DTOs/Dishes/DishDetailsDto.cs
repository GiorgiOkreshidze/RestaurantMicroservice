namespace Restaurant.Application.DTOs.Dishes;

public class DishDetailsDto : DishDto
{
    public string? Calories { get; set; }
    
    public string? Carbohydrates { get; set; }
    
    public string? Description { get; set; }
    
    public string? Fats { get; set; }

    public string? Proteins { get; set; }

    public string? Vitamins { get; set; }
}