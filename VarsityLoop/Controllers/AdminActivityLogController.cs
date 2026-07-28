using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VarsityLoop.Models.Entities;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Controllers
{
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.SuperAdmin}")]
    [Route("Admin/ActivityLog")]
    public class AdminActivityLogController : Controller
    {
        private readonly IActivityLogService _activityLogService;

        public AdminActivityLogController(IActivityLogService activityLogService)
        {
            _activityLogService = activityLogService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Activity Log";
            var logs = await _activityLogService.GetRecentAsync();
            return View(logs);
        }
    }
}
