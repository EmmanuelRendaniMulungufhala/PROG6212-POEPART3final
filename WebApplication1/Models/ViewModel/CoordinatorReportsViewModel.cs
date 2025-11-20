using WebApplication1.Models;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
    public class CoordinatorReportsViewModel
    {
        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public string Department { get; set; } = string.Empty;
        public List<ClaimModel> ApprovedClaims { get; set; } = new List<ClaimModel>();
        public decimal TotalAmount { get; set; }
        public int TotalClaims { get; set; }
    }
}