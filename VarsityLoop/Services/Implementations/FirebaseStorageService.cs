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
    /// Uploads use the raw GCS client (no per-object ACL is set - Firebase
    /// Storage buckets default to Uniform Bucket-Level Access, which rejects
    /// per-object ACL calls outright). Returned URLs instead point at Firebase's
    /// own serving endpoint (firebasestorage.googleapis.com), NOT the raw GCS
    /// endpoint (storage.googleapis.com) - only the Firebase endpoint respects
    /// Firebase Storage Security Rules. The raw GCS endpoint only respects
    /// bucket-level IAM, which is why a rules-based "allow read: if true" alone
    /// does not make a storage.googleapis.com URL public.
    ///
    /// Make sure your bucket's Firebase Storage security rules allow public
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

            // Firebase's serving endpoint requires the full object path (including
            // the "/" separators) to be percent-encoded as a single path segment.
            var encodedObjectName = Uri.EscapeDataString(objectName);

            return $"https://firebasestorage.googleapis.com/v0/b/{_bucketName}/o/{encodedObjectName}?alt=media";
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var cleaned = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "file" : cleaned.Replace(' ', '-');
        }
    }
}
