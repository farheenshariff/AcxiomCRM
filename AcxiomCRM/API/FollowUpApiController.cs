using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api
{
    [Route("api/followups")]
    [ApiController]
    [Authorize]
    public class FollowUpApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmDataScopeService _scopeService;
        private readonly IAuditLogService _auditLogService;

        public FollowUpApiController(
            ApplicationDbContext context,
            ICrmDataScopeService scopeService,
            IAuditLogService auditLogService)
        {
            _context = context;
            _scopeService = scopeService;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetFollowUps([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _scopeService.ApplyFollowUpScope(
                _context.FollowUps
                    .Include(f => f.Customer)
                    .Include(f => f.Lead)
                    .AsNoTracking(), User);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(f => f.Subject.ToLower().Contains(s)
                                      || (f.Customer != null && f.Customer.CustomerName.ToLower().Contains(s))
                                      || (f.Lead != null && f.Lead.LeadName.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(f => f.Status.ToLower() == status.Trim().ToLower());
            }

            var totalCount = await query.CountAsync();
            var followUps = await query
                .OrderByDescending(f => f.FollowUpDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FollowUpDto
                {
                    FollowUpId = f.FollowUpId,
                    CustomerId = f.CustomerId,
                    CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
                    LeadId = f.LeadId,
                    LeadName = f.Lead != null ? f.Lead.LeadName : null,
                    FollowUpDate = f.FollowUpDate,
                    FollowUpType = f.FollowUpType,
                    Subject = f.Subject,
                    Remarks = f.Remarks,
                    Status = f.Status,
                    AssignedTo = f.AssignedTo
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                TotalItems = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Items = followUps
            }, "Follow-ups retrieved successfully."));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetFollowUp(int id)
        {
            var f = await _scopeService.ApplyFollowUpScope(
                _context.FollowUps
                    .Include(x => x.Customer)
                    .Include(x => x.Lead)
                    .AsNoTracking(), User)
                .FirstOrDefaultAsync(x => x.FollowUpId == id);

            if (f == null)
            {
                return NotFound(ApiResponse<FollowUpDto>.Fail($"Follow-up with ID {id} was not found."));
            }

            var dto = new FollowUpDto
            {
                FollowUpId = f.FollowUpId,
                CustomerId = f.CustomerId,
                CustomerName = f.Customer?.CustomerName,
                LeadId = f.LeadId,
                LeadName = f.Lead?.LeadName,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Subject = f.Subject,
                Remarks = f.Remarks,
                Status = f.Status,
                AssignedTo = f.AssignedTo
            };

            return Ok(ApiResponse<FollowUpDto>.Ok(dto));
        }

        [HttpPost]
        public async Task<IActionResult> CreateFollowUp([FromBody] CreateFollowUpDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<FollowUpDto>.Fail("Validation failed.", errors));
            }

            var assignedTo = !string.IsNullOrWhiteSpace(dto.AssignedTo) ? dto.AssignedTo : User.Identity?.Name;

            var followUp = new FollowUp
            {
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                FollowUpDate = dto.FollowUpDate,
                FollowUpType = string.IsNullOrWhiteSpace(dto.FollowUpType) ? "Call" : dto.FollowUpType.Trim(),
                Subject = dto.Subject.Trim(),
                Remarks = dto.Remarks?.Trim(),
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Pending" : dto.Status.Trim(),
                AssignedTo = assignedTo
            };

            _context.FollowUps.Add(followUp);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync("CreateFollowUpApi", "FollowUp", followUp.FollowUpId.ToString(), null, followUp.Subject, HttpContext.Connection.RemoteIpAddress?.ToString(), "Success", $"Created follow-up '{followUp.Subject}' via API");

            var customerName = dto.CustomerId.HasValue ? (await _context.Customers.FindAsync(dto.CustomerId.Value))?.CustomerName : null;
            var leadName = dto.LeadId.HasValue ? (await _context.Leads.FindAsync(dto.LeadId.Value))?.LeadName : null;

            var resultDto = new FollowUpDto
            {
                FollowUpId = followUp.FollowUpId,
                CustomerId = followUp.CustomerId,
                CustomerName = customerName,
                LeadId = followUp.LeadId,
                LeadName = leadName,
                FollowUpDate = followUp.FollowUpDate,
                FollowUpType = followUp.FollowUpType,
                Subject = followUp.Subject,
                Remarks = followUp.Remarks,
                Status = followUp.Status,
                AssignedTo = followUp.AssignedTo
            };

            return CreatedAtAction(nameof(GetFollowUp), new { id = followUp.FollowUpId }, ApiResponse<FollowUpDto>.Ok(resultDto, "Follow-up created successfully."));
        }
    }
}
