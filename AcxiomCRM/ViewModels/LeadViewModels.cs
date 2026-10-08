using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels
{
    public class LeadListViewModel
    {
        public PaginatedList<Lead> Leads { get; set; } = null!;
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public string? PriorityFilter { get; set; }
        public string? AssignedUserFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public IEnumerable<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();
    }

    public class LeadCreateViewModel
    {
        [Required(ErrorMessage = "Lead Code is required")]
        [StringLength(50)]
        [Display(Name = "Lead Code")]
        public string LeadCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lead Name is required")]
        [StringLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters")]
        [Display(Name = "Lead Name")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email Address is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address format")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number is required")]
        [RegularExpression(@"^[\+]?[(]?[0-9]{3}[)]?[-\s\.]?[0-9]{3}[-\s\.]?[0-9]{4,6}$", ErrorMessage = "Invalid Phone Number format")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [StringLength(50)]
        public string? Source { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [StringLength(20)]
        public string Status { get; set; } = "New";

        [Required(ErrorMessage = "Priority is required")]
        [StringLength(20)]
        public string Priority { get; set; } = "Medium";

        [Range(0, 1000000000, ErrorMessage = "Expected Value must be between 0 and 1,000,000,000")]
        [Display(Name = "Expected Value ($)")]
        public decimal ExpectedValue { get; set; }

        [Display(Name = "Assigned To User")]
        public string? AssignedTo { get; set; }

        public IEnumerable<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();
    }

    public class LeadEditViewModel
    {
        public int LeadId { get; set; }

        [Required(ErrorMessage = "Lead Code is required")]
        [StringLength(50)]
        public string LeadCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lead Name is required")]
        [StringLength(100)]
        [Display(Name = "Lead Name")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email Address is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address format")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number is required")]
        [RegularExpression(@"^[\+]?[(]?[0-9]{3}[)]?[-\s\.]?[0-9]{3}[-\s\.]?[0-9]{4,6}$", ErrorMessage = "Invalid Phone Number format")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [StringLength(50)]
        public string? Source { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "New";

        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "Medium";

        [Range(0, 1000000000)]
        [Display(Name = "Expected Value ($)")]
        public decimal ExpectedValue { get; set; }

        [Display(Name = "Assigned To User")]
        public string? AssignedTo { get; set; }

        public IEnumerable<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();
    }

    public class LeadConvertViewModel
    {
        public int LeadId { get; set; }
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public decimal ExpectedValue { get; set; }

        [Display(Name = "Create New Customer Account")]
        public bool CreateCustomer { get; set; } = true;

        [Display(Name = "Create Sales Opportunity")]
        public bool CreateOpportunity { get; set; } = true;

        [Display(Name = "Opportunity Name")]
        [StringLength(150)]
        public string? OpportunityName { get; set; }

        [Display(Name = "Opportunity Amount ($)")]
        public decimal OpportunityAmount { get; set; }

        [Display(Name = "Initial Stage")]
        public string OpportunityStage { get; set; } = "Qualification";

        [Display(Name = "Expected Close Date")]
        public DateTime? ExpectedCloseDate { get; set; }
    }

    public class LeadDetailsViewModel
    {
        public Lead Lead { get; set; } = null!;
        public IEnumerable<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
        public IEnumerable<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public IEnumerable<Activity> Activities { get; set; } = new List<Activity>();
    }
}
