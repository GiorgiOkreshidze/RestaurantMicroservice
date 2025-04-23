using Restaurant.Application.DTOs.PerOrders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Interfaces
{
    public interface IPreOrderService
    {
        public Task<CartDto> GetUserCart(string userId);
    }
}
