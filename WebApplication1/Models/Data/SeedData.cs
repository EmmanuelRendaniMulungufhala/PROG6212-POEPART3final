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
            try
            {
                Console.WriteLine("Starting data seeding...");

                // Create roles
                string[] roleNames = { "HR", "ProgrammeCoordinator", "AcademicManager", "Lecturer" };

                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                        Console.WriteLine($"Created role: {roleName}");
                    }
                }

                // Create default users
                await CreateUser(userManager, "HR", "Admin", "hr@iet.com", "HR001", "Human Resources", "HR", "Password123!");
                await CreateUser(userManager, "John", "Coordinator", "coordinator@iet.com", "PC001", "Computer Science", "ProgrammeCoordinator", "Password123!");
                await CreateUser(userManager, "Sarah", "Manager", "manager@iet.com", "AM001", "Academic Affairs", "AcademicManager", "Password123!");
                await CreateUser(userManager, "David", "Lecturer", "lecturer@iet.com", "LEC001", "Computer Science", "Lecturer", "Password123!");

                Console.WriteLine("Data seeding completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during data seeding: {ex.Message}");
                // Don't throw - just log the error
            }
        }

        private static async Task CreateUser(UserManager<ApplicationUser> userManager,
            string firstName, string lastName, string email, string employeeId,
            string department, string role, string password)
        {
            try
            {
                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    Console.WriteLine($"User {email} already exists.");
                    return;
                }

                var user = new ApplicationUser
                {
                    FirstName = firstName,
                    LastName = lastName,
                    UserName = email,
                    Email = email,
                    EmployeeId = employeeId,
                    Department = department,
                    DateJoined = DateTime.Now,
                    Role = role switch
                    {
                        "HR" => UserRole.HR,
                        "ProgrammeCoordinator" => UserRole.ProgrammeCoordinator,
                        "AcademicManager" => UserRole.AcademicManager,
                        "Lecturer" => UserRole.Lecturer,
                        _ => UserRole.Lecturer
                    },
                    IsActive = true,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                    Console.WriteLine($"Created user: {email}");
                }
                else
                {
                    Console.WriteLine($"Failed to create user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating user {email}: {ex.Message}");
            }
        }
    }
}