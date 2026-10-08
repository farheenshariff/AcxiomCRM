using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api
{
    [Route("api/auth")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogService _auditLogService;

        public AuthApiController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IAuditLogService auditLogService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditLogService = auditLogService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<AuthResponseDto>.Fail("Invalid login payload.", errors));
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !user.IsActive)
            {
                await _auditLogService.LogAsync("ApiLogin", "User", null, null, null, HttpContext.Connection.RemoteIpAddress?.ToString(), "Failure", $"Invalid attempt for {model.Email}");
                return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Invalid credentials or account is disabled."));
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var userDto = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Role = roles.FirstOrDefault() ?? "SalesExecutive",
                    IsActive = user.IsActive,
                    CreatedDate = user.CreatedDate
                };

                await _auditLogService.LogAsync("ApiLogin", "User", user.Id, null, null, HttpContext.Connection.RemoteIpAddress?.ToString(), "Success", $"API login succeeded for {user.Email}");

                return Ok(ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
                {
                    IsAuthenticated = true,
                    Message = "Login successful.",
                    User = userDto
                }, "Login successful."));
            }

            if (result.IsLockedOut)
            {
                await _auditLogService.LogAsync("ApiLogin", "User", user.Id, null, null, HttpContext.Connection.RemoteIpAddress?.ToString(), "Failure", $"Account locked out for {user.Email}");
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<AuthResponseDto>.Fail("Account locked out due to multiple failed login attempts."));
            }

            await _auditLogService.LogAsync("ApiLogin", "User", user.Id, null, null, HttpContext.Connection.RemoteIpAddress?.ToString(), "Failure", $"Invalid password for {user.Email}");
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Invalid credentials."));
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = _userManager.GetUserId(User);
            await _signInManager.SignOutAsync();
            await _auditLogService.LogAsync("ApiLogout", "User", userId, null, null, HttpContext.Connection.RemoteIpAddress?.ToString(), "Success", "API logout successful");
            return Ok(ApiResponse<string>.Ok("Logged out successfully.", "Logout successful."));
        }
    }
}
