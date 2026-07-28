using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Filters
{
    /// <summary>
    /// Runs on every action that returns a ViewResult and stamps the current
    /// SiteSettings onto its ViewData - SiteName, LogoUrl, FaviconUrl, colours,
    /// hero copy, footer text, social links. _Layout.cshtml (and Home/Index for
    /// the hero section) read these keys, so branding changes made in the Admin
    /// Panel show up everywhere immediately without every controller having to
    /// remember to inject them.
    /// </summary>
    public class SiteSettingsResultFilter : IAsyncResultFilter
    {
        private readonly ISiteSettingsService _siteSettingsService;

        public SiteSettingsResultFilter(ISiteSettingsService siteSettingsService)
        {
            _siteSettingsService = siteSettingsService;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context.Result is ViewResult viewResult)
            {
                var settings = await _siteSettingsService.GetSettingsAsync();

                viewResult.ViewData["SiteName"] = settings.SiteName;
                viewResult.ViewData["LogoUrl"] = settings.LogoUrl;
                viewResult.ViewData["FaviconUrl"] = settings.FaviconUrl;
                viewResult.ViewData["PrimaryColour"] = settings.PrimaryColour;
                viewResult.ViewData["AccentColour"] = settings.AccentColour;
                viewResult.ViewData["HeroHeading"] = settings.HeroHeading;
                viewResult.ViewData["HeroDescription"] = settings.HeroDescription;
                viewResult.ViewData["FooterText"] = settings.FooterText;
                viewResult.ViewData["SupportEmail"] = settings.SupportEmail;
                viewResult.ViewData["SupportPhone"] = settings.SupportPhone;
                viewResult.ViewData["FacebookLink"] = settings.FacebookLink;
                viewResult.ViewData["InstagramLink"] = settings.InstagramLink;
                viewResult.ViewData["LinkedInLink"] = settings.LinkedInLink;
                viewResult.ViewData["TwitterLink"] = settings.TwitterLink;
            }

            await next();
        }
    }
}
