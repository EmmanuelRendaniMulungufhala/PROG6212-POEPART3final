using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.Data;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
    public class ApprovalsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ApprovalsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var pendingClaims = await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .Where(c => c.Status == ClaimStatus.Pending)
                .OrderBy(c => c.SubmissionDate)
                .ToListAsync();

            return View(pendingClaims);
        }

        public async Task<IActionResult> Review(Guid id)
        {
            var claim = await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null)
            {
                return NotFound();
            }

            var model = new ApprovalViewModel
            {
                ClaimId = claim.Id,
                Claim = claim
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id, string approvalNotes)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                return NotFound();
            }

            claim.Status = ClaimStatus.Approved;
            claim.ApprovalDate = DateTime.Now;
            claim.ApprovedBy = User.Identity?.Name ?? "Unknown";
            claim.ApprovalNotes = approvalNotes;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Claim approved successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id, string rejectionReason)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                return NotFound();
            }

            claim.Status = ClaimStatus.Rejected;
            claim.ApprovalDate = DateTime.Now;
            claim.ApprovedBy = User.Identity?.Name ?? "Unknown";
            claim.ApprovalNotes = rejectionReason;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Claim rejected successfully!";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "AcademicManager")]
        public async Task<IActionResult> ApprovedClaims()
        {
            var approvedClaims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Approved)
                .OrderByDescending(c => c.ApprovalDate)
                .ToListAsync();

            return View(approvedClaims);
        }

        [HttpPost]
        public async Task<IActionResult> BulkApprove(List<Guid> claimIds)
        {
            if (claimIds != null && claimIds.Any())
            {
                var claims = await _context.Claims
                    .Where(c => claimIds.Contains(c.Id))
                    .ToListAsync();

                foreach (var claim in claims)
                {
                    claim.Status = ClaimStatus.Approved;
                    claim.ApprovalDate = DateTime.Now;
                    claim.ApprovedBy = User.Identity?.Name ?? "Unknown";
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"{claims.Count} claims approved successfully!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}