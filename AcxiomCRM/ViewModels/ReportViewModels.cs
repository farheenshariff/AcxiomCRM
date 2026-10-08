using AcxiomCRM.DTOs;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels
{
    public class ReportFilterViewModel
    {
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? Stage { get; set; }
        public string? AssignedTo { get; set; }
        public string ReportType { get; set; } = "Pipeline";
    }

    public class ReportDashboardViewModel
    {
        public ReportFilterViewModel Filter { get; set; } = new ReportFilterViewModel();
        public PipelineReportDto Pipeline { get; set; } = new PipelineReportDto();
        public CustomerReportSummaryDto CustomerSummary { get; set; } = new CustomerReportSummaryDto();
        public LeadConversionReportDto LeadConversion { get; set; } = new LeadConversionReportDto();
        public FollowUpSummaryReportDto FollowUpSummary { get; set; } = new FollowUpSummaryReportDto();
        public List<UserActivityReportDto> UserActivities { get; set; } = new List<UserActivityReportDto>();
    }
}
