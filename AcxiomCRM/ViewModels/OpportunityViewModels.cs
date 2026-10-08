using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels
{
    public class OpportunityListViewModel
    {
        public PaginatedList<Opportunity> Opportunities { get; set; } = null!;
        public string? SearchTerm { get; set; }
        public string? StageFilter { get; set; }
        public string? StatusFilter { get; set; }
        public string? AssignedUserFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public decimal TotalPipelineValue { get; set; }
        public decimal TotalWeightedValue { get; set; }
        public IEnumerable<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();
    }

    public class OpportunityCreateViewModel
    {
        [Required(ErrorMessage = "Opportunity Name is required")]
        [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters")]
        [Display(Name = "Opportunity Name")]
        public string OpportunityName { get; set; } = string.Empty;

        [Display(Name = "Associated Customer")]
        public int? CustomerId { get; set; }

        [Display(Name = "Associated Lead")]
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0, 1000000000, ErrorMessage = "Amount cannot be negative")]
        [Display(Name = "Opportunity Amount ($)")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Stage is required")]
        [StringLength(50)]
        public string Stage { get; set; } = "Qualification";

        [Required(ErrorMessage = "Probability is required")]
        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100%")]
        [Display(Name = "Win Probability (%)")]
        public int Probability { get; set; } = 50;

        [Display(Name = "Expected Close Date")]
        public DateTime? ExpectedCloseDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Open";

        [Display(Name = "Assigned To User")]
        public string? AssignedTo { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public IEnumerable<SelectListItem> CustomersList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> LeadsList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();
    }

    public class OpportunityEditViewModel
    {
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Opportunity Name is required")]
        [StringLength(150)]
        [Display(Name = "Opportunity Name")]
        public string OpportunityName { get; set; } = string.Empty;

        [Display(Name = "Associated Customer")]
        public int? CustomerId { get; set; }

        [Display(Name = "Associated Lead")]
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0, 1000000000, ErrorMessage = "Amount cannot be negative")]
        [Display(Name = "Opportunity Amount ($)")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Stage is required")]
        [StringLength(50)]
        public string Stage { get; set; } = "Qualification";

        [Required]
        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100%")]
        [Display(Name = "Win Probability (%)")]
        public int Probability { get; set; }

        [Display(Name = "Expected Close Date")]
        public DateTime? ExpectedCloseDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Open";

        [Display(Name = "Assigned To User")]
        public string? AssignedTo { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public IEnumerable<SelectListItem> CustomersList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> LeadsList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();
    }

    public class OpportunityDetailsViewModel
    {
        public Opportunity Opportunity { get; set; } = null!;
        public decimal WeightedValue => Opportunity.Amount * (Opportunity.Probability / 100.0m);
    }

    public class PipelineBoardViewModel
    {
        public List<Opportunity> QualificationStage { get; set; } = new();
        public List<Opportunity> ProposalStage { get; set; } = new();
        public List<Opportunity> NegotiationStage { get; set; } = new();
        public List<Opportunity> WonStage { get; set; } = new();
        public List<Opportunity> LostStage { get; set; } = new();
    }
}
