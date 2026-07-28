namespace VarsityLoop.Services.Interfaces
{
    /// <summary>
    /// Abstraction over Firebase Storage. Kept generic (folder + stream in,
    /// public URL out) so it's reused as-is for listing photos, profile
    /// pictures, and any future media - not just branding assets.
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Uploads a file to the given folder (e.g. "branding", "listings/{id}",
        /// "profile-pictures") under a generated unique name, and returns its
        /// public URL.
        /// </summary>
        Task<string> UploadPublicFileAsync(Stream fileStream, string originalFileName, string contentType, string folder);
    }
}
