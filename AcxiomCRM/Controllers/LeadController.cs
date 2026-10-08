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
    public class LeadController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICrmDataScopeService _scopeService;
        private readonly IAuditLogService _auditLogService;

        public LeadController(
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

        // GET: Lead
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? statusFilter,
            string? priorityFilter,
            string? assignedUserFilter,
            int pageNumber = 1)
        {
            var query = _scopeService.ApplyLeadScope(_context.Leads.AsNoTracking(), User);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(l => l.LeadName.ToLower().Contains(term) ||
                                         l.Email.ToLower().Contains(term) ||
                                         (l.Phone != null && l.Phone.Contains(term)) ||
                                         (l.CompanyName != null && l.CompanyName.ToLower().Contains(term)) ||
                                         l.LeadCode.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(l => l.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(priorityFilter))
            {
                query = query.Where(l => l.Priority == priorityFilter);
            }

            if (!string.IsNullOrWhiteSpace(assignedUserFilter))
            {
                query = query.Where(l => l.AssignedTo == assignedUserFilter);
            }

            query = query.OrderByDescending(l => l.CreatedDate);

            int pageSize = 10;
            var paginatedLeads = await PaginatedList<Lead>.CreateAsync(query, pageNumber, pageSize);

            var users = await _userManager.Users.Where(u => u.IsActive).Select(u => u.Email!).ToListAsync();

            var model = new LeadListViewModel
            {
                Leads = paginatedLeads,
                SearchTerm = searchTerm,
                StatusFilter = statusFilter,
                PriorityFilter = priorityFilter,
                AssignedUserFilter = assignedUserFilter,
                PageNumber = pageNumber,
                UsersList = users.Select(u => new SelectListItem { Value = u, Text = u })
            };

            return View(model);
        }

        // GET: Lead/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var lead = await _context.Leads
                .Include(l => l.Opportunities)
                .Include(l => l.FollowUps)
                .Include(l => l.Activities)
                .FirstOrDefaultAsync(l => l.LeadId == id);

            if (lead == null || !_scopeService.CanAccessLead(lead, User))
            {
                return NotFound();
            }

            var model = new LeadDetailsViewModel
            {
                Lead = lead,
                Opportunities = lead.Opportunities.OrderByDescending(o => o.CreatedDate),
                FollowUps = lead.FollowUps.OrderBy(f => f.FollowUpDate),
                Activities = lead.Activities.OrderByDescending(a => a.ActivityDate)
            };

            return View(model);
        }

        // GET: Lead/Create
        public async Task<IActionResult> Create()
        {
            var users = await _userManager.Users.Where(u => u.IsActive).Select(u => u.Email!).ToListAsync();

            var model = new LeadCreateViewModel
            {
                LeadCode = "LEAD-" + DateTime.UtcNow.Ticks.ToString().Substring(12),
                AssignedTo = User.Identity?.Name,
                UsersList = users.Select(u => new SelectListItem { Value = u, Text = u })
            };

            return View(model);
        }

        // POST: Lead/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeadCreateViewModel model)
        {
            if (!string.IsNullOrEmpty(model.AssignedTo))
            {
                var assignedUserExists = await _userManager.FindByEmailAsync(model.AssignedTo);
                if (assignedUserExists == null)
                {
                    ModelState.AddModelError("AssignedTo", "Assigned user does not exist in the system.");
                }
            }

            if (!ModelState.IsValid)
            {
                var users = await _userManager.Users.Where(u => u.IsActive).Select(u => u.Email!).ToListAsync();
                model.UsersList = users.Select(u => new SelectListItem { Value = u, Text = u });
                return View(model);
            }

            var lead = new Lead
            {
                LeadCode = model.LeadCode.Trim().ToUpper(),
                LeadName = model.LeadName.Trim(),
                Email = model.Email.Trim().ToLower(),
                Phone = model.Phone.Trim(),
                CompanyName = model.CompanyName?.Trim(),
                Source = model.Source?.Trim(),
                Status = model.Status,
                Priority = model.Priority,
                ExpectedValue = model.ExpectedValue,
                AssignedTo = model.AssignedTo,
                CreatedDate = DateTime.UtcNow
            };

            await _context.Leads.AddAsync(lead);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                action: "Lead Created",
                entityName: "Lead",
                recordId: lead.LeadId.ToString(),
                newValue: $"Name: {lead.LeadName}, Email: {lead.Email}, Status: {lead.Status}, AssignedTo: {lead.AssignedTo}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                result: "Success",
                details: $"Created lead '{lead.LeadName}'.");

            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Lead/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var lead = await _context.Leads.FindAsync(id);
            if (lead == null || !_scopeService.CanAccessLead(lead, User))
            {
                return NotFound();
            }

            if (lead.Status == "Converted")
            {
                TempData["ErrorMessage"] = "Converted leads cannot be edited.";
                return RedirectToAction(nameof(Index));
            }

            var users = await _userManager.Users.Where(u => u.IsActive).Select(u => u.Email!).ToListAsync();

            var model = new LeadEditViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone ?? string.Empty,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Status = lead.Status,
                Priority = lead.Priority,
                ExpectedValue = lead.ExpectedValue,
                AssignedTo = lead.AssignedTo,
                UsersList = users.Select(u => new SelectListItem { Value = u, Text = u })
            };

            return View(model);
        }

        // POST: Lead/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LeadEditViewModel model)
        {
            if (id != model.LeadId) return BadRequest();

            var lead = await _context.Leads.FindAsync(id);
            if (lead == null || !_scopeService.CanAccessLead(lead, User)) return NotFound();

            if (lead.Status == "Converted")
            {
                TempData["ErrorMessage"] = "Converted leads cannot be modified.";
                return RedirectToAction(nameof(Index));
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
                model.UsersList = users.Select(u => new SelectListItem { Value = u, Text = u });
                return View(model);
            }

            var oldValue = $"Name: {lead.LeadName}, Status: {lead.Status}, AssignedTo: {lead.AssignedTo}";

            lead.LeadName = model.LeadName.Trim();
            lead.Email = model.Email.Trim().ToLower();
            lead.Phone = model.Phone.Trim();
            lead.CompanyName = model.CompanyName?.Trim();
            lead.Source = model.Source?.Trim();
            lead.Status = model.Status;
            lead.Priority = model.Priority;
            lead.ExpectedValue = model.ExpectedValue;
            lead.AssignedTo = model.AssignedTo;

            _context.Leads.Update(lead);
            await _context.SaveChangesAsync();

            var newValue = $"Name: {lead.LeadName}, Status: {lead.Status}, AssignedTo: {lead.AssignedTo}";

            await _auditLogService.LogAsync(
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                action: "Lead Updated",
                entityName: "Lead",
                recordId: lead.LeadId.ToString(),
                oldValue: oldValue,
                newValue: newValue,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                result: "Success",
                details: $"Updated lead '{lead.LeadName}'.");

            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Lead/Convert/5
        public async Task<IActionResult> Convert(int id)
        {
            var lead = await _context.Leads.FindAsync(id);
            if (lead == null || !_scopeService.CanAccessLead(lead, User)) return NotFound();

            if (lead.Status == "Converted")
            {
                TempData["ErrorMessage"] = "Lead has already been converted.";
                return RedirectToAction(nameof(Index));
            }

            var model = new LeadConvertViewModel
            {
                LeadId = lead.LeadId,
                LeadName = lead.LeadName,
                Email = lead.Email,
                CompanyName = lead.CompanyName,
                ExpectedValue = lead.ExpectedValue,
                OpportunityName = $"{lead.LeadName} - Enterprise Opportunity",
                OpportunityAmount = lead.ExpectedValue,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(30)
            };

            return View(model);
        }

        // POST: Lead/Convert/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convert(int id, LeadConvertViewModel model)
        {
            if (id != model.LeadId) return BadRequest();

            var lead = await _context.Leads.FindAsync(id);
            if (lead == null || !_scopeService.CanAccessLead(lead, User)) return NotFound();

            if (lead.Status == "Converted")
            {
                TempData["ErrorMessage"] = "Lead has already been converted.";
                return RedirectToAction(nameof(Index));
            }

            Customer? createdCustomer = null;
            Opportunity? createdOpp = null;

            // 1. Create Customer if selected
            if (model.CreateCustomer)
            {
                var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == lead.Email.ToLower());
                if (existingCustomer != null)
                {
                    createdCustomer = existingCustomer;
                }
                else
                {
                    createdCustomer = new Customer
                    {
                        CustomerCode = "CUST-" + DateTime.UtcNow.Ticks.ToString().Substring(12),
                        CustomerName = lead.LeadName,
                        Email = lead.Email,
                        Phone = lead.Phone,
                        CompanyName = lead.CompanyName,
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = User.Identity?.Name
                    };
                    await _context.Customers.AddAsync(createdCustomer);
                    await _context.SaveChangesAsync();
                }
            }

            // 2. Create Opportunity if selected
            if (model.CreateOpportunity)
            {
                createdOpp = new Opportunity
                {
                    OpportunityName = string.IsNullOrWhiteSpace(model.OpportunityName) ? $"{lead.LeadName} Opportunity" : model.OpportunityName,
                    CustomerId = createdCustomer?.CustomerId,
                    LeadId = lead.LeadId,
                    Amount = model.OpportunityAmount,
                    Stage = model.OpportunityStage,
                    Probability = model.OpportunityStage switch
                    {
                        "Qualification" => 25,
                        "Proposal" => 50,
                        "Negotiation" => 80,
                        "Won" => 100,
                        _ => 10
                    },
                    ExpectedCloseDate = model.ExpectedCloseDate,
                    Status = "Open",
                    AssignedTo = lead.AssignedTo ?? User.Identity?.Name,
                    CreatedDate = DateTime.UtcNow,
                    Notes = $"Converted from Lead #{lead.LeadId} ({lead.LeadCode})."
                };
                await _context.Opportunities.AddAsync(createdOpp);
                await _context.SaveChangesAsync();
            }

            // 3. Mark Lead as Converted
            lead.Status = "Converted";
            _context.Leads.Update(lead);
            await _context.SaveChangesAsync();

            // 4. Audit Log
            await _auditLogService.LogAsync(
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                action: "Lead Converted",
                entityName: "Lead",
                recordId: lead.LeadId.ToString(),
                oldValue: "Status: " + lead.Status,
                newValue: $"Status: Converted, CustomerId: {createdCustomer?.CustomerId}, OpportunityId: {createdOpp?.OpportunityId}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                result: "Success",
                details: $"Successfully converted Lead '{lead.LeadName}' into Customer '{createdCustomer?.CustomerName}' and Opportunity '{createdOpp?.OpportunityName}'.");

            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' was successfully converted!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Lead/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var lead = await _context.Leads.FindAsync(id);
            if (lead == null || !_scopeService.CanAccessLead(lead, User)) return NotFound();

            lead.Status = "Lost";
            _context.Leads.Update(lead);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                action: "Lead Status Changed to Lost",
                entityName: "Lead",
                recordId: id.ToString(),
                oldValue: $"Name: {lead.LeadName}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                result: "Success",
                details: $"Marked Lead '{lead.LeadName}' as Lost.");

            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' marked as Lost.";
            return RedirectToAction(nameof(Index));
        }
    }
}
