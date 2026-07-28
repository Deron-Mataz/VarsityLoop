using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VarsityLoop.Models.Entities;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Controllers
{
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.SuperAdmin}")]
    [Route("Admin/Listings")]
    public class AdminListingsController : Controller
    {
        private readonly IListingService _listingService;

        public AdminListingsController(IListingService listingService)
        {
            _listingService = listingService;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        private string CurrentUserName => User.Identity?.Name ?? "Unknown";

        [HttpGet("")]
        public async Task<IActionResult> Index(string? q, string? status)
        {
            ViewData["Title"] = "Listings";
            ViewData["SearchTerm"] = q;
            ViewData["StatusFilter"] = status;

            var listings = await _listingService.GetAllForAdminAsync(q, status);
            return View(listings);
        }

        [HttpPost("{id}/Suspend")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend(string id)
        {
            await _listingService.SuspendAsync(id, CurrentUserId, CurrentUserName);
            TempData["SuccessMessage"] = "Listing suspended.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{id}/Restore")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(string id)
        {
            await _listingService.RestoreAsync(id, CurrentUserId, CurrentUserName);
            TempData["SuccessMessage"] = "Listing restored.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{id}/Remove")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(string id)
        {
            await _listingService.RemoveAsync(id, CurrentUserId, CurrentUserName);
            TempData["SuccessMessage"] = "Listing removed.";
            return RedirectToAction(nameof(Index));
        }
    }
}
