using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using VarsityLoop.Models.ViewModels.Account;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAuthService authService, ILogger<AccountController> logger)
        {
            _authService = authService;
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
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null
                });

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
    }
}
