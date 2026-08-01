using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
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
        private readonly IFavoriteService _favoriteService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ListingsController> _logger;

        public ListingsController(
            IListingService listingService,
            ICategoryService categoryService,
            IReportService reportService,
            IFavoriteService favoriteService,
            IUserRepository userRepository,
            ILogger<ListingsController> logger)
        {
            _listingService = listingService;
            _categoryService = categoryService;
            _reportService = reportService;
            _favoriteService = favoriteService;
            _userRepository = userRepository;
            _logger = logger;
        }

        /// <summary>
        /// Logs exactly which field(s) failed model validation and why, so a
        /// silently-rejected form submission is never a mystery - check the
        /// Output window / application logs for a line starting
        /// "ModelState invalid on..." after any failed Create/Edit attempt.
        /// </summary>
        private void LogModelStateErrors(string action)
        {
            var errors = ModelState
                .Where(kvp => kvp.Value?.Errors.Count > 0)
                .Select(kvp => $"{kvp.Key}: {string.Join("; ", kvp.Value!.Errors.Select(e => e.ErrorMessage))}");

            _logger.LogWarning("ModelState invalid on Listings/{Action}: {Errors}", action, string.Join(" | ", errors));
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private bool CurrentUserIsModerator => User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.SuperAdmin);

        [HttpGet]
        public async Task<IActionResult> Browse(string? q, string? module, string? categoryId, double? minPrice, double? maxPrice,
            ListingCondition? condition, ListingSortOption sort = ListingSortOption.Newest, int page = 1)
        {
            ViewData["Title"] = "Marketplace";

            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Query["partial"] == "1";
            var showHomeFeed = string.IsNullOrWhiteSpace(module) && string.IsNullOrWhiteSpace(categoryId) && string.IsNullOrWhiteSpace(q);

            if (showHomeFeed)
            {
                var feed = await _listingService.GetHomeFeedAsync();
                return isAjax ? PartialView("_MarketplaceHomeFeed", feed) : View("Browse", await BuildBrowseViewModel(feed: feed));
            }

            var query = new ListingBrowseQuery
            {
                SearchTerm = q,
                Module = module,
                CategoryId = categoryId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Condition = condition,
                Sort = sort,
                Page = page
            };

            var result = await _listingService.BrowseAsync(query);

            if (isAjax)
            {
                return PartialView("_MarketplaceResults", result);
            }

            var fullModel = await BuildBrowseViewModel(query: query, result: result);
            return View(fullModel);
        }

        private async Task<ListingBrowseViewModel> BuildBrowseViewModel(ListingBrowseQuery? query = null, ListingBrowseResult? result = null, MarketplaceHomeFeed? feed = null)
        {
            var categories = await _categoryService.GetAllAsync();
            return new ListingBrowseViewModel
            {
                Query = query ?? new ListingBrowseQuery(),
                Result = result ?? new ListingBrowseResult(),
                Categories = categories,
                HomeFeed = feed
            };
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

            if (CurrentUserId != null && listing.SellerId != CurrentUserId)
            {
                ViewData["IsFavorited"] = await _favoriteService.IsFavoritedAsync(CurrentUserId, listing.Id);
            }

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

            // Diagnostic: shows exactly what the server received, independent of
            // whether model binding/validation succeeded - if a field the user
            // filled in doesn't appear here (or is blank here) despite being
            // visibly filled in the browser, the bug is upstream of this action
            // entirely (the form's HTML/JS), not in validation or the service layer.
            if (!ModelState.IsValid)
            {
                LogModelStateErrors(nameof(Create));
                await PopulateCategoriesAsync();
                return View(model);
            }

            var currentUser = await _userRepository.GetByFirebaseUidAsync(CurrentUserId!);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var result = await _listingService.CreateAsync(model, currentUser.Id, currentUser.FullName);

            if (!result.Success)
            {
                _logger.LogWarning("Listing creation rejected for user {UserId}: {ErrorMessage}", currentUser.Id, result.ErrorMessage);
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
                Type = listing.Type,
                Brand = listing.Brand,
                ProductModel = listing.Model,
                Specifications = listing.Specifications,
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
                LogModelStateErrors(nameof(Edit));
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
            ViewBag.Categories = await _categoryService.GetAllAsync();
        }
    }
}
