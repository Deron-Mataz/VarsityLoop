using Microsoft.AspNetCore.Mvc;

namespace VarsityLoop.Controllers
{
    /// <summary>
    /// Placeholder for the future Accommodation marketplace module. No backend
    /// functionality yet - see PROJECT PROMPT "ACCOMMODATION PLACEHOLDER".
    /// When this module is built out, it'll follow the same
    /// Repository/Service/Controller pattern as Listings (Phase 4) rather than
    /// anything Accommodation-specific bolted onto Listing itself.
    /// </summary>
    public class AccommodationController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Accommodation";
            ViewData["ModuleName"] = "Accommodation";
            return View("~/Views/Shared/ComingSoon.cshtml");
        }
    }
}
