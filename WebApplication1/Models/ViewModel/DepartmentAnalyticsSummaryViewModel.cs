using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
    public class DepartmentAnalyticsSummaryViewModel
    {
        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public List<DepartmentAnalyticsViewModel> DepartmentStats { get; set; } = new List<DepartmentAnalyticsViewModel>();
    }

    public class DepartmentAnalyticsViewModel
    {
        public string Department { get; set; } = string.Empty;
        public int TotalClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int PendingClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public double AverageProcessingDays { get; set; }
        public decimal ApprovalRate => TotalClaims > 0 ? (decimal)ApprovedClaims / TotalClaims * 100 : 0;
    }
}