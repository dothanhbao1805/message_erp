using messenger.Models;
using System.Security.Claims;

namespace messenger.Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(Users user);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}