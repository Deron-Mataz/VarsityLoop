using VarsityLoop.Models.Entities;

namespace VarsityLoop.Services.Interfaces
{
    /// <summary>
    /// Reads/writes the single SiteSettings document. Reads are cached (this
    /// gets called on every single page request via SiteSettingsResultFilter,
    /// so hitting Firestore every time would be wasteful); writes invalidate
    /// the cache immediately so admin changes reflect on the public site
    /// without any delay, per spec.
    /// </summary>
    public interface ISiteSettingsService
    {
        Task<SiteSettings> GetSettingsAsync();
        Task UpdateSettingsAsync(SiteSettings settings);
    }
}
