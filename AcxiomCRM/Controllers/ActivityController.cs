using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ActivityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICrmDataScopeService _scopeService;
        private readonly IAuditLogService _auditLogService;

        public ActivityController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICrmDataScopeService scopeService,
            IAuditLogService auditLogService)
        {
            _context = context;
            _userManager = userManager;
            _scopeService = scopeService;
            _auditLogService = auditLogService;
        }

        // GET: Activity
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? typeFilter,
            string? statusFilter,
            string? assignedUserFilter,
            int pageNumber = 1)
        {
            var query = _scopeService.ApplyActivityScope(
                _context.Activities
                    .Include(a => a.Customer)
                    .Include(a => a.Lead)
                    .AsNoTracking(), User);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(a => a.Subject.ToLower().Contains(term) ||
                                         (a.Description != null && a.Description.ToLower().Contains(term)) ||
                                         (a.Customer != null && a.Customer.CustomerName.ToLower().Contains(term)) ||
                                         (a.Lead != null && a.Lead.LeadName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(typeFilter))
            {
                query = query.Where(a => a.ActivityType == typeFilter);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(a => a.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(assignedUserFilter))
            {
                query = query.Where(a => a.AssignedTo == assignedUserFilter);
            }

            query = query.OrderByDescending(a => a.ActivityDate);

            int pageSize = 10;
            var paginatedActivities = await PaginatedList<Activity>.CreateAsync(query, pageNumber, pageSize);

            var users = await _userManager.Users.Where(u => u.IsActive).Select(u => u.Email!).ToListAsync();

            var model = new ActivityListViewModel
            {
                Activities = paginatedActivities,
                SearchTerm = searchTerm,
                TypeFilter = typeFilter,
                StatusFilter = statusFilter,
                AssignedUserFilter = assignedUserFilter,
                PageNumber = pageNumber,
                UsersList = users.Select(u => new SelectListItem { Value = u, Text = u })
            };

            return View(model);
        }

        // GET: Activity/Create
        public async Task<IActionResult> Create()
        {
            var users = await _userManager.Users.Where(u => u.IsActive).Select(u => u.Email!).ToListAsync();
            var customers = await _context.Customers.Where(c => c.Status == "Active").ToListAsync();
            var leads = await _context.Leads.Where(l => l.Status != "Converted").ToListAsync();

            var model = new ActivityCreateViewModel
            {
                ActivityDate = DateTime.UtcNow,
                AssignedTo = User.Identity?.Name,
                CustomersList = customers.Select(c => new SelectListItem { Value = c.CustomerId.ToString(), Text = $"{c.CustomerName} ({c.CustomerCode})" }),
                LeadsList = leads.Select(l => new SelectListItem { Value = l.LeadId.ToString(), Text = $"{l.LeadName} ({l.LeadCode})" }),
                UsersList = users.Select(u => new SelectListItem { Value = u, Text = u })
            };

            return View(model);
        }

        // POST: Activity/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActivityCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var users = await _userManager.Users.Where(u => u.IsActive).Select(u => u.Email!).ToListAsync();
                var customers = await _context.Customers.Where(c => c.Status == "Active").ToListAsync();
                var leads = await _context.Leads.Where(l => l.Status != "Converted").ToListAsync();

                model.CustomersList = customers.Select(c => new SelectListItem { Value = c.CustomerId.ToString(), Text = $"{c.CustomerName} ({c.CustomerCode})" });
                model.LeadsList = leads.Select(l => new SelectListItem { Value = l.LeadId.ToString(), Text = $"{l.LeadName} ({l.LeadCode})" });
                model.UsersList = users.Select(u => new SelectListItem { Value = u, Text = u });
                return View(model);
            }

            var activity = new Activity
            {
                ActivityType = model.ActivityType,
                Subject = model.Subject.Trim(),
                Description = model.Description?.Trim(),
                ActivityDate = model.ActivityDate,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                AssignedTo = model.AssignedTo,
                Status = model.Status
            };

            await _context.Activities.AddAsync(activity);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                action: "Activity Logged",
                entityName: "Activity",
                recordId: activity.ActivityId.ToString(),
                newValue: $"Type: {activity.ActivityType}, Subject: {activity.Subject}, AssignedTo: {activity.AssignedTo}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                result: "Success",
                details: $"Logged activity '{activity.Subject}'.");

            TempData["SuccessMessage"] = $"Activity '{activity.Subject}' logged successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Activity/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var activity = await _context.Activities.FindAsync(id);
            if (activity == null || !_scopeService.CanAccessActivity(activity, User)) return NotFound();

            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                action: "Activity Deleted",
                entityName: "Activity",
                recordId: id.ToString(),
                oldValue: $"Subject: {activity.Subject}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                result: "Success",
                details: $"Deleted activity '{activity.Subject}'.");

            TempData["SuccessMessage"] = $"Activity '{activity.Subject}' deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
