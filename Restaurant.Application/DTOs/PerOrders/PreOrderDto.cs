using System;
using System.Collections.Generic;

namespace Restaurant.Application.DTOs.PerOrders
{
    public class PreOrderDto
    {
        public required string Id { get; set; }
        public required string Address { get; set; }
        public DateTime Date { get; set; }
        public required string Status { get; set; }
        public required string TimeSlot { get; set; }
        public required string ReservationId { get; set; }
        public required decimal TotalAmount { get; set; }
        public List<DishItemDto> DishItems { get; set; } = [];
    }
}
