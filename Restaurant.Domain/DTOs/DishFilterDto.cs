using Restaurant.Domain.Entities.Enums;

namespace Restaurant.Domain.DTOs;

public class DishFilterDto
{
    public DishType? DishType { get; set; }
    
    public DishSortBy? SortBy { get; set; }

    public SortDirection? SortDirection { get; set; }
}