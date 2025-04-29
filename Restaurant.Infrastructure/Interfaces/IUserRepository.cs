using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(string id);
        
        Task<List<User>> GetAllUsersAsync();
        
        Task<User?> GetUserByEmailAsync(string email);
        
        Task<string> SignupAsync(User user);
        
        Task<bool> DoesEmailExistAsync(string email);
        
        Task UpdatePasswordAsync(string userId, string newPasswordHash);
    }
}
