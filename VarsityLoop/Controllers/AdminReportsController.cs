using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VarsityLoop.Models.Entities;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Controllers
{
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.SuperAdmin}")]
    [Route("Admin/Reports")]
    public class AdminReportsController : Controller
    {
        private readonly IReportService _reportService;

        public AdminReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        private string CurrentUserName => User.Identity?.Name ?? "Unknown";

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Reports";
            var reports = await _reportService.GetAllAsync();
            return View(reports);
        }

        [HttpPost("{id}/Resolve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(string id)
        {
            await _reportService.ResolveAsync(id, CurrentUserId, CurrentUserName);
            TempData["SuccessMessage"] = "Report resolved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{id}/Dismiss")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dismiss(string id)
        {
            await _reportService.DismissAsync(id, CurrentUserId, CurrentUserName);
            TempData["SuccessMessage"] = "Report dismissed.";
            return RedirectToAction(nameof(Index));
        }
    }
}
