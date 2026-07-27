using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using VarsityLoop.Configuration;
using VarsityLoop.Models.Common;
using VarsityLoop.Models.Entities;
using VarsityLoop.Repositories.Interfaces;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Services.Implementations
{
    /// <summary>
    /// Talks to Firebase Authentication directly via Google's Identity Toolkit
    /// REST API (sign-up, sign-in, password reset, email verification) rather
    /// than a third-party wrapper library, so behaviour doesn't depend on the
    /// exact method signatures of whichever NuGet package version is installed.
    /// The Identity Toolkit REST surface is a stable, documented Google API:
    /// https://firebase.google.com/docs/reference/rest/auth
    ///
    /// Every successful call also mirrors the resulting identity into the
    /// "Users" Firestore collection, which is the single source of truth for
    /// profile data and role. Nothing about role assignment happens here -
    /// new users always land with the default "User" role (except the one
    /// bootstrap SuperAdmin, see RegisterAsync); all further role changes
    /// only ever happen from the Admin Panel (Phase 6).
    /// </summary>
    public class AuthService : IAuthService
    {
        private const string IdentityToolkitBaseUrl = "https://identitytoolkit.googleapis.com/v1/";

        private static readonly HttpClient HttpClient = new();

        private readonly IUserRepository _userRepository;
        private readonly FirebaseOptions _firebaseOptions;
        private readonly AppSettingsOptions _appSettingsOptions;

        public AuthService(
            IOptions<FirebaseOptions> firebaseOptions,
            IOptions<AppSettingsOptions> appSettingsOptions,
            IUserRepository userRepository)
        {
            _firebaseOptions = firebaseOptions.Value;
            _appSettingsOptions = appSettingsOptions.Value;
            _userRepository = userRepository;
        }

        public async Task<OperationResult> RegisterAsync(string firstName, string lastName, string email, string password, string university)
        {
            email = email.Trim().ToLowerInvariant();

            var existing = await _userRepository.GetByEmailAsync(email);
            if (existing != null)
            {
                return OperationResult.Fail("An account with this email already exists.");
            }

            var signUp = await PostAsync<SignUpResponse>("accounts:signUp", new
            {
                email,
                password,
                returnSecureToken = true
            });

            if (!signUp.Success || signUp.Data == null)
            {
                return OperationResult.Fail(TranslateFirebaseError(signUp.ErrorCode));
            }

            // Fire the verification email using the ID token we just received -
            // no separate sign-in needed, the sign-up response already authenticates us.
            await PostAsync<object>("accounts:sendOobCode", new
            {
                requestType = "VERIFY_EMAIL",
                idToken = signUp.Data.IdToken
            });

            // One-time bootstrap: the email configured under AppSettings:DefaultAdminEmail
            // becomes SuperAdmin the moment it registers, so there's always a way into the
            // Admin Panel on a fresh database. Every other registration is a normal User -
            // all subsequent role changes happen exclusively from the Admin Panel (Phase 6).
            var isBootstrapAdmin = !string.IsNullOrWhiteSpace(_appSettingsOptions.DefaultAdminEmail)
                && string.Equals(_appSettingsOptions.DefaultAdminEmail.Trim(), email, StringComparison.OrdinalIgnoreCase);

            var profile = new ApplicationUser
            {
                Id = signUp.Data.LocalId,
                FirebaseUid = signUp.Data.LocalId,
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                Email = email,
                University = university.Trim(),
                Role = isBootstrapAdmin ? RoleNames.SuperAdmin : RoleNames.User,
                AccountStatus = Models.Entities.AccountStatus.Active.ToString(),
                EmailVerified = false
            };

            await _userRepository.AddAsync(profile);

            return OperationResult.Ok();
        }

        public async Task<SignInResult> SignInAsync(string email, string password)
        {
            email = email.Trim().ToLowerInvariant();

            var signIn = await PostAsync<SignInResponse>("accounts:signInWithPassword", new
            {
                email,
                password,
                returnSecureToken = true
            });

            if (!signIn.Success || signIn.Data == null)
            {
                return new SignInResult { Success = false, ErrorMessage = TranslateFirebaseError(signIn.ErrorCode) };
            }

            var lookup = await PostAsync<LookupResponse>("accounts:lookup", new
            {
                idToken = signIn.Data.IdToken
            });

            var isVerified = lookup.Success
                && lookup.Data?.Users is { Count: > 0 }
                && lookup.Data.Users[0].EmailVerified;

            if (_firebaseOptions.RequireEmailVerification && !isVerified)
            {
                // The password was already confirmed correct above - use the ID token
                // we already have to resend the verification email immediately, rather
                // than asking the user to log in again just to trigger a resend.
                await PostAsync<object>("accounts:sendOobCode", new
                {
                    requestType = "VERIFY_EMAIL",
                    idToken = signIn.Data.IdToken
                });

                return new SignInResult
                {
                    Success = false,
                    EmailNotVerified = true,
                    ErrorMessage = "Please verify your email address before logging in. We've sent a new verification link to your inbox."
                };
            }

            var profile = await _userRepository.GetByFirebaseUidAsync(signIn.Data.LocalId);
            if (profile == null)
            {
                return new SignInResult
                {
                    Success = false,
                    ErrorMessage = "We couldn't find your profile. Please contact support."
                };
            }

            if (profile.AccountStatus !=  Models.Entities.AccountStatus.Active.ToString())
            {
                return new SignInResult
                {
                    Success = false,
                    ErrorMessage = "This account has been deactivated. Please contact support."
                };
            }

            var fieldsToUpdate = new Dictionary<string, object?>
            {
                { "lastLoginAt", Google.Cloud.Firestore.Timestamp.GetCurrentTimestamp() }
            };

            if (!profile.EmailVerified && isVerified)
            {
                fieldsToUpdate["emailVerified"] = true;
                profile.EmailVerified = true;
            }

            await _userRepository.UpdateFieldsAsync(profile.Id, fieldsToUpdate);

            return new SignInResult { Success = true, User = profile };
        }

        public async Task<OperationResult> SendPasswordResetEmailAsync(string email)
        {
            // Deliberately ignore the result either way - returning success regardless
            // of whether the email exists prevents leaking which emails are registered
            // (standard practice for "forgot password" flows).
            await PostAsync<object>("accounts:sendOobCode", new
            {
                requestType = "PASSWORD_RESET",
                email = email.Trim().ToLowerInvariant()
            });

            return OperationResult.Ok();
        }

        public async Task<OperationResult> ResendVerificationEmailAsync(string email, string password)
        {
            var signIn = await PostAsync<SignInResponse>("accounts:signInWithPassword", new
            {
                email = email.Trim().ToLowerInvariant(),
                password,
                returnSecureToken = true
            });

            if (!signIn.Success || signIn.Data == null)
            {
                return OperationResult.Fail(TranslateFirebaseError(signIn.ErrorCode));
            }

            await PostAsync<object>("accounts:sendOobCode", new
            {
                requestType = "VERIFY_EMAIL",
                idToken = signIn.Data.IdToken
            });

            return OperationResult.Ok();
        }

        /// <summary>
        /// Posts to the Identity Toolkit REST API and normalizes both success and
        /// error responses into one shape, so callers never touch HttpResponseMessage
        /// or raw JSON directly.
        /// </summary>
        private async Task<RestResult<T>> PostAsync<T>(string endpoint, object body)
        {
            var url = $"{IdentityToolkitBaseUrl}{endpoint}?key={_firebaseOptions.ApiKey}";
            var response = await HttpClient.PostAsJsonAsync(url, body);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>();
                return new RestResult<T> { Success = true, Data = data };
            }

            string? errorCode = null;
            try
            {
                var errorBody = await response.Content.ReadFromJsonAsync<GoogleErrorEnvelope>();
                errorCode = errorBody?.Error?.Message;
            }
            catch (JsonException)
            {
                // Fall through with a null error code - TranslateFirebaseError handles that.
            }

            return new RestResult<T> { Success = false, ErrorCode = errorCode };
        }

        private static string TranslateFirebaseError(string? errorCode)
        {
            // Identity Toolkit returns a short machine code as error.message
            // (sometimes with a suffix like "WEAK_PASSWORD : Password should be
            // at least 6 characters"), so match on the prefix.
            var code = errorCode?.Split(':')[0].Trim() ?? string.Empty;

            return code switch
            {
                "EMAIL_EXISTS" => "An account with this email already exists.",
                "EMAIL_NOT_FOUND" => "Incorrect email or password.",
                "INVALID_PASSWORD" => "Incorrect email or password.",
                "INVALID_LOGIN_CREDENTIALS" => "Incorrect email or password.",
                "USER_DISABLED" => "This account has been deactivated. Please contact support.",
                "WEAK_PASSWORD" => "Password is too weak. Please choose a stronger password.",
                "TOO_MANY_ATTEMPTS_TRY_LATER" => "Too many attempts. Please wait a few minutes and try again.",
                "INVALID_EMAIL" => "Please enter a valid email address.",
                "MISSING_PASSWORD" => "Please enter a password.",
                _ => "Something went wrong. Please try again."
            };
        }

        private class RestResult<T>
        {
            public bool Success { get; set; }
            public T? Data { get; set; }
            public string? ErrorCode { get; set; }
        }

        private class SignUpResponse
        {
            [JsonPropertyName("idToken")]
            public string IdToken { get; set; } = string.Empty;

            [JsonPropertyName("localId")]
            public string LocalId { get; set; } = string.Empty;
        }

        private class SignInResponse
        {
            [JsonPropertyName("idToken")]
            public string IdToken { get; set; } = string.Empty;

            [JsonPropertyName("localId")]
            public string LocalId { get; set; } = string.Empty;
        }

        private class LookupResponse
        {
            [JsonPropertyName("users")]
            public List<LookupUser> Users { get; set; } = new();
        }

        private class LookupUser
        {
            [JsonPropertyName("emailVerified")]
            public bool EmailVerified { get; set; }
        }

        private class GoogleErrorEnvelope
        {
            [JsonPropertyName("error")]
            public GoogleError? Error { get; set; }
        }

        private class GoogleError
        {
            [JsonPropertyName("message")]
            public string? Message { get; set; }
        }
    }
}
