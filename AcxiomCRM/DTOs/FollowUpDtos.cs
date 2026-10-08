using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.DTOs
{
    public class FollowUpDto
    {
        public int FollowUpId { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? LeadId { get; set; }
        public string? LeadName { get; set; }
        public DateTime FollowUpDate { get; set; }
        public string? FollowUpType { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public string Status { get; set; } = "Pending";
        public string? AssignedTo { get; set; }
    }

    public class CreateFollowUpDto
    {
        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }

        [Required]
        public DateTime FollowUpDate { get; set; }

        [MaxLength(50)]
        public string? FollowUpType { get; set; }

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Remarks { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        [MaxLength(100)]
        public string? AssignedTo { get; set; }
    }

    public class UpdateFollowUpDto : CreateFollowUpDto
    {
    }
}
