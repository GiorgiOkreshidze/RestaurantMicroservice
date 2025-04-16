namespace Restaurant.Application.DTOs.Tables
{
    public class FilterParameters
    {
        public string LocationId { get; set; } = string.Empty;

        public string Date { get; set; } = string.Empty;

        public int Guests { get; set; } = 1;

        public string? Time { get; set; }
    }
}
