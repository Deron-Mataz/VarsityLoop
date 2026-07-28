using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VarsityLoop.Models.Entities;
using VarsityLoop.Models.ViewModels.Admin;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Controllers
{
    /// <summary>
    /// Every action under /Admin requires the Admin or SuperAdmin role, read live
    /// from the signed-in user's cookie claims (populated at login time from
    /// Firestore - see AccountController.Login). The full CMS dashboard, user
    /// management, listing moderation, etc. are built out in Phase 6; this
    /// controller exists now to prove the RBAC gate itself works end-to-end,
    /// plus the Site Settings CMS (Phase 3).
    /// </summary>
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.SuperAdmin}")]
    public class AdminController : Controller
    {
        private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/webp" };
        private const long MaxImageBytes = 2 * 1024 * 1024; // 2 MB

        private readonly ISiteSettingsService _siteSettingsService;
        private readonly IStorageService _storageService;

        public AdminController(ISiteSettingsService siteSettingsService, IStorageService storageService)
        {
            _siteSettingsService = siteSettingsService;
            _storageService = storageService;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Admin Dashboard";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            ViewData["Title"] = "Site Settings";

            var settings = await _siteSettingsService.GetSettingsAsync();

            var model = new SiteSettingsViewModel
            {
                SiteName = settings.SiteName,
                LogoUrl = settings.LogoUrl,
                FaviconUrl = settings.FaviconUrl,
                PrimaryColour = settings.PrimaryColour,
                AccentColour = settings.AccentColour,
                HeroHeading = settings.HeroHeading,
                HeroDescription = settings.HeroDescription,
                FooterText = settings.FooterText,
                SupportEmail = settings.SupportEmail,
                SupportPhone = settings.SupportPhone,
                FacebookLink = settings.FacebookLink,
                InstagramLink = settings.InstagramLink,
                LinkedInLink = settings.LinkedInLink,
                TwitterLink = settings.TwitterLink,
                MaintenanceMode = settings.MaintenanceMode
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(SiteSettingsViewModel model)
        {
            ViewData["Title"] = "Site Settings";

            if (model.LogoFile != null && !ValidateImage(model.LogoFile, nameof(model.LogoFile)))
            {
                ModelState.AddModelError(nameof(model.LogoFile), "Logo must be a JPG, PNG, or WEBP under 2MB.");
            }

            if (model.FaviconFile != null && !ValidateImage(model.FaviconFile, nameof(model.FaviconFile)))
            {
                ModelState.AddModelError(nameof(model.FaviconFile), "Favicon must be a JPG, PNG, or WEBP under 2MB.");
            }

            if (!ModelState.IsValid) return View(model);

            var settings = await _siteSettingsService.GetSettingsAsync();

            if (model.LogoFile != null)
            {
                await using var stream = model.LogoFile.OpenReadStream();
                model.LogoUrl = await _storageService.UploadPublicFileAsync(
                    stream, model.LogoFile.FileName, model.LogoFile.ContentType, "branding/logo");
            }

            if (model.FaviconFile != null)
            {
                await using var stream = model.FaviconFile.OpenReadStream();
                model.FaviconUrl = await _storageService.UploadPublicFileAsync(
                    stream, model.FaviconFile.FileName, model.FaviconFile.ContentType, "branding/favicon");
            }

            settings.SiteName = model.SiteName.Trim();
            settings.LogoUrl = model.LogoUrl ?? settings.LogoUrl;
            settings.FaviconUrl = model.FaviconUrl ?? settings.FaviconUrl;
            settings.PrimaryColour = model.PrimaryColour;
            settings.AccentColour = model.AccentColour;
            settings.HeroHeading = model.HeroHeading.Trim();
            settings.HeroDescription = model.HeroDescription.Trim();
            settings.FooterText = model.FooterText.Trim();
            settings.SupportEmail = model.SupportEmail?.Trim();
            settings.SupportPhone = model.SupportPhone?.Trim();
            settings.FacebookLink = model.FacebookLink?.Trim();
            settings.InstagramLink = model.InstagramLink?.Trim();
            settings.LinkedInLink = model.LinkedInLink?.Trim();
            settings.TwitterLink = model.TwitterLink?.Trim();
            settings.MaintenanceMode = model.MaintenanceMode;

            await _siteSettingsService.UpdateSettingsAsync(settings);

            TempData["SuccessMessage"] = "Site settings updated.";
            return RedirectToAction(nameof(Settings));
        }

        private static bool ValidateImage(IFormFile file, string _)
        {
            return AllowedImageTypes.Contains(file.ContentType) && file.Length > 0 && file.Length <= MaxImageBytes;
        }
    }
}
