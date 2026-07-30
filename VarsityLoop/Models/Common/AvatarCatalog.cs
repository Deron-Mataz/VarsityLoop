namespace VarsityLoop.Models.Common
{
    /// <summary>
    /// The 6 preset avatars offered at registration and on the profile page.
    /// Files live at wwwroot/images/avatars/avatar{1-6}.svg. Centralized here
    /// so the count/path pattern isn't duplicated across controllers and views.
    /// </summary>
    public static class AvatarCatalog
    {
        public const int Count = 6;

        public static string Url(int number) => $"/images/avatars/avatar{number}.svg";
    }
}
