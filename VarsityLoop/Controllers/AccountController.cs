using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VarsityLoop.Models.ViewModels.Account;
using VarsityLoop.Repositories.Interfaces;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Controllers
{
    public class AccountController : Controller
    {
        private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/webp" };
        private const long MaxImageBytes = 2 * 1024 * 1024; // 2 MB

        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;
        private readonly IStorageService _storageService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAuthService authService, IUserRepository userRepository, IStorageService storageService, ILogger<AccountController> logger)
        {
            _authService = authService;
            _userRepository = userRepository;
            _storageService = storageService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");

            ViewData["Title"] = "Sign Up";
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ViewData["Title"] = "Sign Up";

            if (!ModelState.IsValid) return View(model);

            var result = await _authService.RegisterAsync(
                model.FirstName, model.LastName, model.Email, model.Password, model.University);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Registration failed.");
                return View(model);
            }

            return RedirectToAction(nameof(RegisterConfirmation));
        }

        [HttpGet]
        public IActionResult RegisterConfirmation()
        {
            ViewData["Title"] = "Check Your Email";
            return View();
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");

            ViewData["Title"] = "Log In";
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            ViewData["Title"] = "Log In";

            if (!ModelState.IsValid) return View(model);

            var result = await _authService.SignInAsync(model.Email, model.Password);

            if (!result.Success || result.User == null)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Incorrect email or password.");
                return View(model);
            }

            var user = result.User;

            await SignInWithCookieAsync(user, model.RememberMe);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            ViewData["Title"] = "Forgot Password";
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            ViewData["Title"] = "Forgot Password";

            if (!ModelState.IsValid) return View(model);

            await _authService.SendPasswordResetEmailAsync(model.Email);

            // Always redirect to the same confirmation page, whether or not the
            // email exists, so this can't be used to enumerate registered accounts.
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            ViewData["Title"] = "Check Your Email";
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            ViewData["Title"] = "My Profile";

            var currentUser = await _userRepository.GetByIdAsync(CurrentUserId!);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            return View(new ProfileViewModel
            {
                FirstName = currentUser.FirstName,
                LastName = currentUser.LastName,
                University = currentUser.University,
                Biography = currentUser.Biography,
                ProfilePictureUrl = currentUser.ProfilePictureUrl
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            ViewData["Title"] = "My Profile";

            if (model.ProfilePictureFile != null)
            {
                if (!AllowedImageTypes.Contains(model.ProfilePictureFile.ContentType) || model.ProfilePictureFile.Length > MaxImageBytes)
                {
                    ModelState.AddModelError(nameof(model.ProfilePictureFile), "Profile picture must be a JPG, PNG, or WEBP under 2MB.");
                }
            }

            if (!ModelState.IsValid) return View(model);

            var currentUser = await _userRepository.GetByIdAsync(CurrentUserId!);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            if (model.ProfilePictureFile != null)
            {
                await using var stream = model.ProfilePictureFile.OpenReadStream();
                model.ProfilePictureUrl = await _storageService.UploadPublicFileAsync(
                    stream, model.ProfilePictureFile.FileName, model.ProfilePictureFile.ContentType, $"profile-pictures/{currentUser.Id}");
            }

            currentUser.FirstName = model.FirstName.Trim();
            currentUser.LastName = model.LastName.Trim();
            currentUser.University = model.University.Trim();
            currentUser.Biography = model.Biography?.Trim();
            currentUser.ProfilePictureUrl = model.ProfilePictureUrl ?? currentUser.ProfilePictureUrl;

            await _userRepository.UpdateAsync(currentUser.Id, currentUser);

            // Name is baked into the cookie's claims at login - re-issue it now so a
            // name change shows up immediately instead of waiting for next login.
            await SignInWithCookieAsync(currentUser, isPersistent: User.Identity is ClaimsIdentity { IsAuthenticated: true });

            TempData["SuccessMessage"] = "Profile updated.";
            return RedirectToAction(nameof(Profile));
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private async Task SignInWithCookieAsync(Models.Entities.ApplicationUser user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Role, user.Role),
                new("university", user.University)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties
                {
                    IsPersistent = isPersistent,
                    ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(30) : null
                });
        }
    }
}
