using System.Threading.Tasks;
using ECommerce.Core.DTOs;

namespace ECommerce.Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RegisterAsync(RegisterUserDto registerUserDto);
    }
}