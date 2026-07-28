using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VarsityLoop.Models.Entities;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Controllers
{
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.SuperAdmin}")]
    [Route("Admin/Users")]
    public class AdminUsersController : Controller
    {
        private readonly IAdminUserService _adminUserService;

        public AdminUsersController(IAdminUserService adminUserService)
        {
            _adminUserService = adminUserService;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        private string CurrentUserName => User.Identity?.Name ?? "Unknown";
        private bool CurrentUserIsSuperAdmin => User.IsInRole(RoleNames.SuperAdmin);

        [HttpGet("")]
        public async Task<IActionResult> Index(string? q)
        {
            ViewData["Title"] = "Users";
            ViewData["SearchTerm"] = q;
            ViewData["CurrentUserIsSuperAdmin"] = CurrentUserIsSuperAdmin;

            var users = string.IsNullOrWhiteSpace(q)
                ? await _adminUserService.GetAllAsync()
                : await _adminUserService.SearchAsync(q);

            return View(users.OrderByDescending(u => u.CreatedAt).ToList());
        }

        [HttpPost("{id}/SetRole")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetRole(string id, string role)
        {
            var result = await _adminUserService.SetRoleAsync(id, role, CurrentUserId, CurrentUserName, CurrentUserIsSuperAdmin);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success ? "Role updated." : result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{id}/Deactivate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string id)
        {
            var result = await _adminUserService.SetAccountStatusAsync(id, AccountStatus.Deactivated, CurrentUserId, CurrentUserName);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success ? "Account deactivated." : result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{id}/Reactivate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(string id)
        {
            var result = await _adminUserService.SetAccountStatusAsync(id, AccountStatus.Active, CurrentUserId, CurrentUserName);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success ? "Account reactivated." : result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{id}/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _adminUserService.DeleteAsync(id, CurrentUserId, CurrentUserName);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success ? "Account deleted." : result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }
    }
}
