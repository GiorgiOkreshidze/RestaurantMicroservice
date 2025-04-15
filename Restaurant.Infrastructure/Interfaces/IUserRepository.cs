using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Interfaces
{
    public interface IUserRepository
    {
        Task<string> SignupAsync(User user);

        Task<bool> DoesEmailExistAsync(string email);

        Task<User?> GetUserByEmailAsync(string email);

        Task<User?> GetUserByIdAsync(string id);
    }
}
