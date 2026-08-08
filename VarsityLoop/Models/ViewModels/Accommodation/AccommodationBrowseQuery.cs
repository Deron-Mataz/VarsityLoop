using VarsityLoop.Models.Entities;

namespace VarsityLoop.Models.ViewModels.Accommodation
{
    public class AccommodationBrowseQuery
    {
        public string? University { get; set; }
        public ResidenceClassification? Classification { get; set; }
        public AccommodationType? AccommodationType { get; set; }
        public double? MaxRent { get; set; }
        public GenderPreference? GenderPreference { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }

    public class AccommodationBrowseResult
    {
        public List<Entities.Accommodation> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
