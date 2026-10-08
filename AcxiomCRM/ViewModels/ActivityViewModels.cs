using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels
{
    public class ActivityListViewModel
    {
        public PaginatedList<Activity> Activities { get; set; } = null!;
        public string? SearchTerm { get; set; }
        public string? TypeFilter { get; set; }
        public string? StatusFilter { get; set; }
        public string? AssignedUserFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public IEnumerable<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();
    }

    public class ActivityCreateViewModel
    {
        [Required(ErrorMessage = "Activity Type is required")]
        [StringLength(50)]
        [Display(Name = "Activity Type")]
        public string ActivityType { get; set; } = "Call";

        [Required(ErrorMessage = "Subject is required")]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters")]
        public string Subject { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Activity Date & Time is required")]
        [Display(Name = "Activity Date & Time")]
        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Associated Customer")]
        public int? CustomerId { get; set; }

        [Display(Name = "Associated Lead")]
        public int? LeadId { get; set; }

        [Display(Name = "Assigned To User")]
        public string? AssignedTo { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Completed";

        public IEnumerable<SelectListItem> CustomersList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> LeadsList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();
    }

    public class ActivityEditViewModel
    {
        public int ActivityId { get; set; }

        [Required(ErrorMessage = "Activity Type is required")]
        [StringLength(50)]
        [Display(Name = "Activity Type")]
        public string ActivityType { get; set; } = "Call";

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Activity Date & Time")]
        public DateTime ActivityDate { get; set; }

        [Display(Name = "Associated Customer")]
        public int? CustomerId { get; set; }

        [Display(Name = "Associated Lead")]
        public int? LeadId { get; set; }

        [Display(Name = "Assigned To User")]
        public string? AssignedTo { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Completed";

        public IEnumerable<SelectListItem> CustomersList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> LeadsList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();
    }
}
