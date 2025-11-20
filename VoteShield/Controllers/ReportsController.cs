using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using System.Xml.Linq;
using VoteShield.Data;
using VoteShield.Models;
using VoteShield.Services;

namespace VoteShield.Controllers
{

    public class ReportsController : Controller
    {
        private readonly IDocumentService _documentService;

        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(ApplicationDbContext context, ILogger<ReportsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Reports/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Reports/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Report report)
        {
            if (ModelState.IsValid)
            {
                report.Id = Guid.NewGuid();
                report.CreatedAt = DateTime.UtcNow;
                report.AnonymousCode = GenerateAnonymousCode();
                report.Status = ReportStatus.Pending;

                _context.Add(report);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Report submitted successfully! Your anonymous code: " + report.AnonymousCode;
                return RedirectToAction(nameof(Success));
            }
            return View(report);
        }

        // GET: Reports/Success
        public IActionResult Success()
        {
            return View();
        }

        // GET: Reports/Status
        public IActionResult Status()
        {
            return View();
        }

        // POST: Reports/CheckStatus
        [HttpPost]
        public async Task<IActionResult> CheckStatus(string anonymousCode)
        {
            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.AnonymousCode == anonymousCode);

            if (report == null)
            {
                ModelState.AddModelError("", "Report not found. Please check your anonymous code.");
                return View("Status");
            }

            return View("StatusDetails", report);
        }

        // GET: Reports/Map
        public async Task<IActionResult> Map()
        {
            var reports = await _context.Reports
                .Where(r => r.Status == ReportStatus.Pending || r.Status == ReportStatus.UnderReview)
                .ToListAsync();

            return View(reports);
        }

        private string GenerateAnonymousCode()
        {
            return "VS" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Report report, List<IFormFile> evidenceFiles)
        {
            if (ModelState.IsValid)
            {
                report.Id = Guid.NewGuid();
                report.CreatedAt = DateTime.UtcNow;
                report.AnonymousCode = GenerateAnonymousCode();
                report.Status = ReportStatus.Pending;

                _context.Add(report);
                await _context.SaveChangesAsync();

                // Handle file uploads
                if (evidenceFiles != null && evidenceFiles.Any())
                {
                    foreach (var file in evidenceFiles)
                    {
                        if (file.Length > 0)
                        {
                            try
                            {
                                await _documentService.UploadDocumentAsync(
                                    file, DocumentTypes.Evidence_Photo, report.Id);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error uploading evidence file");
                                // Continue with other files
                            }
                        }
                    }
                }

                TempData["SuccessMessage"] = "Report submitted successfully! Your anonymous code: " + report.AnonymousCode;
                return RedirectToAction(nameof(Success), new { id = report.Id });
            }
            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> UploadEvidence(Guid reportId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null) return NotFound();
            ViewBag.ReportId = reportId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadEvidence(Guid reportId, List<IFormFile> files, string documentType = DocumentTypes.Evidence_Photo)
        {
            if (files == null || !files.Any())
            {
                ModelState.AddModelError("", "Please select at least one file to upload.");
                ViewBag.ReportId = reportId;
                return View();
            }

            var uploadedDocuments = new List<Document>();

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    try
                    {
                        var document = await _documentService.UploadDocumentAsync(file, documentType, reportId);
                        uploadedDocuments.Add(document);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading file: {FileName}", file.FileName);
                        ModelState.AddModelError("", $"Error uploading {file.FileName}: {ex.Message}");
                    }
                }
            }

            if (uploadedDocuments.Any())
            {
                TempData["SuccessMessage"] = $"Successfully uploaded {uploadedDocuments.Count} files. AI verification in progress.";
            }

            return RedirectToAction(nameof(Details), new { id = reportId });
        }

        [HttpGet]
        public async Task<IActionResult> DocumentDetails(Guid id)
        {
            var document = await _documentService.GetDocumentAsync(id);
            if (document == null) return NotFound();

            return View(document);
        }

        [HttpPost]
        public async Task<IActionResult> VerifyDocument(Guid documentId, string status, string comments)
        {
            var document = await _documentService.UpdateVerificationStatusAsync(documentId, status, comments);
            if (document == null) return NotFound();

            TempData["SuccessMessage"] = $"Document status updated to {status}";
            return RedirectToAction(nameof(DocumentDetails), new { id = documentId });
        }
    }
}