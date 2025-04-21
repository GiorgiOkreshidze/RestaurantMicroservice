using AutoMapper;
using Restaurant.Application.DTOs.Tables;
using Restaurant.Domain.Entities;

namespace Restaurant.Application.Profiles;

public class RestaurantTableProfile : Profile
{
    public RestaurantTableProfile()
    {
        CreateMap<RestaurantTable, RestaurantTableDto>().ReverseMap();
    } 
}