using WebApplication1.Models;

namespace WebApplication1.ViewModels
{
    public class ComprehensiveReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<ClaimModel> Claims { get; set; } = new List<ClaimModel>();
        public List<DepartmentSummaryViewModel> DepartmentSummary { get; set; } = new List<DepartmentSummaryViewModel>();
        public DateTime GeneratedDate { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        public decimal TotalAmount => Claims.Sum(c => c.TotalAmount);
        public int TotalClaims => Claims.Count;
    }

    public class DepartmentSummaryViewModel
    {
        public string Department { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int ClaimCount { get; set; }
        public decimal AverageAmount { get; set; }
    }
}