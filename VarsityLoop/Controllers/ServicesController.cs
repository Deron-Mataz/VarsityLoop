using Microsoft.AspNetCore.Mvc;

namespace VarsityLoop.Controllers
{
    /// <summary>Placeholder for the future Student Services marketplace module - see AccommodationController.</summary>
    public class ServicesController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Services";
            ViewData["ModuleName"] = "Services";
            return View("~/Views/Shared/ComingSoon.cshtml");
        }
    }
}
