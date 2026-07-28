using Microsoft.Extensions.Caching.Memory;
using VarsityLoop.Models.Entities;
using VarsityLoop.Repositories.Interfaces;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Services.Implementations
{
    public class SiteSettingsService : ISiteSettingsService
    {
        private const string CacheKey = "site-settings:global";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        private readonly IFirestoreRepository<SiteSettings> _repository;
        private readonly IMemoryCache _cache;

        public SiteSettingsService(IFirestoreRepository<SiteSettings> repository, IMemoryCache cache)
        {
            _repository = repository;
            _cache = cache;
        }

        public async Task<SiteSettings> GetSettingsAsync()
        {
            if (_cache.TryGetValue(CacheKey, out SiteSettings? cached) && cached != null)
            {
                return cached;
            }

            var settings = await _repository.GetByIdAsync(SiteSettings.DocumentId);

            if (settings == null)
            {
                // First run on a fresh database - create the document with sensible
                // defaults so the CMS always has something to edit rather than the
                // public site depending on a null check everywhere.
                settings = new SiteSettings { Id = SiteSettings.DocumentId };
                await _repository.AddAsync(settings);
            }

            _cache.Set(CacheKey, settings, CacheDuration);
            return settings;
        }

        public async Task UpdateSettingsAsync(SiteSettings settings)
        {
            settings.Id = SiteSettings.DocumentId;
            await _repository.UpdateAsync(SiteSettings.DocumentId, settings);

            // Invalidate immediately - branding changes must reflect on the public
            // site right away, not after the cache happens to expire.
            _cache.Remove(CacheKey);
        }
    }
}
