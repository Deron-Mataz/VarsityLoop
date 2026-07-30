using VarsityLoop.Models.Common;
using VarsityLoop.Models.Entities;

namespace VarsityLoop.Services.Interfaces
{
    /// <summary>
    /// Result of a sign-in attempt: on success, carries the Firebase ID token
    /// (used once to verify the caller server-side) and the matching Firestore
    /// user profile that the cookie's claims are built from.
    /// </summary>
    public class SignInResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public bool EmailNotVerified { get; set; }
        public ApplicationUser? User { get; set; }
    }

    /// <summary>
    /// Wraps Firebase Authentication (REST, for sign-up/sign-in/password-reset)
    /// and Firebase Admin SDK (for server-side ID token verification) behind a
    /// single service, so controllers never talk to Firebase directly.
    /// </summary>
    public interface IAuthService
    {
        Task<OperationResult> RegisterAsync(string firstName, string lastName, string email, string password, string university, string avatarUrl);

        Task<SignInResult> SignInAsync(string email, string password);

        Task<OperationResult> SendPasswordResetEmailAsync(string email);

        Task<OperationResult> ResendVerificationEmailAsync(string email, string password);
    }
}
