using Microsoft.AspNetCore.Mvc;

namespace VarsityLoop.Controllers
{
    /// <summary>
    /// Static legal pages. Content here is placeholder boilerplate - replace
    /// with real, lawyer-reviewed Terms of Use / Privacy Policy text before
    /// this goes live with real users and real transactions.
    /// </summary>
    public class LegalController : Controller
    {
        public IActionResult Terms()
        {
            ViewData["Title"] = "Terms of Use";
            return View();
        }

        public IActionResult Privacy()
        {
            ViewData["Title"] = "Privacy Policy";
            return View();
        }
    }
}
