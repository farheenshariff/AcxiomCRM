using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class DashboardViewModel
    {
        public string UserRole { get; set; } = "SalesExecutive";
        public string ScopeBannerText { get; set; } = string.Empty;

        // Metric Card Counts & Values
        public int TotalCustomers { get; set; }
        public int TotalLeads { get; set; }
        public int OpenLeads { get; set; }
        public int TotalOpportunities { get; set; }
        public int OpenOpportunities { get; set; }
        public int WonOpportunities { get; set; }
        public int LostOpportunities { get; set; }
        public decimal TotalPipelineValue { get; set; }
        public decimal WeightedPipelineValue { get; set; }
        public int PendingFollowUps { get; set; }
        public int OverdueFollowUps { get; set; }

        // Collections for Lists & Widgets
        public IEnumerable<Opportunity> RecentOpportunities { get; set; } = new List<Opportunity>();
        public IEnumerable<FollowUp> UpcomingFollowUps { get; set; } = new List<FollowUp>();
        public IEnumerable<Activity> RecentActivities { get; set; } = new List<Activity>();

        // Chart 1: Lead Status Breakdown
        public Dictionary<string, int> LeadStatusChartData { get; set; } = new();

        // Chart 2: Opportunity Stage Breakdown
        public Dictionary<string, int> OpportunityStageChartData { get; set; } = new();

        // Chart 3: Monthly Opportunity Sales Outcomes (Month Name -> Total Value)
        public Dictionary<string, decimal> MonthlySalesChartData { get; set; } = new();
    }
}
