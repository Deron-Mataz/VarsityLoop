using Google.Cloud.Firestore;
using VarsityLoop.Models.Common;

namespace VarsityLoop.Models.Entities
{
    /// <summary>
    /// Stored as a single document (Id = "global") in the "SiteSettings" collection.
    /// Everything here is editable from the Admin Panel and read on every page
    /// request via SiteSettingsResultFilter - nothing in this file is ever
    /// hardcoded into a view. Logo/favicon are Firebase Storage URLs, not files
    /// shipped with the app.
    /// </summary>
    [FirestoreData]
    public class SiteSettings : BaseEntity
    {
        public const string DocumentId = "global";

        [FirestoreProperty("siteName")]
        public string SiteName { get; set; } = "Varsity Loop";

        [FirestoreProperty("logoUrl")]
        public string? LogoUrl { get; set; }

        [FirestoreProperty("faviconUrl")]
        public string? FaviconUrl { get; set; }

        [FirestoreProperty("primaryColour")]
        public string PrimaryColour { get; set; } = "#1d4ed8";

        [FirestoreProperty("accentColour")]
        public string AccentColour { get; set; } = "#d4a017";

        [FirestoreProperty("heroHeading")]
        public string HeroHeading { get; set; } = "Buy, sell, and swap with students at your university";

        [FirestoreProperty("heroDescription")]
        public string HeroDescription { get; set; } = "Textbooks first, everything student life needs next.";

        [FirestoreProperty("footerText")]
        public string FooterText { get; set; } = $"© {DateTime.UtcNow.Year} Varsity Loop. All rights reserved.";

        [FirestoreProperty("supportEmail")]
        public string? SupportEmail { get; set; }

        [FirestoreProperty("supportPhone")]
        public string? SupportPhone { get; set; }

        [FirestoreProperty("facebookLink")]
        public string? FacebookLink { get; set; }

        [FirestoreProperty("instagramLink")]
        public string? InstagramLink { get; set; }

        [FirestoreProperty("linkedInLink")]
        public string? LinkedInLink { get; set; }

        [FirestoreProperty("twitterLink")]
        public string? TwitterLink { get; set; }

        [FirestoreProperty("maintenanceMode")]
        public bool MaintenanceMode { get; set; } = false;
    }
}
