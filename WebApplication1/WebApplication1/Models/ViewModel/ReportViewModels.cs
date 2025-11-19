using System.ComponentModel.DataAnnotations;
using WebApplication1.Models;

namespace WebApplication1.ViewModels
{
    public class ReportsIndexViewModel
    {
        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-1);

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Now;

        [Display(Name = "Report Type")]
        public ClaimReportType ReportType { get; set; } = ClaimReportType.Claims;
    }

    public enum ClaimReportType
    {
        Claims,
        Approvals,
        Payments,
        Performance
    }

    public class ClaimReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<ClaimModel> Claims { get; set; } = new List<ClaimModel>();
        public DateTime GeneratedDate { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        public ClaimReportType ReportType { get; set; }

        // Statistics
        public int TotalClaims => Claims.Count;
        public int ApprovedClaims => Claims.Count(c => c.Status == ClaimStatus.Approved);
        public int PendingClaims => Claims.Count(c => c.Status == ClaimStatus.Pending);
        public int RejectedClaims => Claims.Count(c => c.Status == ClaimStatus.Rejected);
        public decimal TotalAmount => Claims.Where(c => c.Status == ClaimStatus.Approved).Sum(c => c.TotalAmount);
    }

    public class ApprovalStatisticsViewModel
    {
        public List<ApprovalStatistic> Statistics { get; set; } = new List<ApprovalStatistic>();
        public string Period { get; set; } = string.Empty;
        public int TotalClaims => Statistics.Sum(s => s.Count);
        public decimal TotalAmount => Statistics.Sum(s => s.TotalAmount);
    }

    public class ApprovalStatistic
    {
        public ClaimStatus Status { get; set; }
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public double Percentage { get; set; }
    }
}
