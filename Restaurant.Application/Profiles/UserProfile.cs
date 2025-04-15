using Restaurant.Application.DTOs.Users;
using Restaurant.Domain.Entities;
using Profile = AutoMapper.Profile;

namespace Restaurant.Application.Profiles;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>().ReverseMap();
    } 
}