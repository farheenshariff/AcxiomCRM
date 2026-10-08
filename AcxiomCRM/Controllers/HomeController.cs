using AcxiomCRM.Authorization;
using AcxiomCRM.Data;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmDataScopeService _scopeService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ApplicationDbContext context,
            ICrmDataScopeService scopeService,
            ILogger<HomeController> logger)
        {
            _context = context;
            _scopeService = scopeService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userRole = User.IsInRole(Roles.Admin) ? Roles.Admin :
                           User.IsInRole(Roles.Manager) ? Roles.Manager : Roles.SalesExecutive;

            var bannerText = userRole switch
            {
                Roles.Admin => "Viewing: Full System Enterprise Overview (Admin Scope)",
                Roles.Manager => "Viewing: Sales Team Pipeline & Metrics (Manager Scope)",
                _ => "Viewing: My Assigned Leads, Customers & Opportunities (Sales Exec Scope)"
            };

            // Apply Role Data Scoping to all queries
            var customersQuery = _scopeService.ApplyCustomerScope(_context.Customers.AsNoTracking(), User);
            var leadsQuery = _scopeService.ApplyLeadScope(_context.Leads.AsNoTracking(), User);
            var oppsQuery = _scopeService.ApplyOpportunityScope(_context.Opportunities.AsNoTracking(), User);
            var followUpsQuery = _scopeService.ApplyFollowUpScope(_context.FollowUps.AsNoTracking(), User);
            var activitiesQuery = _scopeService.ApplyActivityScope(_context.Activities.AsNoTracking(), User);

            // Calculations
            var totalCustomers = await customersQuery.CountAsync();
            var totalLeads = await leadsQuery.CountAsync();
            var openLeads = await leadsQuery.CountAsync(l => l.Status == "New" || l.Status == "Contacted" || l.Status == "Qualified");

            var allOpps = await oppsQuery.ToListAsync();
            var totalOpps = allOpps.Count;
            var openOpps = allOpps.Where(o => o.Status == "Open").ToList();
            var wonOpps = allOpps.Where(o => o.Stage == "Won" || o.Status == "Won").ToList();
            var lostOpps = allOpps.Where(o => o.Stage == "Lost" || o.Status == "Lost").ToList();

            var totalPipeline = openOpps.Sum(o => o.Amount);
            var weightedPipeline = openOpps.Sum(o => o.Amount * (o.Probability / 100.0m));

            var now = DateTime.UtcNow;
            var pendingFollowUps = await followUpsQuery.CountAsync(f => f.Status == "Planned");
            var overdueFollowUps = await followUpsQuery.CountAsync(f => f.Status == "Planned" && f.FollowUpDate < now);

            // Lists for Widgets
            var recentOpps = await _scopeService.ApplyOpportunityScope(_context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .AsNoTracking(), User)
                .OrderByDescending(o => o.CreatedDate)
                .Take(5)
                .ToListAsync();

            var upcomingFollowUps = await _scopeService.ApplyFollowUpScope(_context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .AsNoTracking(), User)
                .Where(f => f.Status == "Planned")
                .OrderBy(f => f.FollowUpDate)
                .Take(5)
                .ToListAsync();

            var recentActivities = await _scopeService.ApplyActivityScope(_context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .AsNoTracking(), User)
                .OrderByDescending(a => a.ActivityDate)
                .Take(5)
                .ToListAsync();

            // Chart 1: Lead Status Breakdown
            var allLeads = await leadsQuery.ToListAsync();
            var leadStatusData = new Dictionary<string, int>
            {
                { "New", allLeads.Count(l => l.Status == "New") },
                { "Contacted", allLeads.Count(l => l.Status == "Contacted") },
                { "Qualified", allLeads.Count(l => l.Status == "Qualified") },
                { "Unqualified", allLeads.Count(l => l.Status == "Unqualified") },
                { "Converted", allLeads.Count(l => l.Status == "Converted") },
                { "Lost", allLeads.Count(l => l.Status == "Lost") }
            };

            // Chart 2: Opportunity Stage Breakdown
            var oppStageData = new Dictionary<string, int>
            {
                { "Qualification", allOpps.Count(o => o.Stage == "Qualification") },
                { "Proposal", allOpps.Count(o => o.Stage == "Proposal") },
                { "Negotiation", allOpps.Count(o => o.Stage == "Negotiation") },
                { "Won", allOpps.Count(o => o.Stage == "Won") },
                { "Lost", allOpps.Count(o => o.Stage == "Lost") }
            };

            // Chart 3: Monthly Sales (Past 6 months)
            var monthlySales = new Dictionary<string, decimal>();
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = DateTime.UtcNow.AddMonths(-i);
                var monthName = monthDate.ToString("MMM yyyy");
                var wonAmountInMonth = allOpps
                    .Where(o => (o.Stage == "Won" || o.Status == "Won") &&
                                o.CreatedDate.Month == monthDate.Month &&
                                o.CreatedDate.Year == monthDate.Year)
                    .Sum(o => o.Amount);
                monthlySales[monthName] = wonAmountInMonth;
            }

            var viewModel = new DashboardViewModel
            {
                UserRole = userRole,
                ScopeBannerText = bannerText,
                TotalCustomers = totalCustomers,
                TotalLeads = totalLeads,
                OpenLeads = openLeads,
                TotalOpportunities = totalOpps,
                OpenOpportunities = openOpps.Count,
                WonOpportunities = wonOpps.Count,
                LostOpportunities = lostOpps.Count,
                TotalPipelineValue = totalPipeline,
                WeightedPipelineValue = weightedPipeline,
                PendingFollowUps = pendingFollowUps,
                OverdueFollowUps = overdueFollowUps,
                RecentOpportunities = recentOpps,
                UpcomingFollowUps = upcomingFollowUps,
                RecentActivities = recentActivities,
                LeadStatusChartData = leadStatusData,
                OpportunityStageChartData = oppStageData,
                MonthlySalesChartData = monthlySales
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
