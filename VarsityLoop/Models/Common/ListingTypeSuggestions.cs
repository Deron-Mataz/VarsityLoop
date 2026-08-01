namespace VarsityLoop.Models.Common
{
    /// <summary>
    /// Suggested "Type" values shown in the listing form's datalist per
    /// module. These are suggestions, not an enum - a seller can type
    /// anything, per the spec's "not limited to" wording.
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
            "Hoodie", "T-Shirt", "Jacket", "Jersey", "Dress", "Pants", "Shorts", "Other"
        };

        public static readonly string[] Accessories =
        {
            "Sunglasses", "Phone Case", "Watch", "Handbag", "Wallet", "Belt", "Jewellery", "Other"
        };

        public static readonly string[] StudySupplies =
        {
            "Scientific Calculator", "Geometry Set", "Lab Coat", "Notebook", "File",
            "Lever Arch File", "Pen", "Pencil", "Highlighter", "Whiteboard", "Whiteboard Marker",
            "Art Supplies", "Engineering Equipment", "Printing Paper", "USB Flash Drive", "Other"
        };
    }
}
