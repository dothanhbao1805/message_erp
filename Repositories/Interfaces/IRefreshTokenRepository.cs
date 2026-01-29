using messenger.Models;

namespace messenger.Repositories.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task<RefreshToken> CreateAsync(RefreshToken refreshToken);
        Task<bool> RevokeAsync(string token);
        Task<bool> RevokeAllUserTokensAsync(int Id);
        Task<List<RefreshToken>> GetActiveTokensByIdAsync(int Id);
    }
}