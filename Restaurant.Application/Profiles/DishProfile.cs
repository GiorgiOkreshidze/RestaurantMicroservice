using AutoMapper;
using Restaurant.Application.DTOs.Dishes;
using Restaurant.Domain.Entities;

namespace Restaurant.Application.Profiles;

public class DishProfile : Profile
{
    public DishProfile()
    {
        CreateMap<Dish, DishDto>().ReverseMap();
        CreateMap<Dish, DishDetailsDto>().ReverseMap();
    }
}
