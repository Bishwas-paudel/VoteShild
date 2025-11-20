using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoteShield.Data;
using VoteShield.Models;

namespace VoteShield.Controllers
{
    public class ReportsController : Controller
    {
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
    }
}