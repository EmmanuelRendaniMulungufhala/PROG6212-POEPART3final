using System.ComponentModel.DataAnnotations;
using WebApplication1.Models;

namespace WebApplication1.ViewModels
{
    public class FinancialReportsViewModel
    {
        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public List<ClaimModel> ApprovedClaims { get; set; } = new List<ClaimModel>();
        public List<MonthlyTrendViewModel> MonthlyTrends { get; set; } = new List<MonthlyTrendViewModel>();
        public decimal TotalAmount { get; set; }
        public int TotalClaims { get; set; }
    }

    public class MonthlyTrendViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalAmount { get; set; }
        public int ClaimCount { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMM yyyy");
    }
}