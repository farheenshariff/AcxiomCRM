using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.DTOs
{
    public class LeadDto
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? CompanyName { get; set; }
        public string? Source { get; set; }
        public string Status { get; set; } = "New";
        public string Priority { get; set; } = "Medium";
        public decimal ExpectedValue { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? AssignedTo { get; set; }
    }

    public class CreateLeadDto
    {
        [Required]
        [MaxLength(100)]
        public string LeadName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? CompanyName { get; set; }

        [MaxLength(50)]
        public string? Source { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "New";

        [MaxLength(20)]
        public string Priority { get; set; } = "Medium";

        public decimal ExpectedValue { get; set; }

        [MaxLength(100)]
        public string? AssignedTo { get; set; }
    }

    public class UpdateLeadDto : CreateLeadDto
    {
    }
}
