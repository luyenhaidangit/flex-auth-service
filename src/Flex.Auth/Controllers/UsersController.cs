using Flex.Auth.Models.Users;
using Flex.Auth.Services.Interfaces;
using Flex.Infrastructures.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Flex.Auth.Controllers
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
        /// Get all users.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            ThreadPool.GetMinThreads(out int worker, out int io);
            ThreadPool.GetMaxThreads(out int maxWorker, out int maxIO);

            var users = await _userService.GetAllAsync();
            return Ok(Result.Success(users));
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
