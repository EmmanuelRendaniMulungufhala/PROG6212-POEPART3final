using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WebApplication1.Models;
using WebApplication1.Data;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "ProgrammeCoordinator")]
    public class ProgrammeCoordinatorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProgrammeCoordinatorController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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

            var model = new CoordinatorDashboardViewModel
            {
                TotalClaims = await _context.Claims.CountAsync(c => c.Lecturer!.Department == currentUser.Department),
                PendingClaims = await _context.Claims.CountAsync(c =>
                    c.Lecturer!.Department == currentUser.Department &&
                    c.Status == ClaimStatus.Pending),
                ApprovedClaims = await _context.Claims.CountAsync(c =>
                    c.Lecturer!.Department == currentUser.Department &&
                    c.Status == ClaimStatus.Approved),
                RejectedClaims = await _context.Claims.CountAsync(c =>
                    c.Lecturer!.Department == currentUser.Department &&
                    c.Status == ClaimStatus.Rejected),
                UnderReviewClaims = await _context.Claims.CountAsync(c =>
                    c.Lecturer!.Department == currentUser.Department &&
                    c.Status == ClaimStatus.UnderReview),
                RecentClaims = await _context.Claims
                    .Include(c => c.Lecturer)
                    .Where(c => c.Lecturer!.Department == currentUser.Department &&
                               (c.Status == ClaimStatus.Pending || c.Status == ClaimStatus.UnderReview))
                    .OrderByDescending(c => c.SubmissionDate)
                    .Take(10)
                    .ToListAsync(),
                Department = currentUser.Department ?? "All Departments"
            };

            // Calculate monthly statistics for the department
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var approvedThisMonth = await _context.Claims
                .Where(c => c.Lecturer!.Department == currentUser.Department &&
                           c.Status == ClaimStatus.Approved &&
                           c.ApprovalDate >= startOfMonth)
                .ToListAsync();

            model.TotalAmountThisMonth = approvedThisMonth.Sum(c => c.TotalAmount);
            model.ApprovedClaimsThisMonth = approvedThisMonth.Count;

            return View(model);
        }

        public async Task<IActionResult> ReviewClaims(string? status, string? month, string? lecturer)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return NotFound();
            }

            var query = _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Lecturer!.Department == currentUser.Department)
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
                                        c.Lecturer!.LastName.Contains(lecturer));
            }

            var claims = await query
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();

            ViewBag.Department = currentUser.Department;
            return View(claims);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveClaim(Guid claimId, string? approvalNotes)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(ReviewClaims));
                }

                var claim = await _context.Claims
                    .Include(c => c.Lecturer)
                    .FirstOrDefaultAsync(c => c.Id == claimId && c.Lecturer!.Department == currentUser.Department);

                if (claim == null)
                {
                    TempData["Error"] = "Claim not found or you don't have permission to approve this claim.";
                    return RedirectToAction(nameof(ReviewClaims));
                }

                if (claim.Status == ClaimStatus.Approved)
                {
                    TempData["Warning"] = "Claim is already approved.";
                    return RedirectToAction(nameof(ReviewClaims));
                }

                claim.UpdateStatus(ClaimStatus.Approved, approvalNotes, User.Identity?.Name);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Claim for {claim.Lecturer?.FullName} has been approved successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error approving claim: {ex.Message}";
            }

            return RedirectToAction(nameof(ReviewClaims));
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
                    return RedirectToAction(nameof(ReviewClaims));
                }

                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(ReviewClaims));
                }

                var claim = await _context.Claims
                    .Include(c => c.Lecturer)
                    .FirstOrDefaultAsync(c => c.Id == claimId && c.Lecturer!.Department == currentUser.Department);

                if (claim == null)
                {
                    TempData["Error"] = "Claim not found or you don't have permission to reject this claim.";
                    return RedirectToAction(nameof(ReviewClaims));
                }

                if (claim.Status == ClaimStatus.Rejected)
                {
                    TempData["Warning"] = "Claim is already rejected.";
                    return RedirectToAction(nameof(ReviewClaims));
                }

                claim.UpdateStatus(ClaimStatus.Rejected, rejectionNotes, User.Identity?.Name);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Claim for {claim.Lecturer?.FullName} has been rejected.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error rejecting claim: {ex.Message}";
            }

            return RedirectToAction(nameof(ReviewClaims));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendForReview(Guid claimId, string? reviewNotes)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(ReviewClaims));
                }

                var claim = await _context.Claims
                    .Include(c => c.Lecturer)
                    .FirstOrDefaultAsync(c => c.Id == claimId && c.Lecturer!.Department == currentUser.Department);

                if (claim == null)
                {
                    TempData["Error"] = "Claim not found or you don't have permission to review this claim.";
                    return RedirectToAction(nameof(ReviewClaims));
                }

                claim.UpdateStatus(ClaimStatus.UnderReview, reviewNotes, User.Identity?.Name);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Claim for {claim.Lecturer?.FullName} has been marked for review.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating claim status: {ex.Message}";
            }

            return RedirectToAction(nameof(ReviewClaims));
        }

        public async Task<IActionResult> ClaimDetails(Guid id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return NotFound();
            }

            var claim = await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .Include(c => c.StatusHistory)
                .FirstOrDefaultAsync(c => c.Id == id && c.Lecturer!.Department == currentUser.Department);

            if (claim == null)
            {
                return NotFound();
            }

            return View(claim);
        }

        public async Task<IActionResult> DepartmentLecturers()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return NotFound();
            }

            var lecturers = await _context.Users
                .Where(u => u.Role == UserRole.Lecturer && u.Department == currentUser.Department && u.IsActive)
                .OrderBy(u => u.LastName)
                .ToListAsync();

            ViewBag.Department = currentUser.Department;
            return View(lecturers);
        }

        public async Task<IActionResult> LecturerClaims(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return NotFound();
            }

            var lecturer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Department == currentUser.Department);

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

        public async Task<IActionResult> DepartmentReports()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return NotFound();
            }

            var startDate = DateTime.Now.AddMonths(-1);
            var endDate = DateTime.Now;

            var model = new CoordinatorReportsViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                Department = currentUser.Department ?? "All Departments"
            };

            // Get approved claims for the department in the period
            var approvedClaims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Lecturer!.Department == currentUser.Department &&
                           c.Status == ClaimStatus.Approved &&
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
        public async Task<IActionResult> GenerateDepartmentReport(CoordinatorReportsViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(DepartmentReports));
            }

            if (!ModelState.IsValid)
            {
                model.Department = currentUser.Department ?? "All Departments";
                return View("DepartmentReports", model);
            }

            var approvedClaims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Lecturer!.Department == currentUser.Department &&
                           c.Status == ClaimStatus.Approved &&
                           c.SubmissionDate >= model.StartDate &&
                           c.SubmissionDate <= model.EndDate)
                .ToListAsync();

            model.ApprovedClaims = approvedClaims;
            model.TotalAmount = approvedClaims.Sum(c => c.TotalAmount);
            model.TotalClaims = approvedClaims.Count;
            model.Department = currentUser.Department ?? "All Departments";

            return View("DepartmentReports", model);
        }

        public async Task<IActionResult> GenerateDepartmentPaymentReport(DateTime startDate, DateTime endDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(DepartmentReports));
            }

            if (startDate > endDate)
            {
                TempData["Error"] = "Start date cannot be after end date.";
                return RedirectToAction(nameof(DepartmentReports));
            }

            var claims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Lecturer!.Department == currentUser.Department &&
                           c.Status == ClaimStatus.Approved &&
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
                GeneratedBy = User.Identity?.Name ?? "Unknown",
                Department = currentUser.Department ?? "All Departments"
            };

            return View("DepartmentPaymentReport", model);
        }

        public async Task<IActionResult> GetDepartmentStatistics()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Json(new { error = "User not found" });
                }

                var today = DateTime.Today;
                var startOfMonth = new DateTime(today.Year, today.Month, 1);

                var monthlyClaims = await _context.Claims
                    .Where(c => c.Lecturer!.Department == currentUser.Department &&
                               c.SubmissionDate >= startOfMonth)
                    .GroupBy(c => c.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                var lecturerStats = await _context.Claims
                    .Include(c => c.Lecturer)
                    .Where(c => c.Lecturer!.Department == currentUser.Department &&
                               c.Status == ClaimStatus.Approved &&
                               c.SubmissionDate >= startOfMonth)
                    .GroupBy(c => c.Lecturer!.FullName)
                    .Select(g => new { Lecturer = g.Key, Amount = g.Sum(c => c.TotalAmount) })
                    .OrderByDescending(x => x.Amount)
                    .Take(5)
                    .ToListAsync();

                return Json(new
                {
                    monthlyStats = monthlyClaims,
                    lecturerStats = lecturerStats
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateClaimStatus(Guid claimId, ClaimStatus status, string? notes)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(ReviewClaims));
                }

                var claim = await _context.Claims
                    .Include(c => c.Lecturer)
                    .FirstOrDefaultAsync(c => c.Id == claimId && c.Lecturer!.Department == currentUser.Department);

                if (claim == null)
                {
                    TempData["Error"] = "Claim not found or you don't have permission to update this claim.";
                    return RedirectToAction(nameof(ReviewClaims));
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

            return RedirectToAction(nameof(ReviewClaims));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickApprove(Guid claimId, string? approvalNotes)
        {
            return await UpdateClaimStatus(claimId, ClaimStatus.Approved, approvalNotes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickReject(Guid claimId, string rejectionNotes)
        {
            return await UpdateClaimStatus(claimId, ClaimStatus.Rejected, rejectionNotes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickReview(Guid claimId, string? reviewNotes)
        {
            return await UpdateClaimStatus(claimId, ClaimStatus.UnderReview, reviewNotes);
        }
    }
}