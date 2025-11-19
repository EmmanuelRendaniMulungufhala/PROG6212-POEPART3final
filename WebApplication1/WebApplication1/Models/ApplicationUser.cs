using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [Display(Name = "First Name")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Employee ID")]
        [StringLength(20)]
        public string? EmployeeId { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        [Display(Name = "Date Joined")]
        [DataType(DataType.Date)]
        public DateTime DateJoined { get; set; } = DateTime.Now;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public UserRole Role { get; set; } = UserRole.Lecturer;

        // Navigation properties
        public virtual ICollection<ClaimModel> Claims { get; set; } = new List<ClaimModel>();

        // Computed properties
        [Display(Name = "Full Name")]
        public string FullName => $"{FirstName} {LastName}";
    }

    public enum UserRole
    {
        Lecturer,
        ProgrammeCoordinator,
        AcademicManager,
        HR
    }
}