using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmDataScopeService _scopeService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportController(
            ApplicationDbContext context,
            ICrmDataScopeService scopeService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _scopeService = scopeService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] ReportFilterViewModel filter)
        {
            var model = new ReportDashboardViewModel
            {
                Filter = filter,
                Pipeline = await GetPipelineReportData(filter),
                CustomerSummary = await GetCustomerSummaryData(filter),
                LeadConversion = await GetLeadConversionData(filter),
                FollowUpSummary = await GetFollowUpSummaryData(filter),
                UserActivities = await GetUserActivityData(filter)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Pipeline([FromQuery] ReportFilterViewModel filter)
        {
            var data = await GetPipelineReportData(filter);
            ViewBag.Filter = filter;
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Conversion([FromQuery] ReportFilterViewModel filter)
        {
            var data = await GetLeadConversionData(filter);
            ViewBag.Filter = filter;
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> CustomerSummary([FromQuery] ReportFilterViewModel filter)
        {
            var data = await GetCustomerSummaryData(filter);
            ViewBag.Filter = filter;
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> FollowUpSummary([FromQuery] ReportFilterViewModel filter)
        {
            var data = await GetFollowUpSummaryData(filter);
            ViewBag.Filter = filter;
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> UserActivity([FromQuery] ReportFilterViewModel filter)
        {
            var data = await GetUserActivityData(filter);
            ViewBag.Filter = filter;
            return View(data);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Audit([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string? search)
        {
            var query = _context.AuditLogs.AsNoTracking();

            if (fromDate.HasValue)
                query = query.Where(a => a.CreatedDate >= fromDate.Value.ToUniversalTime());

            if (toDate.HasValue)
                query = query.Where(a => a.CreatedDate <= toDate.Value.ToUniversalTime().AddDays(1));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(a => a.Action.ToLower().Contains(s)
                                      || (a.EntityName != null && a.EntityName.ToLower().Contains(s))
                                      || (a.Details != null && a.Details.ToLower().Contains(s)));
            }

            var logs = await query.OrderByDescending(a => a.CreatedDate).Take(100).ToListAsync();

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.Search = search;

            return View(logs);
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv([FromQuery] string reportType, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var filter = new ReportFilterViewModel { FromDate = fromDate, ToDate = toDate, ReportType = reportType };
            var builder = new StringBuilder();

            switch (reportType?.ToLower())
            {
                case "customer":
                case "customersummary":
                    builder.AppendLine("CustomerCode,CustomerName,Email,Phone,CompanyName,City,State,Status,CreatedDate");
                    var custQuery = _scopeService.ApplyCustomerScope(_context.Customers.AsNoTracking(), User);
                    if (fromDate.HasValue) custQuery = custQuery.Where(c => c.CreatedDate >= fromDate.Value.ToUniversalTime());
                    if (toDate.HasValue) custQuery = custQuery.Where(c => c.CreatedDate <= toDate.Value.ToUniversalTime().AddDays(1));
                    var customers = await custQuery.ToListAsync();
                    foreach (var c in customers)
                    {
                        builder.AppendLine($"\"{c.CustomerCode}\",\"{c.CustomerName}\",\"{c.Email}\",\"{c.Phone}\",\"{c.CompanyName}\",\"{c.City}\",\"{c.State}\",\"{c.Status}\",\"{c.CreatedDate:yyyy-MM-dd HH:mm}\"");
                    }
                    return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"Customer_Report_{DateTime.UtcNow:yyyyMMdd}.csv");

                case "lead":
                case "conversion":
                    builder.AppendLine("LeadCode,LeadName,Email,Phone,CompanyName,Source,Status,Priority,ExpectedValue,AssignedTo,CreatedDate");
                    var leadQuery = _scopeService.ApplyLeadScope(_context.Leads.AsNoTracking(), User);
                    if (fromDate.HasValue) leadQuery = leadQuery.Where(l => l.CreatedDate >= fromDate.Value.ToUniversalTime());
                    if (toDate.HasValue) leadQuery = leadQuery.Where(l => l.CreatedDate <= toDate.Value.ToUniversalTime().AddDays(1));
                    var leads = await leadQuery.ToListAsync();
                    foreach (var l in leads)
                    {
                        builder.AppendLine($"\"{l.LeadCode}\",\"{l.LeadName}\",\"{l.Email}\",\"{l.Phone}\",\"{l.CompanyName}\",\"{l.Source}\",\"{l.Status}\",\"{l.Priority}\",\"{l.ExpectedValue}\",\"{l.AssignedTo}\",\"{l.CreatedDate:yyyy-MM-dd HH:mm}\"");
                    }
                    return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"Lead_Conversion_Report_{DateTime.UtcNow:yyyyMMdd}.csv");

                case "followup":
                case "followupsummary":
                    builder.AppendLine("FollowUpId,Subject,FollowUpType,FollowUpDate,Status,AssignedTo,Remarks");
                    var fuQuery = _scopeService.ApplyFollowUpScope(_context.FollowUps.AsNoTracking(), User);
                    if (fromDate.HasValue) fuQuery = fuQuery.Where(f => f.FollowUpDate >= fromDate.Value.ToUniversalTime());
                    if (toDate.HasValue) fuQuery = fuQuery.Where(f => f.FollowUpDate <= toDate.Value.ToUniversalTime().AddDays(1));
                    var followUps = await fuQuery.ToListAsync();
                    foreach (var f in followUps)
                    {
                        builder.AppendLine($"\"{f.FollowUpId}\",\"{f.Subject}\",\"{f.FollowUpType}\",\"{f.FollowUpDate:yyyy-MM-dd HH:mm}\",\"{f.Status}\",\"{f.AssignedTo}\",\"{f.Remarks}\"");
                    }
                    return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"FollowUp_Report_{DateTime.UtcNow:yyyyMMdd}.csv");

                case "pipeline":
                default:
                    builder.AppendLine("OpportunityId,OpportunityName,Stage,Amount,Probability,WeightedValue,Status,AssignedTo,CreatedDate");
                    var oppQuery = _scopeService.ApplyOpportunityScope(_context.Opportunities.AsNoTracking(), User);
                    if (fromDate.HasValue) oppQuery = oppQuery.Where(o => o.CreatedDate >= fromDate.Value.ToUniversalTime());
                    if (toDate.HasValue) oppQuery = oppQuery.Where(o => o.CreatedDate <= toDate.Value.ToUniversalTime().AddDays(1));
                    var opps = await oppQuery.ToListAsync();
                    foreach (var o in opps)
                    {
                        var weighted = o.Amount * (o.Probability / 100.0m);
                        builder.AppendLine($"\"{o.OpportunityId}\",\"{o.OpportunityName}\",\"{o.Stage}\",\"{o.Amount}\",\"{o.Probability}%\",\"{weighted}\",\"{o.Status}\",\"{o.AssignedTo}\",\"{o.CreatedDate:yyyy-MM-dd HH:mm}\"");
                    }
                    return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"Pipeline_Report_{DateTime.UtcNow:yyyyMMdd}.csv");
            }
        }

        #region Helper Data Fetchers

        private async Task<PipelineReportDto> GetPipelineReportData(ReportFilterViewModel filter)
        {
            var query = _scopeService.ApplyOpportunityScope(_context.Opportunities.AsNoTracking(), User);
            if (filter.FromDate.HasValue) query = query.Where(o => o.CreatedDate >= filter.FromDate.Value.ToUniversalTime());
            if (filter.ToDate.HasValue) query = query.Where(o => o.CreatedDate <= filter.ToDate.Value.ToUniversalTime().AddDays(1));

            var opps = await query.ToListAsync();
            return new PipelineReportDto
            {
                TotalOpportunities = opps.Count,
                TotalPipelineValue = opps.Sum(o => o.Amount),
                WeightedPipelineValue = opps.Sum(o => o.Amount * (o.Probability / 100.0m)),
                Stages = opps.GroupBy(o => o.Stage).Select(g => new StageBreakdownDto
                {
                    Stage = g.Key,
                    Count = g.Count(),
                    TotalValue = g.Sum(x => x.Amount),
                    WeightedValue = g.Sum(x => x.Amount * (x.Probability / 100.0m))
                }).OrderBy(s => s.Stage).ToList()
            };
        }

        private async Task<CustomerReportSummaryDto> GetCustomerSummaryData(ReportFilterViewModel filter)
        {
            var query = _scopeService.ApplyCustomerScope(_context.Customers.AsNoTracking(), User);
            if (filter.FromDate.HasValue) query = query.Where(c => c.CreatedDate >= filter.FromDate.Value.ToUniversalTime());
            if (filter.ToDate.HasValue) query = query.Where(c => c.CreatedDate <= filter.ToDate.Value.ToUniversalTime().AddDays(1));

            var customers = await query.ToListAsync();
            return new CustomerReportSummaryDto
            {
                TotalCustomers = customers.Count,
                ActiveCustomers = customers.Count(c => c.Status == "Active"),
                InactiveCustomers = customers.Count(c => c.Status == "Inactive"),
                ByCity = customers.Where(c => !string.IsNullOrEmpty(c.City))
                                 .GroupBy(c => c.City!)
                                 .Select(g => new CityCustomerCountDto { City = g.Key, Count = g.Count() })
                                 .OrderByDescending(c => c.Count)
                                 .Take(10)
                                 .ToList()
            };
        }

        private async Task<LeadConversionReportDto> GetLeadConversionData(ReportFilterViewModel filter)
        {
            var query = _scopeService.ApplyLeadScope(_context.Leads.AsNoTracking(), User);
            if (filter.FromDate.HasValue) query = query.Where(l => l.CreatedDate >= filter.FromDate.Value.ToUniversalTime());
            if (filter.ToDate.HasValue) query = query.Where(l => l.CreatedDate <= filter.ToDate.Value.ToUniversalTime().AddDays(1));

            var leads = await query.ToListAsync();
            var total = leads.Count;
            var converted = leads.Count(l => l.Status == "Converted");

            return new LeadConversionReportDto
            {
                TotalLeads = total,
                ConvertedLeads = converted,
                ConversionRate = total > 0 ? Math.Round((decimal)converted / total * 100, 2) : 0,
                StatusBreakdown = leads.GroupBy(l => l.Status).Select(g => new StatusCountDto { Status = g.Key, Count = g.Count() }).ToList(),
                SourceBreakdown = leads.GroupBy(l => l.Source ?? "Unknown").Select(g => new SourceCountDto { Source = g.Key, Count = g.Count() }).ToList()
            };
        }

        private async Task<FollowUpSummaryReportDto> GetFollowUpSummaryData(ReportFilterViewModel filter)
        {
            var query = _scopeService.ApplyFollowUpScope(_context.FollowUps.AsNoTracking(), User);
            if (filter.FromDate.HasValue) query = query.Where(f => f.FollowUpDate >= filter.FromDate.Value.ToUniversalTime());
            if (filter.ToDate.HasValue) query = query.Where(f => f.FollowUpDate <= filter.ToDate.Value.ToUniversalTime().AddDays(1));

            var followUps = await query.ToListAsync();
            var now = DateTime.UtcNow;

            return new FollowUpSummaryReportDto
            {
                TotalFollowUps = followUps.Count,
                PendingCount = followUps.Count(f => f.Status == "Pending" || f.Status == "Planned"),
                CompletedCount = followUps.Count(f => f.Status == "Completed"),
                OverdueCount = followUps.Count(f => (f.Status == "Pending" || f.Status == "Planned") && f.FollowUpDate < now),
                ByType = followUps.GroupBy(f => f.FollowUpType ?? "Call").Select(g => new StatusCountDto { Status = g.Key, Count = g.Count() }).ToList()
            };
        }

        private async Task<List<UserActivityReportDto>> GetUserActivityData(ReportFilterViewModel filter)
        {
            var users = await _userManager.Users.AsNoTracking().ToListAsync();
            var list = new List<UserActivityReportDto>();

            var leads = await _context.Leads.AsNoTracking().ToListAsync();
            var opps = await _context.Opportunities.AsNoTracking().ToListAsync();
            var followUps = await _context.FollowUps.AsNoTracking().ToListAsync();

            foreach (var user in users)
            {
                list.Add(new UserActivityReportDto
                {
                    UserId = user.Id,
                    UserName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    LeadsAssigned = leads.Count(l => l.AssignedTo == user.UserName || l.AssignedTo == user.Email),
                    OpportunitiesAssigned = opps.Count(o => o.AssignedTo == user.UserName || o.AssignedTo == user.Email),
                    FollowUpsAssigned = followUps.Count(f => f.AssignedTo == user.UserName || f.AssignedTo == user.Email),
                    CompletedFollowUps = followUps.Count(f => (f.AssignedTo == user.UserName || f.AssignedTo == user.Email) && f.Status == "Completed")
                });
            }

            return list;
        }

        #endregion
    }
}
