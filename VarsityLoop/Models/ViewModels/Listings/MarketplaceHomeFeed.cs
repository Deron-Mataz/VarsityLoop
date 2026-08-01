using VarsityLoop.Models.Entities;

namespace VarsityLoop.Models.ViewModels.Listings
{
    /// <summary>
    /// The "All" module's home feed: curated sections rather than one long
    /// mixed list, per the Marketplace browsing spec.
    /// </summary>
    public class MarketplaceHomeFeed
    {
        public List<Listing> Featured { get; set; } = new();
        public List<Listing> RecentBooks { get; set; } = new();
        public List<Listing> TrendingElectronics { get; set; } = new();
        public List<Listing> LatestFashion { get; set; } = new();
        public List<Listing> PopularAccessories { get; set; } = new();
        public List<Listing> StudySupplies { get; set; } = new();
    }
}
