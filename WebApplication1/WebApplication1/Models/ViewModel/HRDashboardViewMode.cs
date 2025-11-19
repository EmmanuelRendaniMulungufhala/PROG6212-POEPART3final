using WebApplication1.Models;

namespace WebApplication1.ViewModels
{
    public class HRDashboardViewModel
    {
        public int TotalClaims { get; set; }
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public int UnderReviewClaims { get; set; }
        public int ActiveLecturers { get; set; }
        public decimal TotalAmountThisMonth { get; set; }
        public int ApprovedClaimsThisMonth { get; set; }
        public double AverageProcessingDays { get; set; }
        public List<ClaimModel> RecentClaims { get; set; } = new List<ClaimModel>();
    }
}