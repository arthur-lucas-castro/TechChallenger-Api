using Compartilhado.Application.DTOs;

namespace Compartilhado.Application.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto);
    }
}
