using Flex.Identity.Models.Users;
using Flex.Identity.Services.Interfaces;
using Flex.Infrastructures.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Flex.Identity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ILogger<UsersController> _logger;
        private readonly IUserService _userService;
        public UsersController(ILogger<UsersController> logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        /// <summary>
        /// Create a new user request.
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateUserRequest([FromBody] CreateUserCommand command)
        {
            var id = await _userService.CreateAsync(command);
            return Ok(Result.Success(id));
        }
    }
}
