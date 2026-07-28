using Microsoft.AspNetCore.Mvc;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Controllers
{
    public class HomeController : Controller
    {
        private const int HomepageListingCount = 8;

        private readonly IListingService _listingService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IListingService listingService, ILogger<HomeController> logger)
        {
            _listingService = listingService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Home";

            var newest = await _listingService.BrowseAsync(HomepageListingCount);

            // No separate "featured" flag yet (that's a Phase 5+ concept) - for now
            // the most-viewed of the newest batch stands in as "Featured".
            ViewData["FeaturedListings"] = newest.Items.OrderByDescending(l => l.Views).Take(4).ToList();
            ViewData["NewestListings"] = newest.Items.Take(4).ToList();

            return View();
        }
    }
}
