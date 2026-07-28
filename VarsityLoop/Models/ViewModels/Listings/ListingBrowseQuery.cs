using VarsityLoop.Models.Entities;

namespace VarsityLoop.Models.ViewModels.Listings
{
    public enum ListingSortOption
    {
        Newest,
        Oldest,
        PriceLowToHigh,
        PriceHighToLow
    }

    /// <summary>
    /// Parsed straight from the Browse page's query string. Filtering happens
    /// in the service layer over an in-memory snapshot of active listings
    /// (see ListingService.BrowseAsync) rather than as Firestore query
    /// clauses - at MVP catalogue size this avoids stacking up composite
    /// indexes for every filter combination, at the cost of not scaling to
    /// a very large catalogue without revisiting (see comment there).
    /// </summary>
    public class ListingBrowseQuery
    {
        public string? SearchTerm { get; set; }
        public string? CategoryId { get; set; }
        public double? MinPrice { get; set; }
        public double? MaxPrice { get; set; }
        public ListingCondition? Condition { get; set; }
        public ListingSortOption Sort { get; set; } = ListingSortOption.Newest;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}
