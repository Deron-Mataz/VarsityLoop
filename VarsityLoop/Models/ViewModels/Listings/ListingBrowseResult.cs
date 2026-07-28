using VarsityLoop.Models.Entities;

namespace VarsityLoop.Models.ViewModels.Listings
{
    public class ListingBrowseResult
    {
        public List<Listing> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
