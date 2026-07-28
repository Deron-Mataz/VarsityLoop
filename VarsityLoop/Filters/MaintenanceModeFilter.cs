using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VarsityLoop.Models.Entities;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Filters
{
    /// <summary>
    /// When Site Settings > Maintenance Mode is on, every request is shown the
    /// maintenance page instead of the normal site - except Admin/SuperAdmin
    /// users (who need to be able to turn it back off) and the controllers that
    /// make that possible (Account, so admins can still log in; Admin, so they
    /// can reach Settings; Error, so error pages still work).
    /// </summary>
    public class MaintenanceModeFilter : IAsyncActionFilter
    {
        private static readonly string[] ExemptControllers = { "Account", "Admin", "Error" };

        private readonly ISiteSettingsService _siteSettingsService;

        public MaintenanceModeFilter(ISiteSettingsService siteSettingsService)
        {
            _siteSettingsService = siteSettingsService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var isExemptController = controllerName != null && ExemptControllers.Contains(controllerName);
            var isAdmin = context.HttpContext.User.IsInRole(RoleNames.Admin)
                || context.HttpContext.User.IsInRole(RoleNames.SuperAdmin);

            if (!isExemptController && !isAdmin)
            {
                var settings = await _siteSettingsService.GetSettingsAsync();

                if (settings.MaintenanceMode)
                {
                    context.Result = new ViewResult { ViewName = "~/Views/Shared/Maintenance.cshtml" };
                    return;
                }
            }

            await next();
        }
    }
}
