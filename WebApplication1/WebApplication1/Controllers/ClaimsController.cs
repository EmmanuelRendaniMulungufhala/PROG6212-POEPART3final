using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.Data;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Lecturer")]
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ClaimsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var claims = await _context.Claims
                .Where(c => c.LecturerId == userId)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();

            return View(claims);
        }

        public IActionResult Create()
        {
            var model = new ClaimViewModel
            {
                Month = DateTime.Now
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClaimViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Challenge();
                }

                var claim = new ClaimModel
                {
                    Id = Guid.NewGuid(),
                    LecturerId = userId,
                    Month = model.Month,
                    HoursWorked = model.HoursWorked,
                    HourlyRate = model.HourlyRate,
                    TotalAmount = model.HoursWorked * model.HourlyRate,
                    AdditionalNotes = model.AdditionalNotes,
                    Status = ClaimStatus.Pending,
                    SubmissionDate = DateTime.Now,
                    SupportingDocuments = new List<SupportingDocument>()
                };

                // Handle file upload - FIXED: Now using correct property name
                if (model.SupportingDocument != null)
                {
                    var document = await SaveUploadedFile(model.SupportingDocument);
                    if (document != null)
                    {
                        claim.SupportingDocuments.Add(document);
                    }
                }

                _context.Claims.Add(claim);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Claim submitted successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var claim = await _context.Claims
                .Include(c => c.SupportingDocuments)
                .FirstOrDefaultAsync(c => c.Id == id && c.LecturerId == userId);

            if (claim == null)
            {
                return NotFound();
            }

            return View(claim);
        }

        [HttpPost]
        public async Task<IActionResult> UploadDocument(Guid claimId, IFormFile file)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var claim = await _context.Claims
                .Include(c => c.SupportingDocuments)
                .FirstOrDefaultAsync(c => c.Id == claimId && c.LecturerId == userId);

            if (claim == null)
            {
                return NotFound();
            }

            if (file != null && file.Length > 0)
            {
                var document = await SaveUploadedFile(file);
                if (document != null)
                {
                    document.ClaimId = claimId;
                    claim.SupportingDocuments.Add(document);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Document uploaded successfully!";
                }
            }

            return RedirectToAction(nameof(Details), new { id = claimId });
        }

        public async Task<IActionResult> DownloadDocument(Guid documentId)
        {
            var document = await _context.SupportingDocuments.FindAsync(documentId);
            if (document == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var claim = await _context.Claims
                .FirstOrDefaultAsync(c => c.Id == document.ClaimId && (c.LecturerId == userId || User.IsInRole("ProgrammeCoordinator") || User.IsInRole("AcademicManager")));

            if (claim == null)
            {
                return Forbid();
            }

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", document.FileName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, GetContentType(document.FileName), document.OriginalFileName);
        }

        private async Task<SupportingDocument?> SaveUploadedFile(IFormFile file)
        {
            if (file.Length > 5 * 1024 * 1024) // 5MB limit
            {
                ModelState.AddModelError("SupportingDocument", "File size must be less than 5MB");
                return null;
            }

            var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx", ".jpg", ".png" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("SupportingDocument", "Please upload PDF, DOCX, XLSX, JPG, or PNG files only");
                return null;
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return new SupportingDocument
            {
                Id = Guid.NewGuid(),
                OriginalFileName = file.FileName,
                FileName = uniqueFileName,
                FileSize = file.Length,
                ContentType = file.ContentType,
                UploadDate = DateTime.Now,
                UploadedBy = User.Identity?.Name ?? "Unknown"
            };
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".jpg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }
}