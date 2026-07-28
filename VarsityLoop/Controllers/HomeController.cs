using Microsoft.AspNetCore.Mvc;
using VarsityLoop.Models.ViewModels.Listings;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Controllers
{
    public class HomeController : Controller
    {
        private const int HomepageListingCount = 8;

        private readonly IListingService _listingService;
        private readonly ICategoryService _categoryService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IListingService listingService, ICategoryService categoryService, ILogger<HomeController> logger)
        {
            _listingService = listingService;
            _categoryService = categoryService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Home";

            var newest = await _listingService.BrowseAsync(new ListingBrowseQuery
            {
                Sort = ListingSortOption.Newest,
                Page = 1,
                PageSize = HomepageListingCount
            });

            // No separate "featured" flag yet (that's a Phase 6+ concept) - for now
            // the most-viewed of the newest batch stands in as "Featured".
            ViewData["FeaturedListings"] = newest.Items.OrderByDescending(l => l.Views).Take(4).ToList();
            ViewData["NewestListings"] = newest.Items.Take(4).ToList();
            ViewData["Categories"] = await _categoryService.GetAllAsync();

            return View();
        }
    }
}
