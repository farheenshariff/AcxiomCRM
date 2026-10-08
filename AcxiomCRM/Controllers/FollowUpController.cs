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
    public class FollowUpController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICrmDataScopeService _scopeService;
        private readonly IAuditLogService _auditLogService;

        public FollowUpController(
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

        // GET: FollowUp
        public async Task<IActionResult> Index(
            string activeTab = "Pending",
            string? searchTerm = null,
            string? assignedUserFilter = null,
            int pageNumber = 1)
        {
            var query = _scopeService.ApplyFollowUpScope(
                _context.FollowUps
                    .Include(f => f.Customer)
                    .Include(f => f.Lead)
                    .AsNoTracking(), User);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(f => f.Subject.ToLower().Contains(term) ||
                                         (f.Customer != null && f.Customer.CustomerName.ToLower().Contains(term)) ||
                                         (f.Lead != null && f.Lead.LeadName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(assignedUserFilter))
            {
                query = query.Where(f => f.AssignedTo == assignedUserFilter);
            }

            var now = DateTime.UtcNow;

            // Tab Filters
            query = activeTab switch
            {
                "Overdue" => query.Where(f => f.Status == "Planned" && f.FollowUpDate < now),
                "Upcoming" => query.Where(f => f.Status == "Planned" && f.FollowUpDate >= now && f.FollowUpDate <= now.AddDays(7)),
                "Completed" => query.Where(f => f.Status == "Completed"),
                "Cancelled" => query.Where(f => f.Status == "Cancelled"),
                _ => query.Where(f => f.Status == "Planned")
            };

            query = query.OrderBy(f => f.FollowUpDate);

            int pageSize = 10;
            var paginatedFollowUps = await PaginatedList<FollowUp>.CreateAsync(query, pageNumber, pageSize);

            var allScopedQuery = _scopeService.ApplyFollowUpScope(_context.FollowUps.AsNoTracking(), User);

            var users = await _userManager.Users.Where(u => u.IsActive).Select(u => u.Email!).ToListAsync();

            var model = new FollowUpListViewModel
            {
                FollowUps = paginatedFollowUps,
                ActiveTab = activeTab,
                SearchTerm = searchTerm,
                AssignedUserFilter = assignedUserFilter,
                PageNumber = pageNumber,
                PendingCount = await allScopedQuery.CountAsync(f => f.Status == "Planned"),
                OverdueCount = await allScopedQuery.CountAsync(f => f.Status == "Planned" && f.FollowUpDate < now),
                UpcomingCount = await allScopedQuery.CountAsync(f => f.Status == "Planned" && f.FollowUpDate >= now && f.FollowUpDate <= now.AddDays(7)),
                CompletedCount = await allScopedQuery.CountAsync(f => f.Status == "Completed"),
                UsersList = users.Select(u => new SelectListItem { Value = u, Text = u })
            };

            return View(model);
        }

        // GET: FollowUp/Create
        public async Task<IActionResult> Create()
        {
            var users = await _userManager.Users.Where(u => u.IsActive).Select(u => u.Email!).ToListAsync();
            var customers = await _context.Customers.Where(c => c.Status == "Active").ToListAsync();
            var leads = await _context.Leads.Where(l => l.Status != "Converted").ToListAsync();

            var model = new FollowUpCreateViewModel
            {
                FollowUpDate = DateTime.UtcNow.AddDays(1),
                AssignedTo = User.Identity?.Name,
                CustomersList = customers.Select(c => new SelectListItem { Value = c.CustomerId.ToString(), Text = $"{c.CustomerName} ({c.CustomerCode})" }),
                LeadsList = leads.Select(l => new SelectListItem { Value = l.LeadId.ToString(), Text = $"{l.LeadName} ({l.LeadCode})" }),
                UsersList = users.Select(u => new SelectListItem { Value = u, Text = u })
            };

            return View(model);
        }

        // POST: FollowUp/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FollowUpCreateViewModel model)
        {
            if (model.FollowUpDate < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be in the past for new scheduled tasks.");
            }

            if (!string.IsNullOrEmpty(model.AssignedTo))
            {
                var assignedUserExists = await _userManager.FindByEmailAsync(model.AssignedTo);
                if (assignedUserExists == null)
                {
                    ModelState.AddModelError("AssignedTo", "Assigned user does not exist.");
                }
            }

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

            var followUp = new FollowUp
            {
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                FollowUpDate = model.FollowUpDate,
                FollowUpType = model.FollowUpType,
                Subject = model.Subject.Trim(),
                Remarks = model.Remarks?.Trim(),
                Status = "Planned",
                AssignedTo = model.AssignedTo
            };

            await _context.FollowUps.AddAsync(followUp);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                action: "Follow-Up Scheduled",
                entityName: "FollowUp",
                recordId: followUp.FollowUpId.ToString(),
                newValue: $"Subject: {followUp.Subject}, Date: {followUp.FollowUpDate:yyyy-MM-dd}, AssignedTo: {followUp.AssignedTo}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                result: "Success",
                details: $"Scheduled follow-up '{followUp.Subject}'.");

            TempData["SuccessMessage"] = $"Follow-Up '{followUp.Subject}' scheduled successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: FollowUp/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string? completionRemarks)
        {
            var followUp = await _context.FollowUps.FindAsync(id);
            if (followUp == null || !_scopeService.CanAccessFollowUp(followUp, User)) return NotFound();

            followUp.Status = "Completed";
            if (!string.IsNullOrWhiteSpace(completionRemarks))
            {
                followUp.Remarks = (followUp.Remarks + "\n[Completion Notes]: " + completionRemarks).Trim();
            }

            _context.FollowUps.Update(followUp);

            // Also record an Activity log entry automatically when FollowUp is completed!
            var activity = new Activity
            {
                ActivityType = followUp.FollowUpType ?? "Follow-Up",
                Subject = $"Completed Follow-Up: {followUp.Subject}",
                Description = completionRemarks ?? followUp.Remarks,
                ActivityDate = DateTime.UtcNow,
                CustomerId = followUp.CustomerId,
                LeadId = followUp.LeadId,
                AssignedTo = followUp.AssignedTo,
                Status = "Completed"
            };
            await _context.Activities.AddAsync(activity);

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                action: "Follow-Up Completed",
                entityName: "FollowUp",
                recordId: id.ToString(),
                newValue: $"Status: Completed, Remarks: {completionRemarks}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                result: "Success",
                details: $"Completed follow-up '{followUp.Subject}'.");

            TempData["SuccessMessage"] = $"Follow-Up '{followUp.Subject}' marked as Completed!";
            return RedirectToAction(nameof(Index));
        }

        // POST: FollowUp/Reschedule/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(int id, DateTime newFollowUpDate, string? rescheduleReason)
        {
            var followUp = await _context.FollowUps.FindAsync(id);
            if (followUp == null || !_scopeService.CanAccessFollowUp(followUp, User)) return NotFound();

            if (newFollowUpDate.Date < DateTime.UtcNow.Date)
            {
                TempData["ErrorMessage"] = "New follow-up date cannot be in the past.";
                return RedirectToAction(nameof(Index));
            }

            var oldDate = followUp.FollowUpDate;
            followUp.FollowUpDate = newFollowUpDate;
            followUp.Status = "Planned";
            if (!string.IsNullOrWhiteSpace(rescheduleReason))
            {
                followUp.Remarks = (followUp.Remarks + $"\n[Rescheduled from {oldDate:yyyy-MM-dd}]: {rescheduleReason}").Trim();
            }

            _context.FollowUps.Update(followUp);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                action: "Follow-Up Rescheduled",
                entityName: "FollowUp",
                recordId: id.ToString(),
                oldValue: $"Date: {oldDate:yyyy-MM-dd HH:mm}",
                newValue: $"Date: {newFollowUpDate:yyyy-MM-dd HH:mm}, Reason: {rescheduleReason}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                result: "Success",
                details: $"Rescheduled follow-up '{followUp.Subject}' to {newFollowUpDate:MMM dd, yyyy}.");

            TempData["SuccessMessage"] = $"Follow-Up '{followUp.Subject}' rescheduled to {newFollowUpDate:MMM dd, yyyy}!";
            return RedirectToAction(nameof(Index));
        }

        // POST: FollowUp/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var followUp = await _context.FollowUps.FindAsync(id);
            if (followUp == null || !_scopeService.CanAccessFollowUp(followUp, User)) return NotFound();

            followUp.Status = "Cancelled";
            _context.FollowUps.Update(followUp);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                action: "Follow-Up Cancelled",
                entityName: "FollowUp",
                recordId: id.ToString(),
                oldValue: $"Status: Planned",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                result: "Success",
                details: $"Cancelled follow-up '{followUp.Subject}'.");

            TempData["SuccessMessage"] = $"Follow-Up '{followUp.Subject}' cancelled.";
            return RedirectToAction(nameof(Index));
        }
    }
}
