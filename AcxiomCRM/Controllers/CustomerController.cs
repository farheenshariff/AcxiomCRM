using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmDataScopeService _scopeService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(
            ApplicationDbContext context,
            ICrmDataScopeService scopeService,
            IAuditLogService auditLogService,
            ILogger<CustomerController> logger)
        {
            _context = context;
            _scopeService = scopeService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        // GET: Customer
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? statusFilter,
            string? sortOrder,
            int pageNumber = 1)
        {
            var query = _scopeService.ApplyCustomerScope(_context.Customers.AsNoTracking(), User);

            // Filtering
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(c => c.CustomerName.ToLower().Contains(term) ||
                                         c.Email.ToLower().Contains(term) ||
                                         (c.Phone != null && c.Phone.Contains(term)) ||
                                         (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)) ||
                                         c.CustomerCode.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(c => c.Status == statusFilter);
            }

            // Sorting
            query = sortOrder switch
            {
                "name_desc" => query.OrderByDescending(c => c.CustomerName),
                "date_asc" => query.OrderBy(c => c.CreatedDate),
                "date_desc" => query.OrderByDescending(c => c.CreatedDate),
                "code" => query.OrderBy(c => c.CustomerCode),
                _ => query.OrderBy(c => c.CustomerName)
            };

            int pageSize = 10;
            var paginatedCustomers = await PaginatedList<Customer>.CreateAsync(query, pageNumber, pageSize);

            var model = new CustomerListViewModel
            {
                Customers = paginatedCustomers,
                SearchTerm = searchTerm,
                StatusFilter = statusFilter,
                SortOrder = sortOrder,
                PageNumber = pageNumber,
                TotalActiveCount = await _context.Customers.CountAsync(c => c.Status == "Active"),
                TotalInactiveCount = await _context.Customers.CountAsync(c => c.Status == "Inactive")
            };

            return View(model);
        }

        // GET: Customer/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.Opportunities)
                .Include(c => c.FollowUps)
                .Include(c => c.Activities)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null || !_scopeService.CanAccessCustomer(customer, User))
            {
                return NotFound();
            }

            var model = new CustomerDetailsViewModel
            {
                Customer = customer,
                Opportunities = customer.Opportunities.OrderByDescending(o => o.CreatedDate),
                FollowUps = customer.FollowUps.OrderBy(f => f.FollowUpDate),
                Activities = customer.Activities.OrderByDescending(a => a.ActivityDate),
                TotalClosedRevenue = customer.Opportunities.Where(o => o.Stage == "Won").Sum(o => o.Amount),
                ActivePipelineValue = customer.Opportunities.Where(o => o.Status == "Open").Sum(o => o.Amount)
            };

            return View(model);
        }

        // GET: Customer/Create
        public IActionResult Create()
        {
            var model = new CustomerCreateViewModel
            {
                CustomerCode = "CUST-" + DateTime.UtcNow.Ticks.ToString().Substring(12)
            };
            return View(model);
        }

        // POST: Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerCreateViewModel model)
        {
            // Server-side Unique Checks
            if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == model.Email.Trim().ToLower()))
            {
                ModelState.AddModelError("Email", "A customer with this email address already exists.");
            }

            if (await _context.Customers.AnyAsync(c => c.Phone != null && c.Phone.Trim() == model.Phone.Trim()))
            {
                ModelState.AddModelError("Phone", "A customer with this phone number already exists.");
            }

            if (await _context.Customers.AnyAsync(c => c.CustomerCode.ToLower() == model.CustomerCode.Trim().ToLower()))
            {
                ModelState.AddModelError("CustomerCode", "Customer Code must be unique.");
            }

            // Duplicate Name & Company Check
            if (!string.IsNullOrWhiteSpace(model.CompanyName) &&
                await _context.Customers.AnyAsync(c => c.CustomerName.ToLower() == model.CustomerName.Trim().ToLower() &&
                                                        c.CompanyName!.ToLower() == model.CompanyName.Trim().ToLower()))
            {
                ModelState.AddModelError(string.Empty, "A customer with the exact same name and company already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var customer = new Customer
            {
                CustomerCode = model.CustomerCode.Trim().ToUpper(),
                CustomerName = model.CustomerName.Trim(),
                Email = model.Email.Trim().ToLower(),
                Phone = model.Phone.Trim(),
                CompanyName = model.CompanyName?.Trim(),
                Address = model.Address?.Trim(),
                City = model.City?.Trim(),
                State = model.State?.Trim(),
                Status = model.Status,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name
            };

            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();

            // Audit Log
            await _auditLogService.LogAsync(
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                action: "Customer Created",
                entityName: "Customer",
                recordId: customer.CustomerId.ToString(),
                newValue: $"Name: {customer.CustomerName}, Email: {customer.Email}, Code: {customer.CustomerCode}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                result: "Success",
                details: $"Created customer '{customer.CustomerName}' with code '{customer.CustomerCode}'.");

            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Customer/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null || !_scopeService.CanAccessCustomer(customer, User))
            {
                return NotFound();
            }

            var model = new CustomerEditViewModel
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone ?? string.Empty,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status
            };

            return View(model);
        }

        // POST: Customer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CustomerEditViewModel model)
        {
            if (id != model.CustomerId)
            {
                return BadRequest();
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null || !_scopeService.CanAccessCustomer(customer, User))
            {
                return NotFound();
            }

            // Server-side Unique Checks excluding current record
            if (await _context.Customers.AnyAsync(c => c.CustomerId != id && c.Email.ToLower() == model.Email.Trim().ToLower()))
            {
                ModelState.AddModelError("Email", "Another customer with this email address already exists.");
            }

            if (await _context.Customers.AnyAsync(c => c.CustomerId != id && c.Phone != null && c.Phone.Trim() == model.Phone.Trim()))
            {
                ModelState.AddModelError("Phone", "Another customer with this phone number already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var oldValue = $"Name: {customer.CustomerName}, Email: {customer.Email}, Status: {customer.Status}";

            customer.CustomerName = model.CustomerName.Trim();
            customer.Email = model.Email.Trim().ToLower();
            customer.Phone = model.Phone.Trim();
            customer.CompanyName = model.CompanyName?.Trim();
            customer.Address = model.Address?.Trim();
            customer.City = model.City?.Trim();
            customer.State = model.State?.Trim();
            customer.Status = model.Status;

            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            var newValue = $"Name: {customer.CustomerName}, Email: {customer.Email}, Status: {customer.Status}";

            await _auditLogService.LogAsync(
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                action: "Customer Updated",
                entityName: "Customer",
                recordId: customer.CustomerId.ToString(),
                oldValue: oldValue,
                newValue: newValue,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                result: "Success",
                details: $"Updated details for customer '{customer.CustomerName}'.");

            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Customer/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.Opportunities)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null || !_scopeService.CanAccessCustomer(customer, User))
            {
                return NotFound();
            }

            // Soft delete / Status toggle or hard delete if no active opportunities
            string actionTaken;
            if (customer.Opportunities.Any())
            {
                customer.Status = "Inactive";
                _context.Customers.Update(customer);
                actionTaken = "Deactivated";
            }
            else
            {
                _context.Customers.Remove(customer);
                actionTaken = "Deleted";
            }

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                action: $"Customer {actionTaken}",
                entityName: "Customer",
                recordId: id.ToString(),
                oldValue: $"Name: {customer.CustomerName}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                result: "Success",
                details: $"{actionTaken} customer '{customer.CustomerName}'.");

            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' was {actionTaken.ToLower()} successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
