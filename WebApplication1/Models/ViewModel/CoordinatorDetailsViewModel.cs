using WebApplication1.Models;

namespace WebApplication1.ViewModels
{
    public class CoordinatorDetailsViewModel
    {
        public ApplicationUser Coordinator { get; set; } = null!;
        public Dictionary<string, DepartmentStat> DepartmentStats { get; set; } = new Dictionary<string, DepartmentStat>();
    }

    public class DepartmentStat
    {
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }
}