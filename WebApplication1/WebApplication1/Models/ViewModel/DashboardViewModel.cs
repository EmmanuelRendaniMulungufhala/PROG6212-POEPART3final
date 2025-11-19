namespace WebApplication1.Models
{
    public class DashboardViewModel
    {
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int PendingApprovals { get; set; }
        public int TotalClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public List<ActivityItem> RecentActivities { get; set; } = new List<ActivityItem>();
        public List<ClaimStatistic> MonthlyStatistics { get; set; } = new List<ClaimStatistic>();
    }

    public class ActivityItem
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string ClaimId { get; set; } = string.Empty;

        public string StatusBadgeClass
        {
            get
            {
                return Status?.ToLower() switch
                {
                    "approved" => "bg-success",
                    "rejected" => "bg-danger",
                    "underreview" => "bg-info",
                    "paid" => "bg-primary",
                    _ => "bg-warning"
                };
            }
        }

        public string FormattedDate => Date.ToString("MMM dd, yyyy HH:mm");
        public string FormattedAmount => Amount.ToString("C");
    }

    public class ClaimStatistic
    {
        public string Month { get; set; } = string.Empty;
        public int ClaimCount { get; set; }
        public decimal TotalAmount { get; set; }
        public int ApprovedCount { get; set; }
        public int PendingCount { get; set; }
    }
}