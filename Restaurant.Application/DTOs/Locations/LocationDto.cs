namespace Restaurant.Application.DTOs.Locations
{
    public class LocationDto
    {
        public required string Id { get; set; }
        public required string Address { get; set; }
        public required double AverageOccupancy { get; set; }
        public required double Rating { get; set; }
        public required int TotalCapacity { get; set; }
        public required string ImageUrl { get; set; }
        public required string Description { get; set; }
    }
}