using AutoMapper;
using Restaurant.Application.DTOs.Users;
using Restaurant.Application.Exceptions;
using Restaurant.Application.Interfaces;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Application.Services;

public class UserService(IUserRepository userRepository, IMapper mapper) : IUserService
{
    public async Task<UserDto> GetUserByIdAsync(string id)
    {
        var user = await userRepository.GetUserByIdAsync(id);
        if (user == null)
        {
            throw new NotFoundException("User", id);
        }
        
        return mapper.Map<UserDto>(user);
    }
}