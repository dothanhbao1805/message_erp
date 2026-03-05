using messenger.Services.Interfaces;
using messenger.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace messenger.Controllers.site
{
    [Route("api/user")]
    [ApiController]
    [Authorize(Roles = "User")]
    public class UserController_Client : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;

        public UserController_Client(IUserService userService, IAuthService authService)
        {
            _userService = userService;
            _authService = authService;
        }


        [HttpGet("email/{email}")]
        public async Task<ActionResult<UsersDTO>> GetUserByEmail(string email)
        {
            var user = await _userService.GetByEmailAsync(
                new SearchUserDTO { Email = email }
            );

            if (user == null)
                return NotFound();

            return Ok(user);
        }


    }
}