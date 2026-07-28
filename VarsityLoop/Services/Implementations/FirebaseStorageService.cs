using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;
using VarsityLoop.Configuration;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Services.Implementations
{
    /// <summary>
    /// Uploads files to the Firebase Storage bucket configured under
    /// Firebase:StorageBucket, reusing the same service account credential
    /// already resolved for Firestore/Admin SDK (see ServiceCollectionExtensions).
    ///
    /// NOTE: this uploads the object but does not set a per-object ACL, because
    /// Firebase Storage buckets created after late 2020 default to Uniform
    /// Bucket-Level Access, which rejects per-object ACL calls outright. For
    /// uploaded files to be publicly viewable (logos, favicons, listing photos),
    /// make sure your bucket's Firebase Storage security rules allow public
    /// read on the relevant folders, e.g.:
    ///   match /branding/{allPaths=**} { allow read: if true; allow write: if false; }
    /// (write access is always via this server-side service account, never
    /// directly from the browser, so "write: if false" is intentional.)
    /// </summary>
    public class FirebaseStorageService : IStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;

        public FirebaseStorageService(GoogleCredential credential, IOptions<FirebaseOptions> firebaseOptions)
        {
            _storageClient = StorageClient.Create(credential);
            _bucketName = firebaseOptions.Value.StorageBucket;
        }

        public async Task<string> UploadPublicFileAsync(Stream fileStream, string originalFileName, string contentType, string folder)
        {
            var objectName = $"{folder.Trim('/')}/{Guid.NewGuid():N}_{SanitizeFileName(originalFileName)}";

            await _storageClient.UploadObjectAsync(_bucketName, objectName, contentType, fileStream);

            return $"https://storage.googleapis.com/{_bucketName}/{objectName}";
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var cleaned = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "file" : cleaned.Replace(' ', '-');
        }
    }
}
