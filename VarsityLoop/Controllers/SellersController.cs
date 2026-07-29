using Microsoft.AspNetCore.Mvc;
using VarsityLoop.Models.Entities;
using VarsityLoop.Repositories.Interfaces;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Controllers
{
    public class SellersController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IListingService _listingService;

        public SellersController(IUserRepository userRepository, IListingService listingService)
        {
            _userRepository = userRepository;
            _listingService = listingService;
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var seller = await _userRepository.GetByIdAsync(id);
            if (seller == null)
            {
                return RedirectToAction("NotFound404", "Error");
            }

            ViewData["Title"] = seller.FullName;

            // GetMyListingsAsync returns every status for the owner's own dashboard -
            // here we're a visitor, so only their Active listings are shown publicly.
            var sellerListings = await _listingService.GetMyListingsAsync(id);
            ViewData["SellerListings"] = sellerListings
                .Where(l => l.Status == nameof(ListingStatus.Active))
                .ToList();

            return View(seller);
        }
    }
}
