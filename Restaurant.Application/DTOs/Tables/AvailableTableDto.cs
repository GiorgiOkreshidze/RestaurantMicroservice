using Restaurant.Domain;
using System.Text.Json.Serialization;

namespace Restaurant.Application.DTOs.Tables
{
    public class AvailableTableDto
    {
        public required List<TimeSlot> AvailableSlots { get; set; }

        public required string Capacity { get; set; }

        public required string LocationAddress { get; set; }

        public required string LocationId { get; set; }

        public required string TableId { get; set; }

        public required string TableNumber { get; set; }
    }
}
