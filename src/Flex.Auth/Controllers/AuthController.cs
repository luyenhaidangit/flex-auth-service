using Flex.Auth.Models.Users;
using Flex.Auth.Services.Interfaces;
using Flex.Infrastructures.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ClaimTypesApp = Flex.Infrastructures.Authentication.ClaimTypes;

namespace Flex.Auth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IAuthService _authService;

        public AuthController(
            ILogger<AuthController> logger,
            IAuthService authService)
        {
            _logger = logger;
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            var login = await _authService.LoginAsync(request, ct);

            var resutl = Result.Success(login);

            return Ok(resutl);
        }

        //[HttpPost("logout")]
        //[Authorize]
        //public async Task<IActionResult> Logout()
        //{
        //    var ok = await _authService.LogoutAsync(User, HttpContext.RequestAborted);

        //    if (!ok)
        //    {
        //        return BadRequest(Result.Failure("Invalid token."));
        //    }
        //    return Ok(Result.Success(message: "Logout success!"));
        //}

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUserInfo()
        {
            var userInfo = await _authService.GetCurrentUserInfoAsync(User, HttpContext.RequestAborted);
            if (userInfo is null)
            {
                if (string.IsNullOrEmpty(User.FindFirstValue(ClaimTypesApp.Sub)))
                {
                    return Unauthorized(Result.Failure(message: "Unauthorized"));
                }

                return BadRequest(Result.Failure(message: "User not found"));
            }

            return Ok(Result.Success(message: "Get info user success!", data: userInfo));
        }
    }
}
