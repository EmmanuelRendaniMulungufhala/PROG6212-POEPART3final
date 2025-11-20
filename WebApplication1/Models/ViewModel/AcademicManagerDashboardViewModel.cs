using WebApplication1.Models;

namespace WebApplication1.ViewModels
{
    public class AcademicManagerDashboardViewModel
    {
        public int TotalClaims { get; set; }
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public int UnderReviewClaims { get; set; }
        public int TotalLecturers { get; set; }
        public int TotalCoordinators { get; set; }
        public int TotalDepartments { get; set; }
        public decimal TotalAmountThisMonth { get; set; }
        public int ApprovedClaimsThisMonth { get; set; }
        public List<ClaimModel> RecentClaims { get; set; } = new List<ClaimModel>();
        public List<DepartmentStatViewModel> TopDepartments { get; set; } = new List<DepartmentStatViewModel>();
    }

    public class DepartmentStatViewModel
    {
        public string Department { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int ClaimCount { get; set; }
    }
}