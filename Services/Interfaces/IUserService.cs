namespace messenger.Services.Interfaces;

using messenger.DTOs;
public interface IUserService
{
    Task<IEnumerable<UsersDTO>> GetAllAsync();

    Task<UsersDTO> CreateUserAsync(UsersDTO userDto);
    Task<UsersDTO?> GetByEmailAsync(SearchUserDTO searchUserDTO);
}
