// Services/Implementations/AuthService.cs
using messenger.DTOs;
using messenger.Models;
using messenger.Repositories.Interfaces;
using messenger.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using AutoMapper;

namespace messenger.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher<Users> _passwordHasher;
        private readonly IMapper _mapper;
        private readonly IJwtService _jwtService;

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher<Users> passwordHasher,
            IMapper mapper,
            IJwtService jwtService)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _mapper = mapper;
            _jwtService = jwtService;
        }

        public async Task<AuthResponseDTO> RegisterAsync(RegisterDTO registerDto)
        {
            if (await _userRepository.EmailExistsAsync(registerDto.Email))
            {
                return new AuthResponseDTO
                {
                    Success = false,
                    Message = "Email đã được sử dụng"
                };
            }

            var user = new Users
            {
                FullName = registerDto.FullName,
                Email = registerDto.Email,
                Password_Hash = string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Role = "User"
            };

            user.Password_Hash = _passwordHasher.HashPassword(user, registerDto.Password);
            var createdUser = await _userRepository.CreateUserAsync(user);

            var userDto = _mapper.Map<UsersDTO>(createdUser);
            var token = _jwtService.GenerateToken(createdUser);
            var refreshToken = await CreateRefreshTokenAsync(createdUser.Id); // ← Đổi từ UserId thành Id

            return new AuthResponseDTO
            {
                Success = true,
                Message = "Đăng ký thành công",
                User = userDto,
                Token = token,
                RefreshToken = refreshToken
            };
        }

        public async Task<AuthResponseDTO> LoginAsync(LoginDTO loginDto)
        {
            var user = await _userRepository.GetUserByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return new AuthResponseDTO
                {
                    Success = false,
                    Message = "Email hoặc mật khẩu không đúng"
                };
            }

            var passwordHash = _passwordHasher.VerifyHashedPassword(
                user, user.Password_Hash, loginDto.Password);

            if (passwordHash == PasswordVerificationResult.Failed)
            {
                return new AuthResponseDTO
                {
                    Success = false,
                    Message = "Email hoặc mật khẩu không đúng"
                };
            }

            var userDto = _mapper.Map<UsersDTO>(user);
            var token = _jwtService.GenerateToken(user);
            var refreshToken = await CreateRefreshTokenAsync(user.Id); // ← Đổi từ UserId thành Id

            return new AuthResponseDTO
            {
                Success = true,
                Message = "Đăng nhập thành công",
                User = userDto,
                Token = token,
                RefreshToken = refreshToken
            };
        }

        public async Task<AuthResponseDTO> RefreshTokenAsync(RefreshTokenDTO refreshTokenDto)
        {
            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(refreshTokenDto.RefreshToken);

            if (refreshToken == null)
            {
                return new AuthResponseDTO
                {
                    Success = false,
                    Message = "Refresh token không hợp lệ hoặc đã hết hạn"
                };
            }

            await _refreshTokenRepository.RevokeAsync(refreshTokenDto.RefreshToken);

            var user = refreshToken.User;
            var newAccessToken = _jwtService.GenerateToken(user);
            var newRefreshToken = await CreateRefreshTokenAsync(user.Id); // ← Đổi từ UserId thành Id
            var userDto = _mapper.Map<UsersDTO>(user);

            return new AuthResponseDTO
            {
                Success = true,
                Message = "Token đã được làm mới",
                User = userDto,
                Token = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }

        public async Task<bool> LogoutAsync(LogoutDTO logoutDto)
        {
            return await _refreshTokenRepository.RevokeAsync(logoutDto.RefreshToken);
        }

        public async Task<bool> RevokeAllUserTokensAsync(int userId)
        {
            return await _refreshTokenRepository.RevokeAllUserTokensAsync(userId);
        }

        private async Task<string> CreateRefreshTokenAsync(int userId)
        {
            var refreshToken = new RefreshToken
            {
                UserId = userId,
                Token = _jwtService.GenerateRefreshToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            await _refreshTokenRepository.CreateAsync(refreshToken);
            return refreshToken.Token;
        }
    }
}