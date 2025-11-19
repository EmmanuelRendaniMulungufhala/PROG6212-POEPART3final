using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class ClaimStatusHistory
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ClaimId { get; set; }

        [ForeignKey("ClaimId")]
        public virtual ClaimModel? Claim { get; set; }

        [Required]
        [Display(Name = "Old Status")]
        public ClaimStatus OldStatus { get; set; }

        [Required]
        [Display(Name = "New Status")]
        public ClaimStatus NewStatus { get; set; }

        [Required]
        [Display(Name = "Changed Date")]
        public DateTime ChangedDate { get; set; } = DateTime.Now;

        [Display(Name = "Changed By")]
        [StringLength(100)]
        public string? ChangedBy { get; set; }

        [Display(Name = "Change Notes")]
        [StringLength(500)]
        public string? ChangeNotes { get; set; }

        // Computed properties
        [Display(Name = "Status Change")]
        public string StatusChange => $"{OldStatus} → {NewStatus}";

        [Display(Name = "Duration")]
        public string Duration => GetDurationString();

        private string GetDurationString()
        {
            var duration = DateTime.Now - ChangedDate;

            // FIXED: Use the double properties of TimeSpan directly
            if (duration.TotalDays >= 1)
                return $"{(int)duration.TotalDays}d ago";
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours}h ago";
            if (duration.TotalMinutes >= 1)
                return $"{(int)duration.TotalMinutes}m ago";
            return "Just now";
        }
    }
}