namespace VarsityLoop.Models.Common
{
    /// <summary>
    /// Suggested "Type" values shown in the listing form's datalist per
    /// module. These are suggestions, not an enum - a seller can type
    /// anything, per the spec's "not limited to" wording. Centralized here
    /// so Phase 9 (Fashion) and Phase 10 (Study Supplies) add their own
    /// static list alongside this one rather than hardcoding options in a view.
    /// </summary>
    public static class ListingTypeSuggestions
    {
        public static readonly string[] Electronics =
        {
            "Phone", "Laptop", "Desktop Computer", "Tablet", "Monitor", "Gaming Console",
            "Printer", "Router", "Speakers", "Headphones", "Earbuds", "Smart Watch",
            "Fridge", "Bar Fridge", "Microwave", "Kettle", "Air Fryer", "Heater", "Fan", "Other"
        };

        public static readonly string[] Fashion =
        {
            "Shoes", "Jerseys", "Jackets", "Hoodies", "Dresses", "Watches",
            "Bags", "Jewellery", "Caps", "Sunglasses", "Other"
        };
    }
}
