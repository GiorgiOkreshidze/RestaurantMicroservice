using Restaurant.Application.DTOs.Auth;
using System.Threading.Tasks;

namespace Restaurant.Application.Interfaces
{
    public interface IAuthService
    {
        Task<string> RegisterUserAsync(RegisterDto request);
    }
}
