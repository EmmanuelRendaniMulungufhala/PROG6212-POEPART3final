using System.ComponentModel.DataAnnotations;
using WebApplication1.Models;

namespace WebApplication1.ViewModels
{
    public class HRReportsViewModel
    {
        [Required(ErrorMessage = "Start date is required")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-1);

        [Required(ErrorMessage = "End date is required")]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Now;

        public List<ClaimModel> ApprovedClaims { get; set; } = new List<ClaimModel>();
        public decimal TotalAmount { get; set; }
        public int TotalClaims { get; set; }
        public int TotalLecturers { get; set; }

        [Display(Name = "Report Type")]
        public ReportType ReportType { get; set; } = ReportType.Payment;
    }

    public enum ReportType
    {
        Payment,
        Claims,
        Lecturer,
        Performance
    }

    public class PaymentReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<ClaimModel> Claims { get; set; } = new List<ClaimModel>();
        public DateTime GeneratedDate { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal TotalAmount => Claims.Sum(c => c.TotalAmount);
        public int TotalClaims => Claims.Count;
    }

    public class LecturerDetailsViewModel
    {
        public ApplicationUser Lecturer { get; set; } = new ApplicationUser();
        public List<ClaimModel> Claims { get; set; } = new List<ClaimModel>();
        public decimal TotalEarnings => Claims.Where(c => c.Status == ClaimStatus.Approved).Sum(c => c.TotalAmount);
        public int TotalApprovedClaims => Claims.Count(c => c.Status == ClaimStatus.Approved);
        public int TotalPendingClaims => Claims.Count(c => c.Status == ClaimStatus.Pending);
    }

    public class LecturerPerformanceViewModel
    {
        public string LecturerId { get; set; } = string.Empty;
        public string LecturerName { get; set; } = string.Empty;
        public int TotalClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int PendingClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public double AverageProcessingTime { get; set; }
        public double ApprovalRate => TotalClaims > 0 ? (double)ApprovedClaims / TotalClaims * 100 : 0;
    }
}