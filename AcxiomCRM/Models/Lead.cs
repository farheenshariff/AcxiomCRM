using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class Lead
    {
        [Key]
        public int LeadId { get; set; }

        [Required]
        [MaxLength(50)]
        public string LeadCode { get; set; } = string.Empty;

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

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "New";

        [Required]
        [MaxLength(20)]
        public string Priority { get; set; } = "Medium";

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ExpectedValue { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? AssignedTo { get; set; }

        // Navigation properties
        public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
        public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
