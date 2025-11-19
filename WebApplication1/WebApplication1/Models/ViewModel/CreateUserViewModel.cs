using System.ComponentModel.DataAnnotations;
using WebApplication1.Models;

namespace WebApplication1.ViewModels
{
    public class CreateUserViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Employee ID")]
        [StringLength(20)]
        public string? EmployeeId { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        [Required]
        [Display(Name = "Date Joined")]
        [DataType(DataType.Date)]
        public DateTime DateJoined { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "User Role")]
        public UserRole Role { get; set; } = UserRole.Lecturer;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number.")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Send Welcome Email")]
        public bool SendWelcomeEmail { get; set; } = true;

        [Display(Name = "Require Password Change")]
        public bool RequirePasswordChange { get; set; } = true;

        [Display(Name = "Active Account")]
        public bool IsActive { get; set; } = true;
    }
}