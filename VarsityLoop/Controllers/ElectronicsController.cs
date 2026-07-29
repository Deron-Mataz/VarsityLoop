using Microsoft.AspNetCore.Mvc;

namespace VarsityLoop.Controllers
{
    /// <summary>Placeholder for the future Electronics marketplace module - see AccommodationController.</summary>
    public class ElectronicsController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Electronics";
            ViewData["ModuleName"] = "Electronics";
            return View("~/Views/Shared/ComingSoon.cshtml");
        }
    }
}
