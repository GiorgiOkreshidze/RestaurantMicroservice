using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Restaurant.Application.DTOs.Locations;

namespace Restaurant.Application.Profiles
{
    public class DishProfile : Profile
    {
        public DishProfile() 
        {
            CreateMap<Domain.Entities.Dish, LocationDishResponseDto>()
                                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => decimal.Parse(src.Price)));
        }
    }
}
