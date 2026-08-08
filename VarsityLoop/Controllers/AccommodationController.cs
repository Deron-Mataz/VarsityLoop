using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VarsityLoop.Models.Entities;
using VarsityLoop.Models.ViewModels.Accommodation;
using VarsityLoop.Repositories.Interfaces;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Controllers
{
    /// <summary>
    /// Deliberately separate from ListingsController - Accommodation is its
    /// own platform area, not a Marketplace module (see Accommodation.cs).
    /// Publishing is gated on ApplicationUser.LandlordVerificationStatus ==
    /// "Approved", enforced in AccommodationService.CreateAsync itself (not
    /// just hidden in the UI here).
    /// </summary>
    public class AccommodationController : Controller
    {
        private readonly IAccommodationService _accommodationService;
        private readonly IUserRepository _userRepository;

        public AccommodationController(IAccommodationService accommodationService, IUserRepository userRepository)
        {
            _accommodationService = accommodationService;
            _userRepository = userRepository;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private bool CurrentUserIsModerator => User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.SuperAdmin);

        [HttpGet]
        public async Task<IActionResult> Index(string? university, ResidenceClassification? classification,
            AccommodationType? accommodationType, double? maxRent, GenderPreference? genderPreference, int page = 1)
        {
            ViewData["Title"] = "Student Accommodation";

            var query = new AccommodationBrowseQuery
            {
                University = university,
                Classification = classification,
                AccommodationType = accommodationType,
                MaxRent = maxRent,
                GenderPreference = genderPreference,
                Page = page
            };

            var result = await _accommodationService.BrowseAsync(query);
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var preview = await _accommodationService.GetDetailsAsync(id, countView: false);
            if (preview == null) return RedirectToAction("NotFound404", "Error");

            var countView = CurrentUserId == null || preview.LandlordId != CurrentUserId;
            var accommodation = countView ? await _accommodationService.GetDetailsAsync(id, countView: true) : preview;

            if (accommodation == null || (accommodation.Status != nameof(AccommodationStatus.Active) && accommodation.LandlordId != CurrentUserId && !CurrentUserIsModerator))
            {
                return RedirectToAction("NotFound404", "Error");
            }

            ViewData["Title"] = accommodation.ResidenceName;
            return View(accommodation);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userRepository.GetByFirebaseUidAsync(CurrentUserId!);

            if (currentUser?.LandlordVerificationStatus != "Approved")
            {
                TempData["ErrorMessage"] = "Only verified landlords can publish accommodation listings. Landlord verification is coming soon - contact support in the meantime.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = "List a Residence";

            var model = new AccommodationFormViewModel();
            if (currentUser != null) model.University = currentUser.University;

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AccommodationFormViewModel model)
        {
            ViewData["Title"] = "List a Residence";

            if (!ModelState.IsValid) return View(model);

            var currentUser = await _userRepository.GetByFirebaseUidAsync(CurrentUserId!);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var isVerifiedLandlord = currentUser.LandlordVerificationStatus == "Approved";

            var result = await _accommodationService.CreateAsync(model, currentUser.Id, currentUser.FullName, isVerifiedLandlord);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Couldn't create residence.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Your residence is live.";
            return RedirectToAction(nameof(Details), new { id = result.Data });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var accommodation = await _accommodationService.GetDetailsAsync(id, countView: false);

            if (accommodation == null) return RedirectToAction("NotFound404", "Error");
            if (accommodation.LandlordId != CurrentUserId && !CurrentUserIsModerator) return RedirectToAction("Forbidden403", "Error");

            ViewData["Title"] = "Edit Residence";

            var model = new AccommodationFormViewModel
            {
                Id = accommodation.Id,
                ResidenceName = accommodation.ResidenceName,
                Classification = Enum.TryParse<ResidenceClassification>(accommodation.Classification, out var c) ? c : ResidenceClassification.Private,
                AccommodationType = Enum.TryParse<AccommodationType>(accommodation.AccommodationType, out var t) ? t : AccommodationType.SingleRoom,
                MonthlyRent = accommodation.MonthlyRent,
                Deposit = accommodation.Deposit,
                University = accommodation.University,
                DistanceFromCampus = accommodation.DistanceFromCampus,
                AvailableFrom = accommodation.AvailableFrom.ToDateTime(),
                LeasePeriod = accommodation.LeasePeriod,
                GenderPreference = Enum.TryParse<GenderPreference>(accommodation.GenderPreference, out var g) ? g : GenderPreference.Any,
                Description = accommodation.Description,
                GoogleMapsUrl = accommodation.GoogleMapsUrl,
                ExistingGalleryUrls = accommodation.Gallery
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AccommodationFormViewModel model)
        {
            ViewData["Title"] = "Edit Residence";

            if (!ModelState.IsValid) return View(model);

            var result = await _accommodationService.UpdateAsync(model, CurrentUserId!, CurrentUserIsModerator);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Couldn't update residence.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Residence updated.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            await _accommodationService.DeleteAsync(id, CurrentUserId!, CurrentUserIsModerator);
            TempData["SuccessMessage"] = "Residence deleted.";
            return RedirectToAction(nameof(MyResidences));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pause(string id)
        {
            await _accommodationService.SetPausedAsync(id, true, CurrentUserId!, CurrentUserIsModerator);
            return RedirectToAction(nameof(MyResidences));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(string id)
        {
            await _accommodationService.SetPausedAsync(id, false, CurrentUserId!, CurrentUserIsModerator);
            return RedirectToAction(nameof(MyResidences));
        }

        [Authorize]
        [HttpGet]
        [Authorize(Policy = "ApprovedLandlord")]
        public async Task<IActionResult> MyResidences()
        {
            ViewData["Title"] = "My Residences";
            var residences = await _accommodationService.GetMyResidencesAsync(CurrentUserId!);
            return View(residences);
        }
    }
}
