using Restaurant.Domain.Entities;
using System.Threading.Tasks;

namespace Restaurant.Infrastructure.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetUserByIdAsync(string id);
        Task SaveUserAsync(User user);
    }
}
