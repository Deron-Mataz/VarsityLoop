using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace VarsityLoop.Models.ViewModels.Admin
{
    public class SiteSettingsViewModel
    {
        [Required, StringLength(100)]
        [Display(Name = "Website Name")]
        public string SiteName { get; set; } = string.Empty;

        public string? LogoUrl { get; set; }
        public string? FaviconUrl { get; set; }

        [Display(Name = "New Logo")]
        public IFormFile? LogoFile { get; set; }

        [Display(Name = "New Favicon")]
        public IFormFile? FaviconFile { get; set; }

        [Required, RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Enter a hex colour, e.g. #1d4ed8")]
        [Display(Name = "Primary Colour")]
        public string PrimaryColour { get; set; } = "#1d4ed8";

        [Required, RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Enter a hex colour, e.g. #d4a017")]
        [Display(Name = "Accent Colour")]
        public string AccentColour { get; set; } = "#d4a017";

        [Required, StringLength(150)]
        [Display(Name = "Homepage Hero Heading")]
        public string HeroHeading { get; set; } = string.Empty;

        [Required, StringLength(300)]
        [Display(Name = "Homepage Hero Description")]
        public string HeroDescription { get; set; } = string.Empty;

        public string? HeroImageUrl { get; set; }

        [Display(Name = "Hero Background Image")]
        public IFormFile? HeroImageFile { get; set; }

        [Required, StringLength(200)]
        [Display(Name = "Footer Text")]
        public string FooterText { get; set; } = string.Empty;

        [EmailAddress]
        [Display(Name = "Support Email")]
        public string? SupportEmail { get; set; }

        [Display(Name = "Support Phone")]
        public string? SupportPhone { get; set; }

        [Display(Name = "Facebook Link")]
        [Url]
        public string? FacebookLink { get; set; }

        [Display(Name = "Instagram Link")]
        [Url]
        public string? InstagramLink { get; set; }

        [Display(Name = "LinkedIn Link")]
        [Url]
        public string? LinkedInLink { get; set; }

        [Display(Name = "Twitter/X Link")]
        [Url]
        public string? TwitterLink { get; set; }

        [Display(Name = "Maintenance Mode")]
        public bool MaintenanceMode { get; set; }
    }
}
