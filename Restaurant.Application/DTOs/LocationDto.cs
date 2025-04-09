namespace Restaurant.Application.DTOs
{
    public class LocationDto
    {
        public string Id { get; set; }
        public string Address { get; set; }
        public double AverageOccupancy { get; set; }
        public double Rating { get; set; }
        public int TotalCapacity { get; set; }
        public string ImageUrl { get; set; }
        public string Description { get; set; }
    }
}