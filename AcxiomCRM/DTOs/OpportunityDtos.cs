using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.DTOs
{
    public class OpportunityDto
    {
        public int OpportunityId { get; set; }
        public string OpportunityName { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? LeadId { get; set; }
        public string? LeadName { get; set; }
        public decimal Amount { get; set; }
        public string Stage { get; set; } = "Prospecting";
        public int Probability { get; set; }
        public DateTime? ExpectedCloseDate { get; set; }
        public string Status { get; set; } = "Open";
        public DateTime CreatedDate { get; set; }
        public string? AssignedTo { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateOpportunityDto
    {
        [Required]
        [MaxLength(150)]
        public string OpportunityName { get; set; } = string.Empty;

        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }

        [Range(0, 999999999999.99)]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        public string Stage { get; set; } = "Prospecting";

        [Range(0, 100)]
        public int Probability { get; set; } = 10;

        public DateTime? ExpectedCloseDate { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Open";

        [MaxLength(100)]
        public string? AssignedTo { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    public class UpdateOpportunityDto : CreateOpportunityDto
    {
    }
}
