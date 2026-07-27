namespace VarsityLoop.Configuration
{
    /// <summary>
    /// Strongly-typed binding for the "Firebase" section of appsettings.json.
    /// Populated via IOptions&lt;FirebaseOptions&gt; - never hardcode these values.
    /// </summary>
    public class FirebaseOptions
    {
        public const string SectionName = "Firebase";

        public string ProjectId { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string AuthDomain { get; set; } = string.Empty;
        public string StorageBucket { get; set; } = string.Empty;

        /// <summary>
        /// NOT bound from appsettings.json. The raw service account JSON contents live in
        /// the Secret Manager under "Firebase:ServiceAccountJson" and are read directly
        /// from IConfiguration in ServiceCollectionExtensions - this property exists only
        /// so the shape of the section is documented in one place.
        /// </summary>
        public string ServiceAccountJson { get; set; } = string.Empty;

        public int SessionCookieExpiryDays { get; set; } = 5;
        public bool RequireEmailVerification { get; set; } = true;
    }

    public class AppSettingsOptions
    {
        public const string SectionName = "AppSettings";

        public string DefaultAdminEmail { get; set; } = string.Empty;
    }
}
