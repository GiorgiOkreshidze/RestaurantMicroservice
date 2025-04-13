using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Interfaces
{
    public interface IUserRepository
    {
        Task<string> SignupAsync(User user); // Returns the user ID
        Task<bool> DoesEmailExistAsync(string email);
    }
}
