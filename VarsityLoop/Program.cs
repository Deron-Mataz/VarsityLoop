using Microsoft.AspNetCore.Authentication.Cookies;
using VarsityLoop.Configuration;
using VarsityLoop.Extensions;
using VarsityLoop.Filters;
using VarsityLoop.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------

builder.Services.AddControllersWithViews(options =>
{
    // CSRF protection is applied explicitly per state-changing action via
    // [ValidateAntiForgeryToken] (see AccountController), paired with the
    // asp-antiforgery tag helper that forms include by default.

    // Global filters - run on every request, every controller:
    //  - SiteSettingsResultFilter stamps live branding onto ViewData before
    //    any view renders, so _Layout.cshtml always reflects the CMS.
    //  - MaintenanceModeFilter short-circuits to the maintenance page for
    //    non-admin visitors when Site Settings > Maintenance Mode is on.
    options.Filters.Add<SiteSettingsResultFilter>();
    options.Filters.Add<MaintenanceModeFilter>();
});

// Firebase Admin SDK + Firestore client (throws a clear error at startup
// if appsettings.json / service account key haven't been configured yet).
builder.Services.AddFirebaseServices(builder.Configuration);

// Generic repository layer.
builder.Services.AddRepositories();

// Firebase-backed auth service (register/login/password reset).
builder.Services.AddAuthServices();

// Site Settings CMS (cached reads/writes) + Firebase Storage for media uploads.
builder.Services.AddCmsServices();

// Listings core (Phase 4 - Books MVP).
builder.Services.AddListingServices();

// Admin Panel (Phase 6): activity logging, reports, admin user management.
builder.Services.AddAdminServices();

// Cookie-based session authentication. The cookie itself only ever carries
// claims (uid, email, name, role) written at sign-in time from the Firestore
// user profile - Firebase's own tokens are never stored in the cookie.
var sessionExpiryDays = builder.Configuration.GetValue<int?>($"{FirebaseOptions.SectionName}:SessionCookieExpiryDays") ?? 5;

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Error/Forbidden";
        options.ExpireTimeSpan = TimeSpan.FromDays(sessionExpiryDays);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

var app = builder.Build();

// ---------------------------------------------------------------------
// Middleware pipeline
// ---------------------------------------------------------------------

if (!app.Environment.IsDevelopment())
{
    app.UseVarsityLoopExceptionHandling();
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
