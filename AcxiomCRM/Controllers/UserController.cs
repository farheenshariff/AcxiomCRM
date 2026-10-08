using AcxiomCRM.Authorization;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAuditLogService _auditLogService;

        public UserController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IAuditLogService auditLogService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _auditLogService = auditLogService;
        }

        // GET: User
        public async Task<IActionResult> Index(string? searchTerm, string? roleFilter, int pageNumber = 1)
        {
            var usersQuery = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                usersQuery = usersQuery.Where(u => u.FullName.ToLower().Contains(term) || u.Email!.ToLower().Contains(term));
            }

            var usersList = await usersQuery.OrderByDescending(u => u.CreatedDate).ToListAsync();

            var userItemModels = new List<UserItemViewModel>();
            foreach (var user in usersList)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "None";

                if (!string.IsNullOrWhiteSpace(roleFilter) && role != roleFilter)
                {
                    continue;
                }

                var isLockedOut = await _userManager.IsLockedOutAsync(user);

                userItemModels.Add(new UserItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email!,
                    Role = role,
                    IsActive = user.IsActive,
                    IsLockedOut = isLockedOut,
                    LockoutEnd = user.LockoutEnd,
                    CreatedDate = user.CreatedDate
                });
            }

            int pageSize = 10;
            var paginatedUsers = PaginatedList<UserItemViewModel>.Create(userItemModels, pageNumber, pageSize);

            var model = new UserListViewModel
            {
                Users = paginatedUsers,
                SearchTerm = searchTerm,
                RoleFilter = roleFilter,
                PageNumber = pageNumber
            };

            return View(model);
        }

        // GET: User/Create
        public IActionResult Create()
        {
            return View(new UserCreateViewModel());
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "User email address already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName.Trim(),
                EmailConfirmed = true,
                IsActive = model.IsActive,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                if (await _roleManager.RoleExistsAsync(model.Role))
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                }

                await _auditLogService.LogAsync(
                    userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    action: "User Created by Admin",
                    entityName: "ApplicationUser",
                    recordId: user.Id,
                    newValue: $"Email: {user.Email}, Role: {model.Role}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    result: "Success",
                    details: $"Admin created user '{user.Email}' with role '{model.Role}'.");

                TempData["SuccessMessage"] = $"User '{user.Email}' created successfully!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // GET: User/Edit/id
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var model = new UserEditViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                Role = roles.FirstOrDefault() ?? "SalesExecutive",
                IsActive = user.IsActive
            };

            return View(model);
        }

        // POST: User/Edit/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserEditViewModel model)
        {
            if (id != model.Id) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (!ModelState.IsValid) return View(model);

            var currentRoles = await _userManager.GetRolesAsync(user);
            var currentRole = currentRoles.FirstOrDefault() ?? "None";

            var oldValue = $"Name: {user.FullName}, Role: {currentRole}, Active: {user.IsActive}";

            user.FullName = model.FullName.Trim();
            user.IsActive = model.IsActive;

            await _userManager.UpdateAsync(user);

            // Update role if changed
            if (currentRole != model.Role && await _roleManager.RoleExistsAsync(model.Role))
            {
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            var newValue = $"Name: {user.FullName}, Role: {model.Role}, Active: {user.IsActive}";

            await _auditLogService.LogAsync(
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                action: "User Updated by Admin",
                entityName: "ApplicationUser",
                recordId: user.Id,
                oldValue: oldValue,
                newValue: newValue,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                result: "Success",
                details: $"Admin updated user '{user.Email}'.");

            TempData["SuccessMessage"] = $"User '{user.Email}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: User/ToggleStatus/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            var statusStr = user.IsActive ? "Activated" : "Deactivated";

            await _auditLogService.LogAsync(
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                action: $"User {statusStr}",
                entityName: "ApplicationUser",
                recordId: user.Id,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                result: "Success",
                details: $"Admin {statusStr.ToLower()} user '{user.Email}'.");

            TempData["SuccessMessage"] = $"User '{user.Email}' was {statusStr.ToLower()}!";
            return RedirectToAction(nameof(Index));
        }

        // POST: User/Unlock/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            await _auditLogService.LogAsync(
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                action: "User Lockout Reset",
                entityName: "ApplicationUser",
                recordId: user.Id,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                result: "Success",
                details: $"Unlocked account for user '{user.Email}'.");

            TempData["SuccessMessage"] = $"User account '{user.Email}' unlocked!";
            return RedirectToAction(nameof(Index));
        }

        // GET: User/ResetPassword/id
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var model = new UserResetPasswordViewModel
            {
                UserId = user.Id,
                Email = user.Email!
            };

            return View(model);
        }

        // POST: User/ResetPassword/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, UserResetPasswordViewModel model)
        {
            if (id != model.UserId) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (!ModelState.IsValid) return View(model);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                await _auditLogService.LogAsync(
                    userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    action: "Password Reset by Admin",
                    entityName: "ApplicationUser",
                    recordId: user.Id,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    result: "Success",
                    details: $"Admin reset password for user '{user.Email}'.");

                TempData["SuccessMessage"] = $"Password for '{user.Email}' was reset successfully!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }
}
