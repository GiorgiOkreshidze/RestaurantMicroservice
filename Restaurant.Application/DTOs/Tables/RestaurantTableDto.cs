namespace Restaurant.Application.DTOs.Tables;

public class RestaurantTableDto
{
    public required string Id { get; set; }

    public required string TableNumber { get; set; }

    public required string Capacity { get; set; }

    public required string LocationId { get; set; }

    public required string LocationAddress { get; set; }
}