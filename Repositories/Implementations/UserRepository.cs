using Microsoft.EntityFrameworkCore;
using messenger.Data;
using messenger.Models;
using messenger.Repositories.Interfaces;


namespace messenger.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly MessengerContext _context;

        public UserRepository(MessengerContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Users>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<Users?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<Users?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
               .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<Users> CreateUserAsync(Users user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }


    }
}