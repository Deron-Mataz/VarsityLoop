namespace VarsityLoop.Models.Common
{
    /// <summary>
    /// At least 3 Bootstrap Icons suggested per Module for the category icon
    /// picker (see Admin > Categories > Create/Edit). Admins pick one
    /// manually - nothing here is auto-assigned. Filtered client-side by the
    /// selected Module using the same toggle pattern as the listing form's
    /// per-module field groups.
    /// </summary>
    public static class ModuleIconOptions
    {
        public static readonly (string Module, string[] Icons)[] All =
        {
            ("Books", new[] { "bi-book", "bi-book-half", "bi-journal-bookmark" }),
            ("Electronics", new[] { "bi-laptop", "bi-phone", "bi-tv" }),
            ("Fashion", new[] { "bi-bag", "bi-gem", "bi-watch" }),
            ("Accessories", new[] { "bi-sunglasses", "bi-watch", "bi-backpack2" }),
            ("StudySupplies", new[] { "bi-calculator", "bi-pencil", "bi-rulers" })
        };
    }
}
