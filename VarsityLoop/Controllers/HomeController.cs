using Microsoft.AspNetCore.Mvc;

namespace VarsityLoop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // Featured/newest listings are wired up in Phase 4 (Listings core) once
        // the Listing entity and repository exist. For now this renders the
        // homepage shell with proper empty states, per the "no mock data" rule.
        public IActionResult Index()
        {
            ViewData["Title"] = "Home";
            return View();
        }
    }
}
