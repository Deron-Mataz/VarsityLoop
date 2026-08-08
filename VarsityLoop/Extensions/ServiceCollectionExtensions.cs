using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using VarsityLoop.Configuration;
using VarsityLoop.Models.Entities;
using VarsityLoop.Repositories.Implementations;
using VarsityLoop.Repositories.Interfaces;
using VarsityLoop.Services.Implementations;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Wires up Firebase Admin SDK + Firestore client as singletons.
        /// Called once from Program.cs. Keeps all Firebase bootstrapping in one place
        /// so Program.cs stays readable.
        /// </summary>
        public static IServiceCollection AddFirebaseServices(this IServiceCollection services, IConfiguration configuration)
        {
            var firebaseOptions = configuration.GetSection(FirebaseOptions.SectionName).Get<FirebaseOptions>()
                ?? throw new InvalidOperationException("Firebase configuration section is missing. Copy appsettings.example.json to appsettings.json and fill in your Firebase credentials.");

            services.Configure<FirebaseOptions>(configuration.GetSection(FirebaseOptions.SectionName));
            services.Configure<AppSettingsOptions>(configuration.GetSection(AppSettingsOptions.SectionName));

            // Initialize the Firebase Admin App exactly once (used for verifying ID tokens,
            // managing users, and minting custom tokens/session cookies).
            if (FirebaseApp.DefaultInstance == null)
            {
                var credential = ResolveServiceAccountCredential(configuration);

                FirebaseApp.Create(new AppOptions
                {
                    Credential = credential,
                    ProjectId = firebaseOptions.ProjectId
                });

                // FirestoreDb also needs the same credential for server-side (Admin) access.
                var firestoreDb = new FirestoreDbBuilder
                {
                    ProjectId = firebaseOptions.ProjectId,
                    Credential = credential
                }.Build();

                services.AddSingleton(firestoreDb);

                // Registered so other Google Cloud clients (Firebase Storage - see
                // FirebaseStorageService) can reuse the same resolved credential
                // instead of re-reading configuration themselves.
                services.AddSingleton(credential);
            }

            return services;
        }

        /// <summary>
        /// Resolves the Firebase service account credential from configuration rather than
        /// a JSON file on disk. In Development this comes from the .NET Secret Manager
        /// (`dotnet user-secrets`), which is stored outside the repo under your user
        /// profile - see README for the exact `dotnet user-secrets set` command. In
        /// other environments the same "Firebase:ServiceAccountJson" key can instead be
        /// supplied via environment variables, Azure Key Vault, AWS Secrets Manager, or
        /// any other IConfiguration provider - nothing here is tied to a specific store.
        /// </summary>
        private static GoogleCredential ResolveServiceAccountCredential(IConfiguration configuration)
        {
            var serviceAccountPath = configuration["Firebase:ServiceAccountPath"];

            if (string.IsNullOrWhiteSpace(serviceAccountPath))
            {
                throw new InvalidOperationException("Firebase service account path not configured.");
            }

            return GoogleCredential.FromFile(serviceAccountPath);
        }

        /// <summary>
        /// Registers the generic repository layer. Adding a future module
        /// (e.g. AccommodationListing) only requires one more line here.
        /// </summary>
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<IFirestoreRepository<SiteSettings>>(sp =>
                new FirestoreRepository<SiteSettings>(sp.GetRequiredService<FirestoreDb>(), "SiteSettings"));

            services.AddScoped<IListingRepository, ListingRepository>();
            services.AddScoped<IFavoriteRepository, FavoriteRepository>();
            services.AddScoped<IAccommodationRepository, AccommodationRepository>();
            services.AddScoped<ILandlordApplicationRepository, LandlordApplicationRepository>();

            services.AddScoped<IFirestoreRepository<Category>>(sp =>
                new FirestoreRepository<Category>(sp.GetRequiredService<FirestoreDb>(), "Categories"));

            services.AddScoped<IFirestoreRepository<Report>>(sp =>
                new FirestoreRepository<Report>(sp.GetRequiredService<FirestoreDb>(), "Reports"));

            services.AddScoped<IFirestoreRepository<ActivityLog>>(sp =>
                new FirestoreRepository<ActivityLog>(sp.GetRequiredService<FirestoreDb>(), "ActivityLogs"));

            // Future repositories are registered the same generic way, e.g.:
            // services.AddScoped<IFirestoreRepository<Listing>>(sp =>
            //     new FirestoreRepository<Listing>(sp.GetRequiredService<FirestoreDb>(), "Listings"));

            return services;
        }

        /// <summary>
        /// Registers the Site Settings CMS service (cached reads, immediate
        /// cache invalidation on write - see SiteSettingsService) and the
        /// Firebase Storage service used for logo/favicon/media uploads.
        /// </summary>
        public static IServiceCollection AddCmsServices(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddScoped<ISiteSettingsService, SiteSettingsService>();
            services.AddScoped<IStorageService, FirebaseStorageService>();
            return services;
        }

        /// <summary>
        /// Registers the Listings core domain service (Phase 4 - Books MVP).
        /// </summary>
        public static IServiceCollection AddListingServices(this IServiceCollection services)
        {
            services.AddScoped<IListingService, ListingService>();
            services.AddScoped<ICategoryService, CategoryService>();
            return services;
        }

        /// <summary>
        /// Registers the Phase 6 Admin Panel services: activity logging (used
        /// by all of the below), report handling, and admin-side user
        /// management. Must be called after AddListingServices/AddCmsServices
        /// since it doesn't redeclare their dependencies.
        /// </summary>
        public static IServiceCollection AddAdminServices(this IServiceCollection services)
        {
            services.AddScoped<IActivityLogService, ActivityLogService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IAdminUserService, AdminUserService>();
            return services;
        }

        /// <summary>
        /// Registers Phase 7 services: favorites/wishlist. Must be called
        /// after AddListingServices, since FavoriteService depends on
        /// IListingService to resolve favorited listings.
        /// </summary>
        public static IServiceCollection AddFavoriteServices(this IServiceCollection services)
        {
            services.AddScoped<IFavoriteService, FavoriteService>();
            return services;
        }

        /// <summary>
        /// Registers Phase 11 services: Student Accommodation. Deliberately
        /// separate from AddListingServices - Accommodation is not a
        /// Marketplace module.
        /// </summary>
        public static IServiceCollection AddAccommodationServices(this IServiceCollection services)
        {
            services.AddScoped<IAccommodationService, AccommodationService>();
            services.AddScoped<ILandlordVerificationService, LandlordVerificationService>();
            return services;
        }

        /// <summary>
        /// Registers the Firebase-backed auth service. Kept separate from
        /// AddFirebaseServices/AddRepositories so Program.cs reads as a clear
        /// list of what's being wired up.
        /// </summary>
        public static IServiceCollection AddAuthServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            return services;
        }
    }
}
