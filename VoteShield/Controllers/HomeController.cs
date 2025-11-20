using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoteShield.Data;
using VoteShield.Models;

namespace VoteShield.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            var stats = new
            {
                TotalReports = _context.Reports.Count(),
                PendingReports = _context.Reports.Count(r => r.Status == ReportStatus.Pending),
                ResolvedCases = _context.Reports.Count(r => r.Status == ReportStatus.Resolved),
                ActiveElections = _context.ElectionEvents.Count(e => e.IsActive)
            };

            ViewBag.Stats = stats;
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}