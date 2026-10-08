using AcxiomCRM.Authorization;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IAuditLogService auditLogService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Attempt login with Identity lockout enabled
            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                await _auditLogService.LogAsync(
                    userId: user?.Id,
                    action: "User Login",
                    entityName: "ApplicationUser",
                    recordId: user?.Id,
                    ipAddress: ipAddress,
                    result: "Success",
                    details: $"User '{model.Email}' logged in successfully.");

                _logger.LogInformation("User '{Email}' logged in successfully.", model.Email);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                await _auditLogService.LogAsync(
                    userId: user?.Id,
                    action: "Account Lockout",
                    entityName: "ApplicationUser",
                    recordId: user?.Id,
                    ipAddress: ipAddress,
                    result: "LockedOut",
                    details: $"User account '{model.Email}' was locked out due to multiple failed login attempts.");

                _logger.LogWarning("User account '{Email}' locked out.", model.Email);
                ModelState.AddModelError(string.Empty, "This account has been locked out due to multiple failed login attempts. Please try again later.");
                return View(model);
            }

            // Failed login
            var failedUser = await _userManager.FindByEmailAsync(model.Email);
            await _auditLogService.LogAsync(
                userId: failedUser?.Id,
                action: "User Login Failure",
                entityName: "ApplicationUser",
                recordId: failedUser?.Id,
                ipAddress: ipAddress,
                result: "Failure",
                details: $"Failed login attempt for email '{model.Email}'.");

            _logger.LogWarning("Invalid login attempt for email '{Email}'.", model.Email);
            ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your credentials.");
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Ensure target role exists
                string selectedRole = string.IsNullOrEmpty(model.SelectedRole) ? Roles.SalesExecutive : model.SelectedRole;
                if (!await _roleManager.RoleExistsAsync(selectedRole))
                {
                    selectedRole = Roles.SalesExecutive;
                }

                await _userManager.AddToRoleAsync(user, selectedRole);

                await _auditLogService.LogAsync(
                    userId: user.Id,
                    action: "User Registration",
                    entityName: "ApplicationUser",
                    recordId: user.Id,
                    ipAddress: ipAddress,
                    result: "Success",
                    details: $"User '{model.Email}' registered with role '{selectedRole}'.");

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _signInManager.SignOutAsync();

            if (user != null)
            {
                await _auditLogService.LogAsync(
                    userId: user.Id,
                    action: "User Logout",
                    entityName: "ApplicationUser",
                    recordId: user.Id,
                    ipAddress: ipAddress,
                    result: "Success",
                    details: $"User '{user.Email}' logged out.");
            }

            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
