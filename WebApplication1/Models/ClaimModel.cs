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

        // ADD THESE MISSING PROPERTIES:
        [Display(Name = "Last Status Update Date")]
        public DateTime? LastStatusUpdateDate { get; set; }

        [Display(Name = "Reviewed By")]
        [StringLength(100)]
        public string? ReviewedBy { get; set; }

        [Display(Name = "Review Notes")]
        [StringLength(500)]
        public string? ReviewNotes { get; set; }

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
        [NotMapped]
        public TimeSpan? ProcessingTime
        {
            get
            {
                return ApprovalDate.HasValue ? ApprovalDate.Value - SubmissionDate : null;
            }
        }

        [Display(Name = "Processing Days")]
        [NotMapped]
        public double? ProcessingDays
        {
            get
            {
                return ProcessingTime?.TotalDays;
            }
        }

        [Display(Name = "Formatted Month")]
        [NotMapped]
        public string FormattedMonth => Month.ToString("MMMM yyyy");

        [Display(Name = "Status Badge Class")]
        [NotMapped]
        public string StatusBadgeClass => Status switch
        {
            ClaimStatus.Pending => "bg-warning",
            ClaimStatus.Approved => "bg-success",
            ClaimStatus.Rejected => "bg-danger",
            ClaimStatus.UnderReview => "bg-info",
            ClaimStatus.Paid => "bg-primary",
            _ => "bg-secondary"
        };

        // Database-mapped property for processing days (optional - if you want to store it)
        public int? StoredProcessingDays { get; set; }

        // Methods
        public void CalculateTotalAmount()
        {
            TotalAmount = HoursWorked * HourlyRate;
        }

        public void UpdateStatus(ClaimStatus newStatus, string? notes = null, string? reviewedBy = null)
        {
            var oldStatus = Status;
            Status = newStatus;

            // UPDATE: Set the missing properties
            LastStatusUpdateDate = DateTime.Now;
            ReviewedBy = reviewedBy ?? "Unknown";
            ReviewNotes = notes;

            if (newStatus == ClaimStatus.Approved || newStatus == ClaimStatus.Rejected)
            {
                ApprovalDate = DateTime.Now;
                ApprovedBy = reviewedBy ?? "Unknown";
                ApprovalNotes = notes;

                // Calculate and store processing days when approved/rejected
                if (newStatus == ClaimStatus.Approved)
                {
                    StoredProcessingDays = (int)(DateTime.Now - SubmissionDate).TotalDays;
                }
            }

            // Add to status history
            StatusHistory.Add(new ClaimStatusHistory
            {
                Id = Guid.NewGuid(),
                ClaimId = Id,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedBy = reviewedBy ?? "Unknown",
                ChangeNotes = notes,
                ChangedDate = DateTime.Now
            });
        }

        // Helper method to get processing days (preferred approach)
        [NotMapped]
        public int? CalculatedProcessingDays
        {
            get
            {
                if (Status == ClaimStatus.Approved && ApprovalDate.HasValue)
                {
                    return (int)(ApprovalDate.Value - SubmissionDate).TotalDays;
                }
                return null;
            }
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