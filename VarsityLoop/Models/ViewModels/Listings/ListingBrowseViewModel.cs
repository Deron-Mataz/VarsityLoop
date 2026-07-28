using VarsityLoop.Models.Entities;

namespace VarsityLoop.Models.ViewModels.Listings
{
    public class ListingBrowseViewModel
    {
        public ListingBrowseResult Result { get; set; } = new();
        public ListingBrowseQuery Query { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
    }
}
