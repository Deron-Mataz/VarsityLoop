namespace VarsityLoop.Models.Entities
{
    /// <summary>
    /// Defines the set of roles the platform understands. The enum itself is fixed
    /// (C# requires a closed type for policy-based authorization), but which role
    /// a given user HOLDS is entirely data-driven - stored on the user's Firestore
    /// document and editable at runtime from the Admin Panel. Nothing in the code
    /// hardcodes "this user is an admin".
    /// </summary>
    public enum UserRole
    {
        User = 0,
        Moderator = 1,
        Admin = 2,
        SuperAdmin = 3
    }

    public enum AccountStatus
    {
        Active = 0,
        Deactivated = 1,
        Suspended = 2
    }

    public static class RoleNames
    {
        public const string User = nameof(UserRole.User);
        public const string Moderator = nameof(UserRole.Moderator);
        public const string Admin = nameof(UserRole.Admin);
        public const string SuperAdmin = nameof(UserRole.SuperAdmin);

        /// <summary>Roles permitted to access /Admin/*</summary>
        public static readonly string[] AdminPanelRoles = { Admin, SuperAdmin };

        /// <summary>Roles permitted to moderate listings/reports without full admin access.</summary>
        public static readonly string[] ModerationRoles = { Moderator, Admin, SuperAdmin };
    }
}
