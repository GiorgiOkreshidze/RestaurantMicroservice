namespace Restaurant.Domain.DTOs
{
    public class ReservationsQueryParametersDto
    {
        public string? Date { get; set; }
        public string? TimeFrom { get; set; }
        public string? TableId { get; set; }
    }
}
