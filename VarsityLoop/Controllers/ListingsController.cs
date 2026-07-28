using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VarsityLoop.Models.Entities;
using VarsityLoop.Models.ViewModels.Listings;
using VarsityLoop.Repositories.Interfaces;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Controllers
{
    public class ListingsController : Controller
    {
        private readonly IListingService _listingService;
        private readonly ICategoryService _categoryService;
        private readonly IReportService _reportService;
        private readonly IUserRepository _userRepository;

        public ListingsController(IListingService listingService, ICategoryService categoryService, IReportService reportService, IUserRepository userRepository)
        {
            _listingService = listingService;
            _categoryService = categoryService;
            _reportService = reportService;
            _userRepository = userRepository;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private bool CurrentUserIsModerator => User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.SuperAdmin);

        [HttpGet]
        public async Task<IActionResult> Browse(string? q, string? categoryId, double? minPrice, double? maxPrice,
            ListingCondition? condition, ListingSortOption sort = ListingSortOption.Newest, int page = 1)
        {
            ViewData["Title"] = "Textbooks";

            var query = new ListingBrowseQuery
            {
                SearchTerm = q,
                CategoryId = categoryId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Condition = condition,
                Sort = sort,
                Page = page
            };

            var result = await _listingService.BrowseAsync(query);
            var categories = await _categoryService.GetAllAsync();

            var model = new ListingBrowseViewModel
            {
                Result = result,
                Query = query,
                Categories = categories
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var preview = await _listingService.GetDetailsAsync(id, countView: false);
            if (preview == null)
            {
                return RedirectToAction("NotFound404", "Error");
            }

            // Don't count the seller's own views of their listing.
            var countView = CurrentUserId == null || preview.SellerId != CurrentUserId;
            var listing = countView ? await _listingService.GetDetailsAsync(id, countView: true) : preview;

            if (listing == null || (listing.Status != nameof(ListingStatus.Active) && listing.SellerId != CurrentUserId && !CurrentUserIsModerator))
            {
                return RedirectToAction("NotFound404", "Error");
            }

            ViewData["Title"] = listing.Title;
            return View(listing);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Create Listing";

            var model = new ListingFormViewModel();

            var currentUser = await _userRepository.GetByFirebaseUidAsync(CurrentUserId!);
            if (currentUser != null) model.University = currentUser.University;

            await PopulateCategoriesAsync();
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ListingFormViewModel model)
        {
            ViewData["Title"] = "Create Listing";

            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAsync();
                return View(model);
            }

            var currentUser = await _userRepository.GetByFirebaseUidAsync(CurrentUserId!);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var result = await _listingService.CreateAsync(model, currentUser.Id, currentUser.FullName);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Couldn't create listing.");
                await PopulateCategoriesAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = "Your listing is live.";
            return RedirectToAction(nameof(Details), new { id = result.Data });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var listing = await _listingService.GetDetailsAsync(id, countView: false);

            if (listing == null) return RedirectToAction("NotFound404", "Error");
            if (listing.SellerId != CurrentUserId && !CurrentUserIsModerator) return RedirectToAction("Forbidden403", "Error");

            ViewData["Title"] = "Edit Listing";

            var model = new ListingFormViewModel
            {
                Id = listing.Id,
                CategoryId = listing.CategoryId,
                Title = listing.Title,
                Description = listing.Description,
                Price = listing.Price,
                Author = listing.Author,
                Isbn = listing.Isbn,
                Course = listing.Course,
                Faculty = listing.Faculty,
                Condition = Enum.TryParse<ListingCondition>(listing.Condition, out var c) ? c : ListingCondition.Good,
                University = listing.University,
                Location = listing.Location,
                ExistingImageUrls = listing.ImageUrls
            };

            await PopulateCategoriesAsync();
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ListingFormViewModel model)
        {
            ViewData["Title"] = "Edit Listing";

            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAsync();
                return View(model);
            }

            var result = await _listingService.UpdateAsync(model, CurrentUserId!, CurrentUserIsModerator);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Couldn't update listing.");
                await PopulateCategoriesAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = "Listing updated.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            await _listingService.DeleteAsync(id, CurrentUserId!, CurrentUserIsModerator);
            TempData["SuccessMessage"] = "Listing deleted.";
            return RedirectToAction(nameof(MyListings));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pause(string id)
        {
            await _listingService.SetPausedAsync(id, true, CurrentUserId!, CurrentUserIsModerator);
            return RedirectToAction(nameof(MyListings));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(string id)
        {
            await _listingService.SetPausedAsync(id, false, CurrentUserId!, CurrentUserIsModerator);
            return RedirectToAction(nameof(MyListings));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Report(string id, string reason)
        {
            var listing = await _listingService.GetDetailsAsync(id, countView: false);
            if (listing == null) return RedirectToAction("NotFound404", "Error");

            var currentUser = await _userRepository.GetByFirebaseUidAsync(CurrentUserId!);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var result = await _reportService.CreateAsync(listing.Id, listing.Title, currentUser.Id, currentUser.FullName, reason);

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Thanks - we've received your report and will review it."
                : result.ErrorMessage;

            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyListings()
        {
            ViewData["Title"] = "My Listings";
            var listings = await _listingService.GetMyListingsAsync(CurrentUserId!);
            return View(listings);
        }

        private async Task PopulateCategoriesAsync()
        {
            var categories = await _categoryService.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
        }
    }
}
