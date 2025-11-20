using WebApplication1.Models;

namespace WebApplication1.ViewModels
{
    public class LecturerClaimsViewModel
    {
        public ApplicationUser Lecturer { get; set; } = null!;
        public List<ClaimModel> Claims { get; set; } = new List<ClaimModel>();
    }
}