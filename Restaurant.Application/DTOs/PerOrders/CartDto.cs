using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.DTOs.PerOrders
{
    public class CartDto
    {
        public List<PreOrderDto> Content { get; set; } = [];

        /// <summary>
        /// Indicates whether the cart is empty
        /// </summary>
        public bool IsEmpty { get; set; }
    }
}