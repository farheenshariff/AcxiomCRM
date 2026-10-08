using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api
{
    [Route("api/opportunities")]
    [ApiController]
    [Authorize]
    public class OpportunityApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmDataScopeService _scopeService;
        private readonly IAuditLogService _auditLogService;

        public OpportunityApiController(
            ApplicationDbContext context,
            ICrmDataScopeService scopeService,
            IAuditLogService auditLogService)
        {
            _context = context;
            _scopeService = scopeService;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetOpportunities([FromQuery] string? search, [FromQuery] string? stage, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _scopeService.ApplyOpportunityScope(
                _context.Opportunities
                    .Include(o => o.Customer)
                    .Include(o => o.Lead)
                    .AsNoTracking(), User);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(o => o.OpportunityName.ToLower().Contains(s)
                                      || (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(s))
                                      || (o.Lead != null && o.Lead.LeadName.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(stage))
            {
                query = query.Where(o => o.Stage.ToLower() == stage.Trim().ToLower());
            }

            var totalCount = await query.CountAsync();
            var opps = await query
                .OrderByDescending(o => o.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OpportunityDto
                {
                    OpportunityId = o.OpportunityId,
                    OpportunityName = o.OpportunityName,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer != null ? o.Customer.CustomerName : null,
                    LeadId = o.LeadId,
                    LeadName = o.Lead != null ? o.Lead.LeadName : null,
                    Amount = o.Amount,
                    Stage = o.Stage,
                    Probability = o.Probability,
                    ExpectedCloseDate = o.ExpectedCloseDate,
                    Status = o.Status,
                    CreatedDate = o.CreatedDate,
                    AssignedTo = o.AssignedTo,
                    Notes = o.Notes
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                TotalItems = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Items = opps
            }, "Opportunities retrieved successfully."));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOpportunity(int id)
        {
            var opp = await _scopeService.ApplyOpportunityScope(
                _context.Opportunities
                    .Include(o => o.Customer)
                    .Include(o => o.Lead)
                    .AsNoTracking(), User)
                .FirstOrDefaultAsync(o => o.OpportunityId == id);

            if (opp == null)
            {
                return NotFound(ApiResponse<OpportunityDto>.Fail($"Opportunity with ID {id} was not found."));
            }

            var dto = new OpportunityDto
            {
                OpportunityId = opp.OpportunityId,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                CustomerName = opp.Customer?.CustomerName,
                LeadId = opp.LeadId,
                LeadName = opp.Lead?.LeadName,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Status = opp.Status,
                CreatedDate = opp.CreatedDate,
                AssignedTo = opp.AssignedTo,
                Notes = opp.Notes
            };

            return Ok(ApiResponse<OpportunityDto>.Ok(dto));
        }

        [HttpPost]
        public async Task<IActionResult> CreateOpportunity([FromBody] CreateOpportunityDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<OpportunityDto>.Fail("Validation failed.", errors));
            }

            var assignedTo = !string.IsNullOrWhiteSpace(dto.AssignedTo) ? dto.AssignedTo : User.Identity?.Name;

            var opp = new Opportunity
            {
                OpportunityName = dto.OpportunityName.Trim(),
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                Amount = dto.Amount,
                Stage = string.IsNullOrWhiteSpace(dto.Stage) ? "Prospecting" : dto.Stage.Trim(),
                Probability = dto.Probability,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Open" : dto.Status.Trim(),
                CreatedDate = DateTime.UtcNow,
                AssignedTo = assignedTo,
                Notes = dto.Notes?.Trim()
            };

            _context.Opportunities.Add(opp);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync("CreateOpportunityApi", "Opportunity", opp.OpportunityId.ToString(), null, opp.OpportunityName, HttpContext.Connection.RemoteIpAddress?.ToString(), "Success", $"Created opportunity {opp.OpportunityName} via API");

            var customerName = dto.CustomerId.HasValue ? (await _context.Customers.FindAsync(dto.CustomerId.Value))?.CustomerName : null;
            var leadName = dto.LeadId.HasValue ? (await _context.Leads.FindAsync(dto.LeadId.Value))?.LeadName : null;

            var resultDto = new OpportunityDto
            {
                OpportunityId = opp.OpportunityId,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                CustomerName = customerName,
                LeadId = opp.LeadId,
                LeadName = leadName,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Status = opp.Status,
                CreatedDate = opp.CreatedDate,
                AssignedTo = opp.AssignedTo,
                Notes = opp.Notes
            };

            return CreatedAtAction(nameof(GetOpportunity), new { id = opp.OpportunityId }, ApiResponse<OpportunityDto>.Ok(resultDto, "Opportunity created successfully."));
        }
    }
}
