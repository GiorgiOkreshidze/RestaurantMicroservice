using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.DTOs.Locations
{
    public class LocationDishResponseDto
    {
        public required string Id { get; set; }

        public required string Name { get; set; }

        public required decimal Price { get; set; }

        public required string ImageUrl { get; set; }

        public required string Weight { get; set; }
    }
}
