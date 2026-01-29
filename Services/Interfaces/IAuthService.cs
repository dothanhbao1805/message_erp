using messenger.DTOs;

namespace messenger.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDTO> RegisterAsync(RegisterDTO registerDto);
        Task<AuthResponseDTO> LoginAsync(LoginDTO loginDto);
        Task<AuthResponseDTO> RefreshTokenAsync(RefreshTokenDTO refreshTokenDto);
        Task<bool> LogoutAsync(LogoutDTO logoutDto);
    }
}