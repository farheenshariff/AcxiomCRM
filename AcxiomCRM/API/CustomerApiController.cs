using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api
{
    [Route("api/customers")]
    [ApiController]
    [Authorize]
    public class CustomerApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmDataScopeService _scopeService;
        private readonly IAuditLogService _auditLogService;

        public CustomerApiController(
            ApplicationDbContext context,
            ICrmDataScopeService scopeService,
            IAuditLogService auditLogService)
        {
            _context = context;
            _scopeService = scopeService;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _scopeService.ApplyCustomerScope(_context.Customers.AsNoTracking(), User);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(c => c.CustomerName.ToLower().Contains(s)
                                      || c.Email.ToLower().Contains(s)
                                      || (c.CompanyName != null && c.CompanyName.ToLower().Contains(s))
                                      || (c.CustomerCode != null && c.CustomerCode.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.Status.ToLower() == status.Trim().ToLower());
            }

            var totalCount = await query.CountAsync();
            var customers = await query
                .OrderByDescending(c => c.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CustomerDto
                {
                    CustomerId = c.CustomerId,
                    CustomerCode = c.CustomerCode,
                    CustomerName = c.CustomerName,
                    Email = c.Email,
                    Phone = c.Phone,
                    CompanyName = c.CompanyName,
                    Address = c.Address,
                    City = c.City,
                    State = c.State,
                    Status = c.Status,
                    CreatedDate = c.CreatedDate,
                    CreatedBy = c.CreatedBy
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                TotalItems = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Items = customers
            }, "Customers retrieved successfully."));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCustomer(int id)
        {
            var customer = await _scopeService.ApplyCustomerScope(_context.Customers.AsNoTracking(), User)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound(ApiResponse<CustomerDto>.Fail($"Customer with ID {id} was not found."));
            }

            var dto = new CustomerDto
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                CreatedDate = customer.CreatedDate,
                CreatedBy = customer.CreatedBy
            };

            return Ok(ApiResponse<CustomerDto>.Ok(dto));
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<CustomerDto>.Fail("Validation failed.", errors));
            }

            // Check duplicate Email
            if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == dto.Email.ToLower()))
            {
                return Conflict(ApiResponse<CustomerDto>.Fail($"A customer with email '{dto.Email}' already exists."));
            }

            // Check duplicate Phone
            if (!string.IsNullOrWhiteSpace(dto.Phone) && await _context.Customers.AnyAsync(c => c.Phone == dto.Phone))
            {
                return Conflict(ApiResponse<CustomerDto>.Fail($"A customer with phone number '{dto.Phone}' already exists."));
            }

            var code = $"CUST-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
            var username = User.Identity?.Name ?? "API";

            var customer = new Customer
            {
                CustomerCode = code,
                CustomerName = dto.CustomerName.Trim(),
                Email = dto.Email.Trim(),
                Phone = dto.Phone?.Trim(),
                CompanyName = dto.CompanyName?.Trim(),
                Address = dto.Address?.Trim(),
                City = dto.City?.Trim(),
                State = dto.State?.Trim(),
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status.Trim(),
                CreatedDate = DateTime.UtcNow,
                CreatedBy = username
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync("CreateCustomerApi", "Customer", customer.CustomerId.ToString(), null, customer.CustomerName, HttpContext.Connection.RemoteIpAddress?.ToString(), "Success", $"Created customer {customer.CustomerName} via API");

            var resultDto = new CustomerDto
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                CreatedDate = customer.CreatedDate,
                CreatedBy = customer.CreatedBy
            };

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerId }, ApiResponse<CustomerDto>.Ok(resultDto, "Customer created successfully."));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<CustomerDto>.Fail("Validation failed.", errors));
            }

            var customer = await _scopeService.ApplyCustomerScope(_context.Customers, User)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound(ApiResponse<CustomerDto>.Fail($"Customer with ID {id} was not found."));
            }

            // Check duplicate Email
            if (await _context.Customers.AnyAsync(c => c.CustomerId != id && c.Email.ToLower() == dto.Email.ToLower()))
            {
                return Conflict(ApiResponse<CustomerDto>.Fail($"A customer with email '{dto.Email}' already exists."));
            }

            // Check duplicate Phone
            if (!string.IsNullOrWhiteSpace(dto.Phone) && await _context.Customers.AnyAsync(c => c.CustomerId != id && c.Phone == dto.Phone))
            {
                return Conflict(ApiResponse<CustomerDto>.Fail($"A customer with phone number '{dto.Phone}' already exists."));
            }

            customer.CustomerName = dto.CustomerName.Trim();
            customer.Email = dto.Email.Trim();
            customer.Phone = dto.Phone?.Trim();
            customer.CompanyName = dto.CompanyName?.Trim();
            customer.Address = dto.Address?.Trim();
            customer.City = dto.City?.Trim();
            customer.State = dto.State?.Trim();
            customer.Status = dto.Status.Trim();

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync("UpdateCustomerApi", "Customer", customer.CustomerId.ToString(), null, customer.CustomerName, HttpContext.Connection.RemoteIpAddress?.ToString(), "Success", $"Updated customer {customer.CustomerName} via API");

            var resultDto = new CustomerDto
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                CreatedDate = customer.CreatedDate,
                CreatedBy = customer.CreatedBy
            };

            return Ok(ApiResponse<CustomerDto>.Ok(resultDto, "Customer updated successfully."));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _scopeService.ApplyCustomerScope(_context.Customers, User)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound(ApiResponse<string>.Fail($"Customer with ID {id} was not found."));
            }

            customer.Status = "Inactive";
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync("DeleteCustomerApi", "Customer", id.ToString(), customer.CustomerName, "Inactive", HttpContext.Connection.RemoteIpAddress?.ToString(), "Success", $"Deactivated customer {customer.CustomerName} via API");

            return Ok(ApiResponse<string>.Ok($"Customer '{customer.CustomerName}' has been deactivated.", "Customer deactivated successfully."));
        }
    }
}
