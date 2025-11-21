using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WebApplication1.Models;
using WebApplication1.Data;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "HR")]
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // Single constructor with all dependencies
        public HRController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = new HRDashboardViewModel
            {
                TotalClaims = await _context.Claims.CountAsync(),
                PendingClaims = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Pending),
                ApprovedClaims = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Approved),
                RejectedClaims = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Rejected),
                UnderReviewClaims = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.UnderReview),
                ActiveLecturers = await _context.Users.CountAsync(u => u.Role == UserRole.Lecturer && u.IsActive),
                RecentClaims = await _context.Claims
                    .Include(c => c.Lecturer)
                    .Where(c => c.Status == ClaimStatus.Pending || c.Status == ClaimStatus.UnderReview)
                    .OrderByDescending(c => c.SubmissionDate)
                    .Take(10)
                    .ToListAsync()
            };

            // Calculate monthly statistics
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var approvedThisMonth = await _context.Claims
                .Where(c => c.Status == ClaimStatus.Approved && c.ApprovalDate >= startOfMonth)
                .ToListAsync();

            model.TotalAmountThisMonth = approvedThisMonth.Sum(c => c.TotalAmount);
            model.ApprovedClaimsThisMonth = approvedThisMonth.Count;

            // Calculate average processing time using computed properties
            var approvedClaims = await _context.Claims
                .Where(c => c.Status == ClaimStatus.Approved)
                .ToListAsync();

            // Calculate processing days on the client side
            var approvedClaimsWithProcessing = approvedClaims
                .Where(c => c.ApprovalDate.HasValue)
                .Select(c => new
                {
                    ProcessingDays = (c.ApprovalDate.Value - c.SubmissionDate).TotalDays
                })
                .ToList();

            model.AverageProcessingDays = approvedClaimsWithProcessing.Any()
                ? approvedClaimsWithProcessing.Average(c => c.ProcessingDays)
                : 0;

            return View(model);
        }

        public async Task<IActionResult> ProcessClaims(string? status, string? month, string? lecturer)
        {
            var query = _context.Claims
                .Include(c => c.Lecturer)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ClaimStatus>(status, out var statusFilter))
            {
                query = query.Where(c => c.Status == statusFilter);
            }

            if (!string.IsNullOrEmpty(month) && DateTime.TryParse(month + "-01", out var monthFilter))
            {
                query = query.Where(c => c.Month.Year == monthFilter.Year && c.Month.Month == monthFilter.Month);
            }

            if (!string.IsNullOrEmpty(lecturer))
            {
                query = query.Where(c => c.Lecturer!.FirstName.Contains(lecturer) ||
                                        c.Lecturer!.LastName.Contains(lecturer) ||
                                        c.Lecturer!.Email.Contains(lecturer));
            }

            var claims = await query
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();

            return View(claims);
        }

        // Status-specific claim views
        public async Task<IActionResult> ApprovedClaims(string? month, string? lecturer, string? department)
        {
            var query = _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Approved)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(month) && DateTime.TryParse(month + "-01", out var monthFilter))
            {
                query = query.Where(c => c.Month.Year == monthFilter.Year && c.Month.Month == monthFilter.Month);
            }

            if (!string.IsNullOrEmpty(lecturer))
            {
                query = query.Where(c => c.Lecturer!.FirstName.Contains(lecturer) ||
                                        c.Lecturer!.LastName.Contains(lecturer) ||
                                        c.Lecturer!.Email.Contains(lecturer));
            }

            if (!string.IsNullOrEmpty(department))
            {
                query = query.Where(c => c.Lecturer!.Department == department);
            }

            var claims = await query
                .OrderByDescending(c => c.ApprovalDate)
                .ThenByDescending(c => c.SubmissionDate)
                .ToListAsync();

            ViewBag.Departments = await _context.Users
                .Where(u => u.Role == UserRole.Lecturer)
                .Select(u => u.Department)
                .Distinct()
                .ToListAsync();

            return View(claims);
        }

        public async Task<IActionResult> PendingClaims(string? month, string? lecturer, string? department)
        {
            var query = _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Pending)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(month) && DateTime.TryParse(month + "-01", out var monthFilter))
            {
                query = query.Where(c => c.Month.Year == monthFilter.Year && c.Month.Month == monthFilter.Month);
            }

            if (!string.IsNullOrEmpty(lecturer))
            {
                query = query.Where(c => c.Lecturer!.FirstName.Contains(lecturer) ||
                                        c.Lecturer!.LastName.Contains(lecturer) ||
                                        c.Lecturer!.Email.Contains(lecturer));
            }

            if (!string.IsNullOrEmpty(department))
            {
                query = query.Where(c => c.Lecturer!.Department == department);
            }

            var claims = await query
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();

            ViewBag.Departments = await _context.Users
                .Where(u => u.Role == UserRole.Lecturer)
                .Select(u => u.Department)
                .Distinct()
                .ToListAsync();

            return View(claims);
        }

        public async Task<IActionResult> RejectedClaims(string? month, string? lecturer, string? department)
        {
            var query = _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Rejected)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(month) && DateTime.TryParse(month + "-01", out var monthFilter))
            {
                query = query.Where(c => c.Month.Year == monthFilter.Year && c.Month.Month == monthFilter.Month);
            }

            if (!string.IsNullOrEmpty(lecturer))
            {
                query = query.Where(c => c.Lecturer!.FirstName.Contains(lecturer) ||
                                        c.Lecturer!.LastName.Contains(lecturer) ||
                                        c.Lecturer!.Email.Contains(lecturer));
            }

            if (!string.IsNullOrEmpty(department))
            {
                query = query.Where(c => c.Lecturer!.Department == department);
            }

            var claims = await query
                .OrderByDescending(c => c.LastStatusUpdateDate)
                .ThenByDescending(c => c.SubmissionDate)
                .ToListAsync();

            ViewBag.Departments = await _context.Users
                .Where(u => u.Role == UserRole.Lecturer)
                .Select(u => u.Department)
                .Distinct()
                .ToListAsync();

            return View(claims);
        }

        public async Task<IActionResult> UnderReviewClaims(string? month, string? lecturer, string? department)
        {
            var query = _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.UnderReview)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(month) && DateTime.TryParse(month + "-01", out var monthFilter))
            {
                query = query.Where(c => c.Month.Year == monthFilter.Year && c.Month.Month == monthFilter.Month);
            }

            if (!string.IsNullOrEmpty(lecturer))
            {
                query = query.Where(c => c.Lecturer!.FirstName.Contains(lecturer) ||
                                        c.Lecturer!.LastName.Contains(lecturer) ||
                                        c.Lecturer!.Email.Contains(lecturer));
            }

            if (!string.IsNullOrEmpty(department))
            {
                query = query.Where(c => c.Lecturer!.Department == department);
            }

            var claims = await query
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();

            ViewBag.Departments = await _context.Users
                .Where(u => u.Role == UserRole.Lecturer)
                .Select(u => u.Department)
                .Distinct()
                .ToListAsync();

            return View(claims);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveClaim(Guid claimId, string? approvalNotes)
        {
            try
            {
                var claim = await _context.Claims
                    .Include(c => c.Lecturer)
                    .FirstOrDefaultAsync(c => c.Id == claimId);

                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction(nameof(ProcessClaims));
                }

                if (claim.Status == ClaimStatus.Approved)
                {
                    TempData["Warning"] = "Claim is already approved.";
                    return RedirectToAction(nameof(ProcessClaims));
                }

                claim.UpdateStatus(ClaimStatus.Approved, approvalNotes, User.Identity?.Name);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Claim for {claim.Lecturer?.FullName} has been approved successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error approving claim: {ex.Message}";
            }

            return RedirectToAction(nameof(ProcessClaims));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectClaim(Guid claimId, string rejectionNotes)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rejectionNotes))
                {
                    TempData["Error"] = "Rejection notes are required.";
                    return RedirectToAction(nameof(ProcessClaims));
                }

                var claim = await _context.Claims
                    .Include(c => c.Lecturer)
                    .FirstOrDefaultAsync(c => c.Id == claimId);

                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction(nameof(ProcessClaims));
                }

                if (claim.Status == ClaimStatus.Rejected)
                {
                    TempData["Warning"] = "Claim is already rejected.";
                    return RedirectToAction(nameof(ProcessClaims));
                }

                claim.UpdateStatus(ClaimStatus.Rejected, rejectionNotes, User.Identity?.Name);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Claim for {claim.Lecturer?.FullName} has been rejected.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error rejecting claim: {ex.Message}";
            }

            return RedirectToAction(nameof(ProcessClaims));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLecturerDetails(
            string id,
            string firstName,
            string lastName,
            string email,
            string? phoneNumber,
            string? employeeId,
            string? department,
            DateTime dateJoined,
            bool isActive)
        {
            try
            {
                var lecturer = await _context.Users.FindAsync(id);
                if (lecturer == null)
                {
                    return NotFound();
                }

                lecturer.FirstName = firstName;
                lecturer.LastName = lastName;
                lecturer.Email = email;
                lecturer.PhoneNumber = phoneNumber;
                lecturer.EmployeeId = employeeId;
                lecturer.Department = department;
                lecturer.DateJoined = dateJoined;
                lecturer.IsActive = isActive;

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Lecturer {lecturer.FullName} details updated successfully!";
                return RedirectToAction(nameof(LecturerDetails), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating lecturer details: {ex.Message}";
                return RedirectToAction(nameof(LecturerDetails), new { id });
            }
        }

        // GET: HR/CreateUser
        public IActionResult CreateUser()
        {
            return View();
        }

        // POST: HR/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(
            string firstName,
            string lastName,
            string email,
            string? phoneNumber,
            string? employeeId,
            string? department,
            DateTime dateJoined,
            UserRole role,
            string password,
            bool isActive = true,
            bool sendWelcomeEmail = false,
            bool requirePasswordChange = false)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "A user with this email already exists.");
                    return View();
                }

                // Create new user
                var user = new ApplicationUser
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    UserName = email,
                    PhoneNumber = phoneNumber,
                    EmployeeId = employeeId,
                    Department = department,
                    DateJoined = dateJoined,
                    Role = role,
                    IsActive = isActive
                };

                // Create user with password
                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // Add to role based on selected role
                    var roleName = role.ToString();
                    await _userManager.AddToRoleAsync(user, roleName);

                    // Set password change requirement if needed
                    if (requirePasswordChange)
                    {
                        // You might want to set a flag or use Identity's built-in functionality
                        // For now, we'll just log it
                        Console.WriteLine($"Password change required for user: {email}");
                    }

                    // Send welcome email if requested
                    if (sendWelcomeEmail)
                    {
                        // Implement email sending logic here
                        // await _emailService.SendWelcomeEmail(user.Email, user.FullName);
                    }

                    TempData["Success"] = $"User {user.FullName} created successfully with {roleName} role! " +
                                         $"{(sendWelcomeEmail ? "Welcome email sent." : "Please provide login details to the user.")}";

                    return RedirectToAction(nameof(LecturerManagement));
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View();
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating user: {ex.Message}";
                return View();
            }
        }

        public async Task<IActionResult> LecturerManagement()
        {
            var lecturers = await _context.Users
                .Where(u => u.Role == UserRole.Lecturer)
                .OrderBy(u => u.LastName)
                .ToListAsync();

            return View(lecturers);
        }

        public async Task<IActionResult> LecturerDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var lecturer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRole.Lecturer);

            if (lecturer == null)
            {
                return NotFound();
            }

            var claims = await _context.Claims
                .Where(c => c.LecturerId == id)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();

            var model = new LecturerDetailsViewModel
            {
                Lecturer = lecturer,
                Claims = claims
            };

            return View(model);
        }

        public async Task<IActionResult> Reports()
        {
            var startDate = DateTime.Now.AddMonths(-1);
            var endDate = DateTime.Now;

            var model = new HRReportsViewModel
            {
                StartDate = startDate,
                EndDate = endDate
            };

            // Get approved claims for the period
            var approvedClaims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Approved &&
                           c.SubmissionDate >= startDate &&
                           c.SubmissionDate <= endDate)
                .ToListAsync();

            model.ApprovedClaims = approvedClaims;
            model.TotalAmount = approvedClaims.Sum(c => c.TotalAmount);
            model.TotalClaims = approvedClaims.Count;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateReport(HRReportsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Reports", model);
            }

            var approvedClaims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Approved &&
                           c.SubmissionDate >= model.StartDate &&
                           c.SubmissionDate <= model.EndDate)
                .ToListAsync();

            model.ApprovedClaims = approvedClaims;
            model.TotalAmount = approvedClaims.Sum(c => c.TotalAmount);
            model.TotalClaims = approvedClaims.Count;

            return View("Reports", model);
        }

        public async Task<IActionResult> GeneratePaymentReport(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                TempData["Error"] = "Start date cannot be after end date.";
                return RedirectToAction(nameof(Reports));
            }

            var claims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Approved &&
                           c.ApprovalDate >= startDate &&
                           c.ApprovalDate <= endDate)
                .OrderBy(c => c.Lecturer!.LastName)
                .ToListAsync();

            var model = new PaymentReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                Claims = claims,
                GeneratedDate = DateTime.Now,
                GeneratedBy = User.Identity?.Name ?? "Unknown"
            };

            return View("PaymentReport", model);
        }

        public async Task<IActionResult> UpdateLecturerStatus(string id, bool isActive)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var lecturer = await _context.Users.FindAsync(id);
            if (lecturer == null)
            {
                return NotFound();
            }

            lecturer.IsActive = isActive;
            await _context.SaveChangesAsync();

            var status = isActive ? "activated" : "deactivated";
            TempData["Success"] = $"Lecturer {lecturer.FullName} has been {status} successfully!";
            return RedirectToAction(nameof(LecturerManagement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateClaimStatus(Guid claimId, ClaimStatus status, string? notes)
        {
            try
            {
                var claim = await _context.Claims
                    .Include(c => c.Lecturer)
                    .FirstOrDefaultAsync(c => c.Id == claimId);

                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction(nameof(ProcessClaims));
                }

                claim.UpdateStatus(status, notes, User.Identity?.Name);
                await _context.SaveChangesAsync();

                var statusText = status.ToString().ToLower();
                TempData["Success"] = $"Claim for {claim.Lecturer?.FullName} has been {statusText} successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating claim status: {ex.Message}";
            }

            return RedirectToAction(nameof(ProcessClaims));
        }

        public async Task<IActionResult> ClaimDetails(Guid id)
        {
            var claim = await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .Include(c => c.StatusHistory)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null)
            {
                return NotFound();
            }

            return View(claim);
        }

        public async Task<IActionResult> GetClaimsStatistics()
        {
            try
            {
                var today = DateTime.Today;
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var startOfYear = new DateTime(today.Year, 1, 1);

                var monthlyClaims = await _context.Claims
                    .Where(c => c.SubmissionDate >= startOfMonth)
                    .GroupBy(c => c.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                var yearlyStats = await _context.Claims
                    .Where(c => c.SubmissionDate >= startOfYear && c.Status == ClaimStatus.Approved)
                    .GroupBy(c => c.Month.Month)
                    .Select(g => new { Month = g.Key, TotalAmount = g.Sum(c => c.TotalAmount) })
                    .ToListAsync();

                return Json(new
                {
                    monthlyStats = monthlyClaims,
                    yearlyTrends = yearlyStats
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Helper methods for navigation counts
        private async Task<int> GetPendingClaimsCount()
        {
            return await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Pending);
        }

        private async Task<int> GetApprovedClaimsCount()
        {
            return await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Approved);
        }

        private async Task<int> GetRejectedClaimsCount()
        {
            return await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Rejected);
        }

        private async Task<int> GetUnderReviewClaimsCount()
        {
            return await _context.Claims.CountAsync(c => c.Status == ClaimStatus.UnderReview);
        }

        // Additional HR actions
        public async Task<IActionResult> UserRoles()
        {
            // Implementation for user roles management
            return View();
        }

        public async Task<IActionResult> FinancialAnalytics()
        {
            // Implementation for financial analytics
            return View();
        }

        public async Task<IActionResult> SystemAudit()
        {
            // Implementation for system audit
            return View();
        }

        public async Task<IActionResult> DownloadClaimReport(Guid id)
        {
            var claim = await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null)
            {
                return NotFound();
            }

            // For now, return the details view as PDF
            // In a real application, you would generate a proper PDF report
            return View("ClaimDetails", claim);
        }
    }
}