using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var model = new DashboardViewModel();

            if (User.Identity?.IsAuthenticated == true)
            {
                // Simple data for testing
                model.PendingClaims = 2;
                model.ApprovedClaims = 5;
                model.PendingApprovals = 3;

                model.RecentActivities = new List<ActivityItem>
                {
                    new ActivityItem
                    {
                        Date = DateTime.Now.AddDays(-1),
                        Description = "Monthly teaching claim submitted",
                        Status = "Pending",
                        Amount = 4500.00m
                    },
                    new ActivityItem
                    {
                        Date = DateTime.Now.AddDays(-3),
                        Description = "Workshop facilitation claim",
                        Status = "Approved",
                        Amount = 3200.00m
                    }
                };
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}