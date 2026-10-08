using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api
{
    [Route("api/reports")]
    [ApiController]
    [Authorize]
    public class ReportsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmDataScopeService _scopeService;

        public ReportsApiController(ApplicationDbContext context, ICrmDataScopeService scopeService)
        {
            _context = context;
            _scopeService = scopeService;
        }

        [HttpGet("pipeline")]
        public async Task<IActionResult> GetPipelineReport()
        {
            var oppsQuery = _scopeService.ApplyOpportunityScope(_context.Opportunities.AsNoTracking(), User);

            var opps = await oppsQuery.ToListAsync();

            var totalCount = opps.Count;
            var totalValue = opps.Sum(o => o.Amount);
            var weightedValue = opps.Sum(o => o.Amount * (o.Probability / 100.0m));

            var stageBreakdown = opps
                .GroupBy(o => o.Stage)
                .Select(g => new StageBreakdownDto
                {
                    Stage = g.Key,
                    Count = g.Count(),
                    TotalValue = g.Sum(x => x.Amount),
                    WeightedValue = g.Sum(x => x.Amount * (x.Probability / 100.0m))
                })
                .OrderBy(s => s.Stage)
                .ToList();

            var report = new PipelineReportDto
            {
                TotalOpportunities = totalCount,
                TotalPipelineValue = totalValue,
                WeightedPipelineValue = weightedValue,
                Stages = stageBreakdown
            };

            return Ok(ApiResponse<PipelineReportDto>.Ok(report, "Pipeline report generated successfully."));
        }
    }
}
