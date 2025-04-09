using Restaurant.Application.DTOs;
using System.Threading.Tasks;

namespace Restaurant.Application.Interfaces
{
    public interface IAuthService
    {
        Task<bool> RegisterAsync(RegisterDto model);
        Task<string> LoginAsync(LoginDto model);
    }
}
