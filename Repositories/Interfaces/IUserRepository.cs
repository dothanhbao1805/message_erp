namespace messenger.Repositories.Interfaces;

using messenger.Models;
public interface IUserRepository
{
    Task<IEnumerable<Users>> GetAllAsync();
    Task<Users?> GetUserByIdAsync(int id);
    Task<Users?> GetUserByEmailAsync(string email);
    Task<Users> CreateUserAsync(Users user);
    Task<bool> EmailExistsAsync(string email);
}