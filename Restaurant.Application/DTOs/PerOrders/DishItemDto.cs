using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.DTOs.PerOrders
{
    public class DishItemDto
    {
        public required string DishId { get; set; }
        public required string DishImageUrl { get; set; }
        public required string DishName { get; set; }
        public required decimal DishPrice { get; set; }
        public int DishQuantity { get; set; }
    }
}
