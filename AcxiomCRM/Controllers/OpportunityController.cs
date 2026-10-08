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
    public class OpportunityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICrmDataScopeService _scopeService;
        private readonly IAuditLogService _auditLogService;

        public OpportunityController(
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

        // GET: Opportunity
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? stageFilter,
            string? statusFilter,
            string? assignedUserFilter,
            int pageNumber = 1)
        {
            var query = _scopeService.ApplyOpportunityScope(
                _context.Opportunities
                    .Include(o => o.Customer)
                    .Include(o => o.Lead)
                    .AsNoTracking(), User);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(o => o.OpportunityName.ToLower().Contains(term) ||
                                         (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(term)) ||
                                         (o.Lead != null && o.Lead.LeadName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(stageFilter))
            {
                query = query.Where(o => o.Stage == stageFilter);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(o => o.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(assignedUserFilter))
            {
                query = query.Where(o => o.AssignedTo == assignedUserFilter);
            }

            var allScopedList = await query.ToListAsync();
            var totalPipeline = allScopedList.Where(o => o.Status == "Open").Sum(o => o.Amount);
            var totalWeighted = allScopedList.Where(o => o.Status == "Open").Sum(o => o.Amount * (o.Probability / 100.0m));

            query = query.OrderByDescending(o => o.CreatedDate);

            int pageSize = 10;
            var paginatedOpps = await PaginatedList<Opportunity>.CreateAsync(query, pageNumber, pageSize);

            var users = await _userManager.Users.Where(u => u.IsActive).Select(u => u.Email!).ToListAsync();

            var model = new OpportunityListViewModel
            {
                Opportunities = paginatedOpps,
                SearchTerm = searchTerm,
                StageFilter = stageFilter,
                StatusFilter = statusFilter,
                AssignedUserFilter = assignedUserFilter,
                PageNumber = pageNumber,
                TotalPipelineValue = totalPipeline,
                TotalWeightedValue = totalWeighted,
                UsersList = users.Select(u => new SelectListItem { Value = u, Text = u })
            };

            return View(model);
        }

        // GET: Opportunity/Board (Pipeline Kanban View)
        public async Task<IActionResult> Board()
        {
            var query = _scopeService.ApplyOpportunityScope(
                _context.Opportunities
                    .Include(o => o.Customer)
                    .Include(o => o.Lead)
                    .AsNoTracking(), User);

            var opps = await query.ToListAsync();

            var model = new PipelineBoardViewModel
            {
                QualificationStage = opps.Where(o => o.Stage == "Qualification").ToList(),
                ProposalStage = opps.Where(o => o.Stage == "Proposal").ToList(),
                NegotiationStage = opps.Where(o => o.Stage == "Negotiation").ToList(),
                WonStage = opps.Where(o => o.Stage == "Won").ToList(),
                LostStage = opps.Where(o => o.Stage == "Lost").ToList()
            };

            return View(model);
        }

        // GET: Opportunity/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var opp = await _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .FirstOrDefaultAsync(o => o.OpportunityId == id);

            if (opp == null || !_scopeService.CanAccessOpportunity(opp, User)) return NotFound();

            return View(new OpportunityDetailsViewModel { Opportunity = opp });
        }

        // GET: Opportunity/Create
        public async Task<IActionResult> Create()
        {
            var users = await _userManager.Users.Where(u => u.IsActive).Select(u => u.Email!).ToListAsync();
            var customers = await _context.Customers.Where(c => c.Status == "Active").ToListAsync();
            var leads = await _context.Leads.Where(l => l.Status != "Converted").ToListAsync();

            var model = new OpportunityCreateViewModel
            {
                AssignedTo = User.Identity?.Name,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
                CustomersList = customers.Select(c => new SelectListItem { Value = c.CustomerId.ToString(), Text = $"{c.CustomerName} ({c.CustomerCode})" }),
                LeadsList = leads.Select(l => new SelectListItem { Value = l.LeadId.ToString(), Text = $"{l.LeadName} ({l.LeadCode})" }),
                UsersList = users.Select(u => new SelectListItem { Value = u, Text = u })
            };

            return View(model);
        }

        // POST: Opportunity/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OpportunityCreateViewModel model)
        {
            // Validations
            if (model.Status == "Open" && model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Active open opportunities must have an Amount greater than 0.");
            }

            if (model.Status == "Open" && model.ExpectedCloseDate.HasValue && model.ExpectedCloseDate.Value.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past for active opportunities.");
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

            var opp = new Opportunity
            {
                OpportunityName = model.OpportunityName.Trim(),
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = model.Status,
                AssignedTo = model.AssignedTo,
                Notes = model.Notes?.Trim(),
                CreatedDate = DateTime.UtcNow
            };

            await _context.Opportunities.AddAsync(opp);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                action: "Opportunity Created",
                entityName: "Opportunity",
                recordId: opp.OpportunityId.ToString(),
                newValue: $"Name: {opp.OpportunityName}, Amount: ${opp.Amount}, Stage: {opp.Stage}, AssignedTo: {opp.AssignedTo}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                result: "Success",
                details: $"Created opportunity '{opp.OpportunityName}'.");

            TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Opportunity/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var opp = await _context.Opportunities.FindAsync(id);
            if (opp == null || !_scopeService.CanAccessOpportunity(opp, User)) return NotFound();

            var users = await _userManager.Users.Where(u => u.IsActive).Select(u => u.Email!).ToListAsync();
            var customers = await _context.Customers.Where(c => c.Status == "Active").ToListAsync();
            var leads = await _context.Leads.ToListAsync();

            var model = new OpportunityEditViewModel
            {
                OpportunityId = opp.OpportunityId,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                LeadId = opp.LeadId,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Status = opp.Status,
                AssignedTo = opp.AssignedTo,
                Notes = opp.Notes,
                CustomersList = customers.Select(c => new SelectListItem { Value = c.CustomerId.ToString(), Text = $"{c.CustomerName} ({c.CustomerCode})" }),
                LeadsList = leads.Select(l => new SelectListItem { Value = l.LeadId.ToString(), Text = $"{l.LeadName} ({l.LeadCode})" }),
                UsersList = users.Select(u => new SelectListItem { Value = u, Text = u })
            };

            return View(model);
        }

        // POST: Opportunity/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OpportunityEditViewModel model)
        {
            if (id != model.OpportunityId) return BadRequest();

            var opp = await _context.Opportunities.FindAsync(id);
            if (opp == null || !_scopeService.CanAccessOpportunity(opp, User)) return NotFound();

            if (model.Status == "Open" && model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Active open opportunities must have an Amount greater than 0.");
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
                var leads = await _context.Leads.ToListAsync();

                model.CustomersList = customers.Select(c => new SelectListItem { Value = c.CustomerId.ToString(), Text = $"{c.CustomerName} ({c.CustomerCode})" });
                model.LeadsList = leads.Select(l => new SelectListItem { Value = l.LeadId.ToString(), Text = $"{l.LeadName} ({l.LeadCode})" });
                model.UsersList = users.Select(u => new SelectListItem { Value = u, Text = u });
                return View(model);
            }

            var oldValue = $"Stage: {opp.Stage}, Amount: ${opp.Amount}, Status: {opp.Status}";

            opp.OpportunityName = model.OpportunityName.Trim();
            opp.CustomerId = model.CustomerId;
            opp.LeadId = model.LeadId;
            opp.Amount = model.Amount;
            opp.Stage = model.Stage;
            opp.Probability = model.Probability;
            opp.ExpectedCloseDate = model.ExpectedCloseDate;
            opp.Status = model.Status;
            opp.AssignedTo = model.AssignedTo;
            opp.Notes = model.Notes?.Trim();

            // Auto status adjust on stage update
            if (opp.Stage == "Won") opp.Status = "Won";
            else if (opp.Stage == "Lost") opp.Status = "Lost";

            _context.Opportunities.Update(opp);
            await _context.SaveChangesAsync();

            var newValue = $"Stage: {opp.Stage}, Amount: ${opp.Amount}, Status: {opp.Status}";

            await _auditLogService.LogAsync(
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                action: "Opportunity Stage/Details Updated",
                entityName: "Opportunity",
                recordId: opp.OpportunityId.ToString(),
                oldValue: oldValue,
                newValue: newValue,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                result: "Success",
                details: $"Updated opportunity '{opp.OpportunityName}'. Stage: '{opp.Stage}'.");

            TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Opportunity/UpdateStage (Kanban Board Quick Transition API/Action)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStage(int id, string newStage)
        {
            var opp = await _context.Opportunities.FindAsync(id);
            if (opp == null || !_scopeService.CanAccessOpportunity(opp, User)) return NotFound();

            var oldStage = opp.Stage;
            opp.Stage = newStage;
            opp.Probability = newStage switch
            {
                "Qualification" => 25,
                "Proposal" => 50,
                "Negotiation" => 80,
                "Won" => 100,
                "Lost" => 0,
                _ => opp.Probability
            };

            if (newStage == "Won") opp.Status = "Won";
            else if (newStage == "Lost") opp.Status = "Lost";
            else opp.Status = "Open";

            _context.Opportunities.Update(opp);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                action: "Opportunity Stage Changed",
                entityName: "Opportunity",
                recordId: id.ToString(),
                oldValue: $"Stage: {oldStage}",
                newValue: $"Stage: {newStage}, Probability: {opp.Probability}%",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                result: "Success",
                details: $"Moved opportunity '{opp.OpportunityName}' from {oldStage} to {newStage}.");

            return RedirectToAction(nameof(Board));
        }

        // POST: Opportunity/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var opp = await _context.Opportunities.FindAsync(id);
            if (opp == null || !_scopeService.CanAccessOpportunity(opp, User)) return NotFound();

            opp.Status = "Lost";
            opp.Stage = "Lost";
            opp.Probability = 0;
            _context.Opportunities.Update(opp);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                action: "Opportunity Marked Lost",
                entityName: "Opportunity",
                recordId: id.ToString(),
                oldValue: $"Name: {opp.OpportunityName}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                result: "Success",
                details: $"Opportunity '{opp.OpportunityName}' was marked as Lost.");

            TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' marked as Lost.";
            return RedirectToAction(nameof(Index));
        }
    }
}
