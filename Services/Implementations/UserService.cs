using messenger.Models;
using messenger.DTOs;
using messenger.Repositories.Interfaces;
using messenger.Services.Interfaces;
using AutoMapper;


namespace messenger.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<UsersDTO>> GetAllAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<UsersDTO>>(users);
        }

        public async Task<UsersDTO> CreateUserAsync(UsersDTO userDto)
        {
            var users = await _userRepository.CreateUserAsync(_mapper.Map<Users>(userDto));
            return _mapper.Map<UsersDTO>(users);
        }

    }
}