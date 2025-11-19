using WebApplication1.Models;

namespace WebApplication1.ViewModels
{
    public class CoordinatorDashboardViewModel
    {
        public int TotalClaims { get; set; }
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public int UnderReviewClaims { get; set; }
        public decimal TotalAmountThisMonth { get; set; }
        public int ApprovedClaimsThisMonth { get; set; }
        public string Department { get; set; } = string.Empty;
        public List<ClaimModel> RecentClaims { get; set; } = new List<ClaimModel>();
    }
}