using System.ComponentModel.DataAnnotations;
using WebApplication1.Models;

namespace WebApplication1.ViewModels
{
    public class ClaimViewModel
    {
        [Required(ErrorMessage = "Month is required")]
        [Display(Name = "Claim Month")]
        [DataType(DataType.Date)]
        public DateTime Month { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        [Required(ErrorMessage = "Hours worked is required")]
        [Display(Name = "Hours Worked")]
        [Range(0.1, 200, ErrorMessage = "Hours worked must be between 0.1 and 200")]
        public decimal HoursWorked { get; set; }

        [Required(ErrorMessage = "Hourly rate is required")]
        [Display(Name = "Hourly Rate (R)")]
        [Range(50, 1000, ErrorMessage = "Hourly rate must be between R50 and R1000")]
        public decimal HourlyRate { get; set; }

        [Display(Name = "Additional Notes")]
        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? AdditionalNotes { get; set; }

        [Display(Name = "Supporting Document")]
        public IFormFile? SupportingDocument { get; set; }

        // Computed property for display
        [Display(Name = "Total Amount")]
        public decimal TotalAmount => HoursWorked * HourlyRate;
    }

    public class ApprovalViewModel
    {
        public Guid ClaimId { get; set; }
        public ClaimModel? Claim { get; set; }

        [Required(ErrorMessage = "Approval notes are required")]
        [Display(Name = "Approval Notes")]
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string ApprovalNotes { get; set; } = string.Empty;

        [Display(Name = "Action")]
        public ApprovalAction Action { get; set; }
    }

    public enum ApprovalAction
    {
        Approve,
        Reject,
        RequestMoreInfo
    }
}