using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
    public class PerformanceMetricsViewModel
    {
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-3);

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Now;

        // Department Performance
        public List<DepartmentPerformanceViewModel>? DepartmentPerformance { get; set; }

        // Coordinator Performance
        public List<CoordinatorPerformanceViewModel>? CoordinatorPerformance { get; set; }

        // Claim Processing Metrics
        public ClaimProcessingMetricsViewModel? ProcessingMetrics { get; set; }

        // Financial Metrics
        public FinancialMetricsViewModel? FinancialMetrics { get; set; }
    }

    public class DepartmentPerformanceViewModel
    {
        public string? Department { get; set; }
        public int TotalClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public int PendingClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ApprovalRate { get; set; }
        public double AverageProcessingDays { get; set; }
    }

    public class CoordinatorPerformanceViewModel
    {
        public string? CoordinatorName { get; set; }
        public string? Department { get; set; }
        public int ClaimsReviewed { get; set; }
        public int ClaimsApproved { get; set; }
        public int ClaimsRejected { get; set; }
        public decimal ApprovalRate { get; set; }
        public double AverageReviewTimeHours { get; set; }
    }

    public class ClaimProcessingMetricsViewModel
    {
        public int TotalClaimsProcessed { get; set; }
        public double AverageProcessingTimeDays { get; set; }
        public int ClaimsWithinSLA { get; set; }
        public double SLAAchievementRate { get; set; }
        public int BacklogCount { get; set; }
        public double BacklogReductionRate { get; set; }
    }

    public class FinancialMetricsViewModel
    {
        public decimal TotalAmountProcessed { get; set; }
        public decimal AverageClaimAmount { get; set; }
        public decimal HighestClaimAmount { get; set; }
        public decimal LowestClaimAmount { get; set; }
        public decimal MonthlyTrend { get; set; }
        public Dictionary<string, decimal>? DepartmentBreakdown { get; set; }
    }
}