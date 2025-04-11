using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Interfaces
{
    public interface IDishRepository
    {
        Task<IEnumerable<Dish>> GetSpecialtyDishesByLocationAsync(string locationId);
    }
}
