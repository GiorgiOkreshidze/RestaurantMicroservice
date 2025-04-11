using AutoMapper;
using Restaurant.Application.DTOs.Locations;
using Restaurant.Domain.Entities;

namespace Restaurant.Application.Profiles
{
    public class LocationProfile : Profile
    {
        public LocationProfile()
        {
            CreateMap<Location, LocationDto>().ReverseMap();
            CreateMap<Location, LocationSelectOptionDto>();
        }
    }
}