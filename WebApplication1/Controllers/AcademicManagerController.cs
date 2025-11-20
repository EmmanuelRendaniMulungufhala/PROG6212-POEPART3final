using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WebApplication1.Models;
using WebApplication1.Data;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "AcademicManager")]
    public class AcademicManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AcademicManagerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return NotFound();
            }

            var model = new AcademicManagerDashboardViewModel
            {
                TotalClaims = await _context.Claims.CountAsync(),
                PendingClaims = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Pending),
                ApprovedClaims = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Approved),
                RejectedClaims = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Rejected),
                UnderReviewClaims = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.UnderReview),
                TotalLecturers = await _context.Users.CountAsync(u => u.Role == UserRole.Lecturer && u.IsActive),
                TotalCoordinators = await _context.Users.CountAsync(u => u.Role == UserRole.ProgrammeCoordinator && u.IsActive),
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

            // Calculate department statistics
            var departmentStats = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Approved && c.ApprovalDate >= startOfMonth)
                .GroupBy(c => c.Lecturer!.Department)
                .Select(g => new DepartmentStatViewModel
                {
                    Department = g.Key ?? "Unknown",
                    TotalAmount = g.Sum(c => c.TotalAmount),
                    ClaimCount = g.Count()
                })
                .OrderByDescending(d => d.TotalAmount)
                .Take(5)
                .ToListAsync();

            model.TopDepartments = departmentStats;

            // Get total departments count
            model.TotalDepartments = await _context.Users
                .Where(u => u.Department != null)
                .Select(u => u.Department!)
                .Distinct()
                .CountAsync();

            return View(model);
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
                    return RedirectToAction(nameof(AllClaims));
                }

                if (claim.Status == ClaimStatus.Approved)
                {
                    TempData["Warning"] = "Claim is already approved.";
                    return RedirectToAction(nameof(AllClaims));
                }

                claim.UpdateStatus(ClaimStatus.Approved, approvalNotes, User.Identity?.Name);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Claim for {claim.Lecturer?.FullName} has been approved successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error approving claim: {ex.Message}";
            }

            return RedirectToAction(nameof(AllClaims));
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
                    return RedirectToAction(nameof(AllClaims));
                }

                var claim = await _context.Claims
                    .Include(c => c.Lecturer)
                    .FirstOrDefaultAsync(c => c.Id == claimId);

                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction(nameof(AllClaims));
                }

                if (claim.Status == ClaimStatus.Rejected)
                {
                    TempData["Warning"] = "Claim is already rejected.";
                    return RedirectToAction(nameof(AllClaims));
                }

                claim.UpdateStatus(ClaimStatus.Rejected, rejectionNotes, User.Identity?.Name);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Claim for {claim.Lecturer?.FullName} has been rejected.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error rejecting claim: {ex.Message}";
            }

            return RedirectToAction(nameof(AllClaims));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendForReview(Guid claimId, string? reviewNotes)
        {
            try
            {
                var claim = await _context.Claims
                    .Include(c => c.Lecturer)
                    .FirstOrDefaultAsync(c => c.Id == claimId);

                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction(nameof(AllClaims));
                }

                claim.UpdateStatus(ClaimStatus.UnderReview, reviewNotes, User.Identity?.Name);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Claim for {claim.Lecturer?.FullName} has been marked for review.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating claim status: {ex.Message}";
            }

            return RedirectToAction(nameof(AllClaims));
        }

        public async Task<IActionResult> DepartmentClaims(string department)
        {
            if (string.IsNullOrEmpty(department))
            {
                TempData["Error"] = "Department is required.";
                return RedirectToAction(nameof(AllClaims));
            }

            var claims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Lecturer!.Department == department)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();

            ViewBag.Department = department;

            // Get department statistics
            var departmentStats = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Lecturer!.Department == department)
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count(), Amount = g.Sum(c => c.TotalAmount) })
                .ToListAsync();

            ViewBag.DepartmentStats = departmentStats;

            return View(claims);
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
                    return RedirectToAction(nameof(AllClaims));
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

            return RedirectToAction(nameof(AllClaims));
        }

        // ... rest of your existing methods remain the same ...

        public async Task<IActionResult> AllClaims(string? status, string? department, string? month, string? lecturer)
        {
            var query = _context.Claims
                .Include(c => c.Lecturer)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ClaimStatus>(status, out var statusFilter))
            {
                query = query.Where(c => c.Status == statusFilter);
            }

            if (!string.IsNullOrEmpty(department))
            {
                query = query.Where(c => c.Lecturer!.Department == department);
            }

            if (!string.IsNullOrEmpty(month) && DateTime.TryParse(month + "-01", out var monthFilter))
            {
                query = query.Where(c => c.Month.Year == monthFilter.Year && c.Month.Month == monthFilter.Month);
            }

            if (!string.IsNullOrEmpty(lecturer))
            {
                query = query.Where(c => c.Lecturer!.FirstName.Contains(lecturer) ||
                                        c.Lecturer!.LastName.Contains(lecturer));
            }

            var claims = await query
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();

            // Get unique departments for filter dropdown
            var departments = await _context.Users
                .Where(u => u.Department != null)
                .Select(u => u.Department!)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            ViewBag.Departments = departments;
            return View(claims);
        }

        public async Task<IActionResult> PendingClaims(string? department, string? month)
        {
            var query = _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Pending)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(department))
            {
                query = query.Where(c => c.Lecturer!.Department == department);
            }

            if (!string.IsNullOrEmpty(month) && DateTime.TryParse(month + "-01", out var monthFilter))
            {
                query = query.Where(c => c.Month.Year == monthFilter.Year && c.Month.Month == monthFilter.Month);
            }

            var claims = await query
                .OrderBy(c => c.SubmissionDate)
                .ToListAsync();

            // Get unique departments for filter dropdown
            var departments = await _context.Users
                .Where(u => u.Department != null)
                .Select(u => u.Department!)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            ViewBag.Departments = departments;
            return View(claims);
        }

        public async Task<IActionResult> ApprovedClaims(string? department, string? month)
        {
            var query = _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Approved)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(department))
            {
                query = query.Where(c => c.Lecturer!.Department == department);
            }

            if (!string.IsNullOrEmpty(month) && DateTime.TryParse(month + "-01", out var monthFilter))
            {
                query = query.Where(c => c.Month.Year == monthFilter.Year && c.Month.Month == monthFilter.Month);
            }

            var claims = await query
                .OrderByDescending(c => c.ApprovalDate)
                .ToListAsync();

            // Get unique departments for filter dropdown
            var departments = await _context.Users
                .Where(u => u.Department != null)
                .Select(u => u.Department!)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            ViewBag.Departments = departments;
            return View(claims);
        }

        public async Task<IActionResult> ReviewClaim(Guid id)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessClaimReview(Guid claimId, string action, string reviewNotes, string priority)
        {
            try
            {
                var claim = await _context.Claims
                    .Include(c => c.Lecturer)
                    .FirstOrDefaultAsync(c => c.Id == claimId);

                if (claim == null)
                {
                    return NotFound();
                }

                switch (action)
                {
                    case "Approve":
                        claim.UpdateStatus(ClaimStatus.Approved, reviewNotes, User.Identity?.Name);
                        TempData["Success"] = $"Claim for {claim.Lecturer?.FullName} has been approved!";
                        break;

                    case "Reject":
                        claim.UpdateStatus(ClaimStatus.Rejected, reviewNotes, User.Identity?.Name);
                        TempData["Warning"] = $"Claim for {claim.Lecturer?.FullName} has been rejected.";
                        break;

                    case "RequestInfo":
                        claim.UpdateStatus(ClaimStatus.UnderReview, $"Additional information requested: {reviewNotes}", User.Identity?.Name);
                        TempData["Info"] = $"Information request sent to {claim.Lecturer?.FullName}.";
                        break;
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(PendingClaims));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error processing claim: {ex.Message}";
                return RedirectToAction(nameof(ReviewClaim), new { id = claimId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestInformation(Guid id, string message)
        {
            try
            {
                var claim = await _context.Claims
                    .Include(c => c.Lecturer)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (claim == null)
                {
                    return NotFound();
                }

                claim.UpdateStatus(ClaimStatus.UnderReview, $"Information requested by Academic Manager: {message}", User.Identity?.Name);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Information request sent to {claim.Lecturer?.FullName}.";
                return RedirectToAction(nameof(PendingClaims));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error sending information request: {ex.Message}";
                return RedirectToAction(nameof(PendingClaims));
            }
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

        public async Task<IActionResult> AllLecturers(string? department, string? status)
        {
            var query = _context.Users
                .Where(u => u.Role == UserRole.Lecturer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(department))
            {
                query = query.Where(u => u.Department == department);
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "Active")
                {
                    query = query.Where(u => u.IsActive);
                }
                else if (status == "Inactive")
                {
                    query = query.Where(u => !u.IsActive);
                }
            }

            var lecturers = await query
                .OrderBy(u => u.Department)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            // Get unique departments for filter dropdown
            var departments = await _context.Users
                .Where(u => u.Department != null)
                .Select(u => u.Department!)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            ViewBag.Departments = departments;
            return View(lecturers);
        }

        public async Task<IActionResult> LecturerDetails(string id)
        {
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

            var model = new LecturerClaimsViewModel
            {
                Lecturer = lecturer,
                Claims = claims
            };

            return View(model);
        }

        public async Task<IActionResult> ProgrammeCoordinators()
        {
            var coordinators = await _context.Users
                .Where(u => u.Role == UserRole.ProgrammeCoordinator && u.IsActive)
                .OrderBy(u => u.Department)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            return View(coordinators);
        }

        public async Task<IActionResult> CoordinatorDetails(string id)
        {
            var coordinator = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRole.ProgrammeCoordinator);

            if (coordinator == null)
            {
                return NotFound();
            }

            // Get department claims statistics for this coordinator
            var departmentStats = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Lecturer!.Department == coordinator.Department)
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count(), Amount = g.Sum(c => c.TotalAmount) })
                .ToListAsync();

            var model = new CoordinatorDetailsViewModel
            {
                Coordinator = coordinator,
                DepartmentStats = departmentStats.ToDictionary(
                    x => x.Status.ToString(),
                    x => new DepartmentStat { Count = x.Count, Amount = x.Amount }
                )
            };

            return View(model);
        }

        public async Task<IActionResult> DepartmentAnalytics()
        {
            var startDate = DateTime.Now.AddMonths(-3);
            var endDate = DateTime.Now;

            var departmentStats = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.SubmissionDate >= startDate && c.SubmissionDate <= endDate)
                .GroupBy(c => c.Lecturer!.Department)
                .Select(g => new DepartmentAnalyticsViewModel
                {
                    Department = g.Key ?? "Unknown",
                    TotalClaims = g.Count(),
                    ApprovedClaims = g.Count(c => c.Status == ClaimStatus.Approved),
                    PendingClaims = g.Count(c => c.Status == ClaimStatus.Pending),
                    TotalAmount = g.Where(c => c.Status == ClaimStatus.Approved).Sum(c => c.TotalAmount),
                    AverageProcessingDays = g.Where(c => c.Status == ClaimStatus.Approved && c.ProcessingDays.HasValue)
                                           .Average(c => c.ProcessingDays) ?? 0
                })
                .OrderByDescending(d => d.TotalAmount)
                .ToListAsync();

            var model = new DepartmentAnalyticsSummaryViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                DepartmentStats = departmentStats
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateAnalyticsReport(DepartmentAnalyticsSummaryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("DepartmentAnalytics", model);
            }

            var departmentStats = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.SubmissionDate >= model.StartDate && c.SubmissionDate <= model.EndDate)
                .GroupBy(c => c.Lecturer!.Department)
                .Select(g => new DepartmentAnalyticsViewModel
                {
                    Department = g.Key ?? "Unknown",
                    TotalClaims = g.Count(),
                    ApprovedClaims = g.Count(c => c.Status == ClaimStatus.Approved),
                    PendingClaims = g.Count(c => c.Status == ClaimStatus.Pending),
                    TotalAmount = g.Where(c => c.Status == ClaimStatus.Approved).Sum(c => c.TotalAmount),
                    AverageProcessingDays = g.Where(c => c.Status == ClaimStatus.Approved && c.ProcessingDays.HasValue)
                                           .Average(c => c.ProcessingDays) ?? 0
                })
                .OrderByDescending(d => d.TotalAmount)
                .ToListAsync();

            model.DepartmentStats = departmentStats;
            return View("DepartmentAnalytics", model);
        }

        public async Task<IActionResult> FinancialReports()
        {
            var startDate = DateTime.Now.AddMonths(-1);
            var endDate = DateTime.Now;

            var model = new FinancialReportsViewModel
            {
                StartDate = startDate,
                EndDate = endDate
            };

            // Get approved claims for the period
            var approvedClaims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Approved &&
                           c.ApprovalDate >= startDate &&
                           c.ApprovalDate <= endDate)
                .ToListAsync();

            model.ApprovedClaims = approvedClaims;
            model.TotalAmount = approvedClaims.Sum(c => c.TotalAmount);
            model.TotalClaims = approvedClaims.Count;

            // Get monthly trend data
            var monthlyTrends = await _context.Claims
                .Where(c => c.Status == ClaimStatus.Approved && c.ApprovalDate >= startDate.AddMonths(-6))
                .GroupBy(c => new { c.ApprovalDate!.Value.Year, c.ApprovalDate.Value.Month })
                .Select(g => new MonthlyTrendViewModel
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalAmount = g.Sum(c => c.TotalAmount),
                    ClaimCount = g.Count()
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToListAsync();

            model.MonthlyTrends = monthlyTrends;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateFinancialReport(FinancialReportsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("FinancialReports", model);
            }

            var approvedClaims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Approved &&
                           c.ApprovalDate >= model.StartDate &&
                           c.ApprovalDate <= model.EndDate)
                .ToListAsync();

            model.ApprovedClaims = approvedClaims;
            model.TotalAmount = approvedClaims.Sum(c => c.TotalAmount);
            model.TotalClaims = approvedClaims.Count;

            // Get monthly trend data
            var monthlyTrends = await _context.Claims
                .Where(c => c.Status == ClaimStatus.Approved && c.ApprovalDate >= model.StartDate.AddMonths(-6))
                .GroupBy(c => new { c.ApprovalDate!.Value.Year, c.ApprovalDate.Value.Month })
                .Select(g => new MonthlyTrendViewModel
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalAmount = g.Sum(c => c.TotalAmount),
                    ClaimCount = g.Count()
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToListAsync();

            model.MonthlyTrends = monthlyTrends;

            return View("FinancialReports", model);
        }

        public async Task<IActionResult> GenerateComprehensiveReport(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                TempData["Error"] = "Start date cannot be after end date.";
                return RedirectToAction(nameof(FinancialReports));
            }

            var claims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Approved &&
                           c.ApprovalDate >= startDate &&
                           c.ApprovalDate <= endDate)
                .OrderBy(c => c.Lecturer!.Department)
                .ThenBy(c => c.Lecturer!.LastName)
                .ToListAsync();

            var departmentSummary = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Approved &&
                           c.ApprovalDate >= startDate &&
                           c.ApprovalDate <= endDate)
                .GroupBy(c => c.Lecturer!.Department)
                .Select(g => new DepartmentSummaryViewModel
                {
                    Department = g.Key ?? "Unknown",
                    TotalAmount = g.Sum(c => c.TotalAmount),
                    ClaimCount = g.Count(),
                    AverageAmount = g.Average(c => c.TotalAmount)
                })
                .OrderByDescending(d => d.TotalAmount)
                .ToListAsync();

            var model = new ComprehensiveReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                Claims = claims,
                DepartmentSummary = departmentSummary,
                GeneratedDate = DateTime.Now,
                GeneratedBy = User.Identity?.Name ?? "Unknown"
            };

            return View("ComprehensiveReport", model);
        }

        public async Task<IActionResult> PerformanceMetrics()
        {
            var model = new PerformanceMetricsViewModel
            {
                StartDate = DateTime.Now.AddMonths(-3),
                EndDate = DateTime.Now
            };

            return View(model);
        }

        public async Task<IActionResult> ExportApprovedClaims(string format, string? department, string? month)
        {
            var query = _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Approved)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(department))
            {
                query = query.Where(c => c.Lecturer!.Department == department);
            }

            if (!string.IsNullOrEmpty(month) && DateTime.TryParse(month + "-01", out var monthFilter))
            {
                query = query.Where(c => c.Month.Year == monthFilter.Year && c.Month.Month == monthFilter.Month);
            }

            var claims = await query
                .OrderByDescending(c => c.ApprovalDate)
                .ToListAsync();

            // For now, just return JSON - you can implement CSV/Excel export later
            return Json(new
            {
                success = true,
                data = claims.Select(c => new {
                    Lecturer = c.Lecturer?.FullName,
                    Department = c.Lecturer?.Department,
                    Month = c.Month.ToString("MMM yyyy"),
                    HoursWorked = c.HoursWorked,
                    HourlyRate = c.HourlyRate,
                    TotalAmount = c.TotalAmount,
                    ApprovalDate = c.ApprovalDate?.ToString("yyyy-MM-dd"),
                    ProcessingDays = c.ProcessingDays
                })
            });
        }

        public async Task<IActionResult> GetAcademicManagerStatistics()
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

                var departmentPerformance = await _context.Claims
                    .Include(c => c.Lecturer)
                    .Where(c => c.Status == ClaimStatus.Approved && c.ApprovalDate >= startOfMonth)
                    .GroupBy(c => c.Lecturer!.Department)
                    .Select(g => new { Department = g.Key, Amount = g.Sum(c => c.TotalAmount), Count = g.Count() })
                    .OrderByDescending(x => x.Amount)
                    .Take(5)
                    .ToListAsync();

                var coordinatorStats = await _context.Users
                    .Where(u => u.Role == UserRole.ProgrammeCoordinator && u.IsActive)
                    .GroupBy(u => u.Department)
                    .Select(g => new { Department = g.Key, CoordinatorCount = g.Count() })
                    .ToListAsync();

                return Json(new
                {
                    monthlyStats = monthlyClaims,
                    departmentPerformance = departmentPerformance,
                    coordinatorStats = coordinatorStats
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}