using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VarsityLoop.Models.Entities;

namespace VarsityLoop.Controllers
{
    /// <summary>
    /// Every action under /Admin requires the Admin or SuperAdmin role, read live
    /// from the signed-in user's cookie claims (populated at login time from
    /// Firestore - see AccountController.Login). The full CMS dashboard, user
    /// management, listing moderation, etc. are built out in Phase 6; this
    /// controller exists now to prove the RBAC gate itself works end-to-end.
    /// </summary>
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.SuperAdmin}")]
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Admin Dashboard";
            return View();
        }
    }
}
