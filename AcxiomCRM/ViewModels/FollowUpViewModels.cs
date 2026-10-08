using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels
{
    public class FollowUpListViewModel
    {
        public PaginatedList<FollowUp> FollowUps { get; set; } = null!;
        public string ActiveTab { get; set; } = "Pending";
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public string? AssignedUserFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PendingCount { get; set; }
        public int OverdueCount { get; set; }
        public int UpcomingCount { get; set; }
        public int CompletedCount { get; set; }
        public IEnumerable<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();
    }

    public class FollowUpCreateViewModel
    {
        [Display(Name = "Associated Customer")]
        public int? CustomerId { get; set; }

        [Display(Name = "Associated Lead")]
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Follow-Up Date & Time is required")]
        [Display(Name = "Scheduled Date & Time")]
        public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.AddDays(1);

        [Required(ErrorMessage = "Type is required")]
        [StringLength(50)]
        [Display(Name = "Follow-Up Type")]
        public string FollowUpType { get; set; } = "Call";

        [Required(ErrorMessage = "Subject is required")]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters")]
        public string Subject { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Remarks { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Planned";

        [Display(Name = "Assigned To User")]
        public string? AssignedTo { get; set; }

        public IEnumerable<SelectListItem> CustomersList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> LeadsList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();
    }

    public class FollowUpEditViewModel
    {
        public int FollowUpId { get; set; }

        [Display(Name = "Associated Customer")]
        public int? CustomerId { get; set; }

        [Display(Name = "Associated Lead")]
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Follow-Up Date & Time is required")]
        [Display(Name = "Scheduled Date & Time")]
        public DateTime FollowUpDate { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Follow-Up Type")]
        public string FollowUpType { get; set; } = "Call";

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Remarks { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Planned";

        [Display(Name = "Assigned To User")]
        public string? AssignedTo { get; set; }

        public IEnumerable<SelectListItem> CustomersList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> LeadsList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();
    }

    public class FollowUpCompleteViewModel
    {
        public int FollowUpId { get; set; }
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Completion notes/remarks are required")]
        [StringLength(1000)]
        [Display(Name = "Outcome & Completion Notes")]
        public string CompletionRemarks { get; set; } = string.Empty;
    }

    public class FollowUpRescheduleViewModel
    {
        public int FollowUpId { get; set; }
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "New Follow-Up Date is required")]
        [Display(Name = "New Scheduled Date & Time")]
        public DateTime NewFollowUpDate { get; set; } = DateTime.UtcNow.AddDays(2);

        [StringLength(1000)]
        [Display(Name = "Reason for Rescheduling")]
        public string? RescheduleReason { get; set; }
    }
}
