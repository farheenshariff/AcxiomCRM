namespace AcxiomCRM.DTOs
{
    public class PipelineReportDto
    {
        public int TotalOpportunities { get; set; }
        public decimal TotalPipelineValue { get; set; }
        public decimal WeightedPipelineValue { get; set; }
        public List<StageBreakdownDto> Stages { get; set; } = new List<StageBreakdownDto>();
    }

    public class StageBreakdownDto
    {
        public string Stage { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
        public decimal WeightedValue { get; set; }
    }

    public class CustomerReportSummaryDto
    {
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int InactiveCustomers { get; set; }
        public List<CityCustomerCountDto> ByCity { get; set; } = new List<CityCustomerCountDto>();
    }

    public class CityCustomerCountDto
    {
        public string City { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class LeadConversionReportDto
    {
        public int TotalLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public decimal ConversionRate { get; set; }
        public List<StatusCountDto> StatusBreakdown { get; set; } = new List<StatusCountDto>();
        public List<SourceCountDto> SourceBreakdown { get; set; } = new List<SourceCountDto>();
    }

    public class StatusCountDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class SourceCountDto
    {
        public string Source { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class FollowUpSummaryReportDto
    {
        public int TotalFollowUps { get; set; }
        public int PendingCount { get; set; }
        public int CompletedCount { get; set; }
        public int OverdueCount { get; set; }
        public List<StatusCountDto> ByType { get; set; } = new List<StatusCountDto>();
    }

    public class UserActivityReportDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int LeadsAssigned { get; set; }
        public int OpportunitiesAssigned { get; set; }
        public int FollowUpsAssigned { get; set; }
        public int CompletedFollowUps { get; set; }
    }
}
