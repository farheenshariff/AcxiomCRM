using AcxiomCRM.Data;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api
{
    [Route("api/dashboard")]
    [ApiController]
    [Authorize]
    public class DashboardApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmDataScopeService _scopeService;

        public DashboardApiController(ApplicationDbContext context, ICrmDataScopeService scopeService)
        {
            _context = context;
            _scopeService = scopeService;
        }

        [HttpGet("metrics")]
        public async Task<IActionResult> GetMetrics()
        {
            var customers = await _scopeService.ApplyCustomerScope(_context.Customers.AsNoTracking(), User).CountAsync();
            var leads = await _scopeService.ApplyLeadScope(_context.Leads.AsNoTracking(), User).CountAsync();
            var opps = await _scopeService.ApplyOpportunityScope(_context.Opportunities.AsNoTracking(), User).Where(o => o.Status == "Open").ToListAsync();
            var followUps = await _scopeService.ApplyFollowUpScope(_context.FollowUps.AsNoTracking(), User).CountAsync(f => f.Status == "Pending" || f.Status == "Planned");

            return Ok(new
            {
                TotalCustomers = customers,
                TotalLeads = leads,
                OpenOpportunitiesCount = opps.Count,
                TotalPipelineValue = opps.Sum(o => o.Amount),
                WeightedPipelineValue = opps.Sum(o => o.Amount * (o.Probability / 100.0m)),
                PendingFollowUpsCount = followUps
            });
        }

        [HttpGet("charts/pipeline")]
        public async Task<IActionResult> GetPipelineData()
        {
            var opps = await _scopeService.ApplyOpportunityScope(_context.Opportunities.AsNoTracking(), User).ToListAsync();
            var data = opps
                .GroupBy(o => o.Stage)
                .Select(g => new { Stage = g.Key, Count = g.Count(), TotalValue = g.Sum(x => x.Amount) })
                .ToList();

            return Ok(data);
        }

        [HttpGet("charts/leads-by-status")]
        public async Task<IActionResult> GetLeadStatusData()
        {
            var leads = await _scopeService.ApplyLeadScope(_context.Leads.AsNoTracking(), User).ToListAsync();
            var data = leads
                .GroupBy(l => l.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            return Ok(data);
        }
    }
}
