using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api
{
    [Route("api/leads")]
    [ApiController]
    [Authorize]
    public class LeadApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmDataScopeService _scopeService;
        private readonly IAuditLogService _auditLogService;

        public LeadApiController(
            ApplicationDbContext context,
            ICrmDataScopeService scopeService,
            IAuditLogService auditLogService)
        {
            _context = context;
            _scopeService = scopeService;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetLeads([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _scopeService.ApplyLeadScope(_context.Leads.AsNoTracking(), User);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(l => l.LeadName.ToLower().Contains(s)
                                      || l.Email.ToLower().Contains(s)
                                      || (l.CompanyName != null && l.CompanyName.ToLower().Contains(s))
                                      || (l.LeadCode != null && l.LeadCode.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(l => l.Status.ToLower() == status.Trim().ToLower());
            }

            var totalCount = await query.CountAsync();
            var leads = await query
                .OrderByDescending(l => l.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new LeadDto
                {
                    LeadId = l.LeadId,
                    LeadCode = l.LeadCode,
                    LeadName = l.LeadName,
                    Email = l.Email,
                    Phone = l.Phone,
                    CompanyName = l.CompanyName,
                    Source = l.Source,
                    Status = l.Status,
                    Priority = l.Priority,
                    ExpectedValue = l.ExpectedValue,
                    CreatedDate = l.CreatedDate,
                    AssignedTo = l.AssignedTo
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                TotalItems = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Items = leads
            }, "Leads retrieved successfully."));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetLead(int id)
        {
            var lead = await _scopeService.ApplyLeadScope(_context.Leads.AsNoTracking(), User)
                .FirstOrDefaultAsync(l => l.LeadId == id);

            if (lead == null)
            {
                return NotFound(ApiResponse<LeadDto>.Fail($"Lead with ID {id} was not found."));
            }

            var dto = new LeadDto
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Status = lead.Status,
                Priority = lead.Priority,
                ExpectedValue = lead.ExpectedValue,
                CreatedDate = lead.CreatedDate,
                AssignedTo = lead.AssignedTo
            };

            return Ok(ApiResponse<LeadDto>.Ok(dto));
        }

        [HttpPost]
        public async Task<IActionResult> CreateLead([FromBody] CreateLeadDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<LeadDto>.Fail("Validation failed.", errors));
            }

            // Check duplicate Email in Leads
            if (await _context.Leads.AnyAsync(l => l.Email.ToLower() == dto.Email.ToLower()))
            {
                return Conflict(ApiResponse<LeadDto>.Fail($"A lead with email '{dto.Email}' already exists."));
            }

            var code = $"LEAD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
            var assignedTo = !string.IsNullOrWhiteSpace(dto.AssignedTo) ? dto.AssignedTo : User.Identity?.Name;

            var lead = new Lead
            {
                LeadCode = code,
                LeadName = dto.LeadName.Trim(),
                Email = dto.Email.Trim(),
                Phone = dto.Phone?.Trim(),
                CompanyName = dto.CompanyName?.Trim(),
                Source = dto.Source?.Trim() ?? "Web",
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "New" : dto.Status.Trim(),
                Priority = string.IsNullOrWhiteSpace(dto.Priority) ? "Medium" : dto.Priority.Trim(),
                ExpectedValue = dto.ExpectedValue,
                CreatedDate = DateTime.UtcNow,
                AssignedTo = assignedTo
            };

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync("CreateLeadApi", "Lead", lead.LeadId.ToString(), null, lead.LeadName, HttpContext.Connection.RemoteIpAddress?.ToString(), "Success", $"Created lead {lead.LeadName} via API");

            var resultDto = new LeadDto
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Status = lead.Status,
                Priority = lead.Priority,
                ExpectedValue = lead.ExpectedValue,
                CreatedDate = lead.CreatedDate,
                AssignedTo = lead.AssignedTo
            };

            return CreatedAtAction(nameof(GetLead), new { id = lead.LeadId }, ApiResponse<LeadDto>.Ok(resultDto, "Lead created successfully."));
        }
    }
}
