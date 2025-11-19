using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1
{
    public static class SeedData
    {
        public static async Task InitializeAsync(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Create roles if they don't exist
            string[] roleNames = { "HR", "ProgrammeCoordinator", "AcademicManager", "Lecturer" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create default HR user if no users exist
            if (!userManager.Users.Any())
            {
                var hrUser = new ApplicationUser
                {
                    FirstName = "HR",
                    LastName = "Admin",
                    UserName = "hr@iet.com",
                    Email = "hr@iet.com",
                    EmployeeId = "HR001",
                    Department = "Human Resources",
                    DateJoined = DateTime.Now,
                    Role = UserRole.HR,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(hrUser, "Password123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(hrUser, "HR");
                }

                // Create a sample Programme Coordinator
                var coordinatorUser = new ApplicationUser
                {
                    FirstName = "John",
                    LastName = "Coordinator",
                    UserName = "coordinator@iet.com",
                    Email = "coordinator@iet.com",
                    EmployeeId = "PC001",
                    Department = "Computer Science",
                    DateJoined = DateTime.Now,
                    Role = UserRole.ProgrammeCoordinator,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var coordResult = await userManager.CreateAsync(coordinatorUser, "Password123!");
                if (coordResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(coordinatorUser, "ProgrammeCoordinator");
                }

                // Create a sample Academic Manager
                var managerUser = new ApplicationUser
                {
                    FirstName = "Sarah",
                    LastName = "Manager",
                    UserName = "manager@iet.com",
                    Email = "manager@iet.com",
                    EmployeeId = "AM001",
                    Department = "Academic Affairs",
                    DateJoined = DateTime.Now,
                    Role = UserRole.AcademicManager,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var managerResult = await userManager.CreateAsync(managerUser, "Password123!");
                if (managerResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(managerUser, "AcademicManager");
                }

                // Create a sample Lecturer
                var lecturerUser = new ApplicationUser
                {
                    FirstName = "David",
                    LastName = "Lecturer",
                    UserName = "lecturer@iet.com",
                    Email = "lecturer@iet.com",
                    EmployeeId = "LEC001",
                    Department = "Computer Science",
                    DateJoined = DateTime.Now,
                    Role = UserRole.Lecturer,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var lecturerResult = await userManager.CreateAsync(lecturerUser, "Password123!");
                if (lecturerResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(lecturerUser, "Lecturer");
                }
            }

            // Add some sample claims if none exist
            if (!context.Claims.Any())
            {
                var lecturer = await userManager.FindByEmailAsync("lecturer@iet.com");
                if (lecturer != null)
                {
                    var claims = new[]
                    {
                        new ClaimModel
                        {
                            Id = Guid.NewGuid(),
                            LecturerId = lecturer.Id,
                            Month = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                            HoursWorked = 40,
                            HourlyRate = 150,
                            TotalAmount = 6000,
                            AdditionalNotes = "Regular teaching hours",
                            Status = ClaimStatus.Pending,
                            SubmissionDate = DateTime.Now.AddDays(-5)
                        },
                        new ClaimModel
                        {
                            Id = Guid.NewGuid(),
                            LecturerId = lecturer.Id,
                            Month = new DateTime(DateTime.Now.Year, DateTime.Now.Month - 1, 1),
                            HoursWorked = 35,
                            HourlyRate = 150,
                            TotalAmount = 5250,
                            AdditionalNotes = "Additional marking hours",
                            Status = ClaimStatus.Approved,
                            SubmissionDate = DateTime.Now.AddDays(-35),
                            ApprovalDate = DateTime.Now.AddDays(-30),
                            ApprovedBy = "HR Admin"
                        }
                    };

                    context.Claims.AddRange(claims);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}