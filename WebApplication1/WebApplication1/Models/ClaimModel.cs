using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class ClaimModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string LecturerId { get; set; } = string.Empty;

        [ForeignKey("LecturerId")]
        public virtual ApplicationUser? Lecturer { get; set; }

        [Required]
        [Display(Name = "Month")]
        [DataType(DataType.Date)]
        public DateTime Month { get; set; }

        [Required]
        [Display(Name = "Hours Worked")]
        [Range(0.1, 200, ErrorMessage = "Hours worked must be between 0.1 and 200")]
        public decimal HoursWorked { get; set; }

        [Required]
        [Display(Name = "Hourly Rate")]
        [Range(50, 1000, ErrorMessage = "Hourly rate must be between R50 and R1000")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }

        [Required]
        [Display(Name = "Total Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Additional Notes")]
        [StringLength(1000)]
        public string? AdditionalNotes { get; set; }

        [Required]
        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        [Display(Name = "Submission Date")]
        public DateTime SubmissionDate { get; set; } = DateTime.Now;

        [Display(Name = "Approval Date")]
        public DateTime? ApprovalDate { get; set; }

        [Display(Name = "Approved/Rejected By")]
        [StringLength(100)]
        public string? ApprovedBy { get; set; }

        [Display(Name = "Approval Notes")]
        [StringLength(500)]
        public string? ApprovalNotes { get; set; }

        // Navigation properties
        public virtual ICollection<SupportingDocument> SupportingDocuments { get; set; } = new List<SupportingDocument>();
        public virtual ICollection<ClaimStatusHistory> StatusHistory { get; set; } = new List<ClaimStatusHistory>();

        // Computed properties with proper null handling
        [Display(Name = "Processing Time")]
        public TimeSpan? ProcessingTime
        {
            get
            {
                return ApprovalDate.HasValue ? ApprovalDate.Value - SubmissionDate : null;
            }
        }

        [Display(Name = "Processing Days")]
        public double? ProcessingDays
        {
            get
            {
                return ProcessingTime?.TotalDays;
            }
        }

        [Display(Name = "Formatted Month")]
        public string FormattedMonth => Month.ToString("MMMM yyyy");

        [Display(Name = "Status Badge Class")]
        public string StatusBadgeClass => Status switch
        {
            ClaimStatus.Pending => "bg-warning",
            ClaimStatus.Approved => "bg-success",
            ClaimStatus.Rejected => "bg-danger",
            ClaimStatus.UnderReview => "bg-info",
            _ => "bg-secondary"
        };

        // Methods
        public void CalculateTotalAmount()
        {
            TotalAmount = HoursWorked * HourlyRate;
        }

        public void UpdateStatus(ClaimStatus newStatus, string? notes = null, string? approvedBy = null)
        {
            var oldStatus = Status;
            Status = newStatus;

            if (newStatus == ClaimStatus.Approved || newStatus == ClaimStatus.Rejected)
            {
                ApprovalDate = DateTime.Now;
                ApprovedBy = approvedBy ?? "Unknown";
                ApprovalNotes = notes;
            }

            // Add to status history
            StatusHistory.Add(new ClaimStatusHistory
            {
                Id = Guid.NewGuid(),
                ClaimId = Id,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedBy = approvedBy ?? "Unknown",
                ChangeNotes = notes,
                ChangedDate = DateTime.Now
            });
        }
    }

    public enum ClaimStatus
    {
        Pending,
        UnderReview,
        Approved,
        Rejected,
        Paid
    }
}