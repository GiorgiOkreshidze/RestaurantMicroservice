using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetUserByIdAsync(string id);
        Task SaveUserAsync(User user);
    }
}
