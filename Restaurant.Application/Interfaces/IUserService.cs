using Restaurant.Application.DTOs.Users;

namespace Restaurant.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> GetUserByIdAsync(string id);
        
        Task<List<UserDto>> GetAllUsersAsync();
        
        Task UpdatePasswordAsync(string userId, UpdatePasswordRequest request);
        
        Task UpdateProfileAsync(string userId, UpdateProfileRequest request);
    }
}
