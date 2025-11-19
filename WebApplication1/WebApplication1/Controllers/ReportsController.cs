using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.Data;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "ProgrammeCoordinator,AcademicManager,HR")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var model = new ReportsIndexViewModel
            {
                StartDate = DateTime.Now.AddMonths(-1),
                EndDate = DateTime.Now
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateClaimReport(ReportsIndexViewModel model)
        {
            var claims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.SubmissionDate >= model.StartDate && c.SubmissionDate <= model.EndDate)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();

            var reportModel = new ClaimReportViewModel
            {
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Claims = claims,
                GeneratedDate = DateTime.Now,
                GeneratedBy = User.Identity?.Name ?? "Unknown"
            };

            return View("ClaimReport", reportModel);
        }

        [Authorize(Roles = "AcademicManager,HR")]
        public async Task<IActionResult> ApprovalStatistics()
        {
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);

            var statistics = await _context.Claims
                .Where(c => c.SubmissionDate >= thirtyDaysAgo)
                .GroupBy(c => c.Status)
                .Select(g => new ApprovalStatistic
                {
                    Status = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(c => c.TotalAmount)
                })
                .ToListAsync();

            var model = new ApprovalStatisticsViewModel
            {
                Statistics = statistics,
                Period = "Last 30 Days"
            };

            return View(model);
        }

        [Authorize(Roles = "HR")]
        public async Task<IActionResult> LecturerPerformance()
        {
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);

            var lecturerPerformance = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.SubmissionDate >= sixMonthsAgo)
                .GroupBy(c => new { c.LecturerId, c.Lecturer!.FirstName, c.Lecturer.LastName })
                .Select(g => new LecturerPerformanceViewModel
                {
                    LecturerId = g.Key.LecturerId,
                    LecturerName = $"{g.Key.FirstName} {g.Key.LastName}",
                    TotalClaims = g.Count(),
                    ApprovedClaims = g.Count(c => c.Status == ClaimStatus.Approved),
                    TotalAmount = g.Sum(c => c.TotalAmount),
                    AverageProcessingTime = g.Average(c => (c.ApprovalDate - c.SubmissionDate).Value.TotalDays)
                })
                .OrderByDescending(l => l.TotalAmount)
                .ToListAsync();

            return View(lecturerPerformance);
        }
    }
}