# Varsity Loop

Student marketplace platform (ASP.NET Core MVC / .NET 8 / Firebase). MVP scope: textbook listings, with architecture built to extend to Accommodation, Electronics, Services, and more.

## Build status

This solution is being built in phases. **Phase 1 (Foundation) is complete.**

- [x] Phase 1 — Project scaffold, Firebase DI wiring, generic Firestore repository layer, error pages (404/401/403/500), base layout, homepage shell with empty states
- [ ] Phase 2 — Firebase Authentication (register/login/logout/forgot-password/email verification), Firestore-driven RBAC, cookie session auth
- [ ] Phase 3 — Site Settings CMS + dynamic branding
- [ ] Phase 4 — Listings core (Books MVP), image upload
- [ ] Phase 5 — Search, filters, pagination, categories, homepage wiring
- [ ] Phase 6 — Admin panel (users, listings, reports, activity logs, roles)
- [ ] Phase 7 — Seller profiles, favorites/wishlist, Accommodation/Electronics/Services placeholders

Later phases will build directly on top of what's here — nothing below needs to be redone.

## Setup (Visual Studio 2022)

1. **Firebase project**: create one at https://console.firebase.google.com if you don't have one. Enable:
   - Authentication (Email/Password provider)
   - Firestore Database
   - Storage

2. **Service account key**: Project Settings → Service Accounts → "Generate new private key". Save the downloaded JSON as
   `VarsityLoop/firebase-service-account.json` (this exact path is already git-ignored).

3. **App config**: copy `VarsityLoop/appsettings.example.json` to `VarsityLoop/appsettings.json` and fill in:
   - `ProjectId`, `ApiKey`, `AuthDomain`, `StorageBucket` — from Project Settings → General
   - `ServiceAccountKeyPath` — leave as `firebase-service-account.json` if you followed step 2

4. Open `VarsityLoop.sln` in Visual Studio 2022. On first build, LibMan restores Bootstrap 5 into `wwwroot/lib` automatically (or run `Right-click solution → Restore Client-Side Libraries` if it doesn't trigger automatically).

5. Press F5. The app throws a clear startup error if the service account file or config is missing — that's expected until step 2/3 are done.

## Architecture notes

- **Generic repository layer** (`IFirestoreRepository<T>` / `FirestoreRepository<T>`): every entity (users, and later listings, categories, etc.) shares one CRUD implementation. Adding a new marketplace module is a new entity class + one DI line, not a new repository.
- **Soft deletes**: `BaseEntity.IsDeleted` — nothing is hard-deleted by default, so admin "restore" actions stay possible.
- **Roles are 100% data-driven**: `ApplicationUser.Role` is a string field in Firestore, editable from the Admin Panel. No role is ever hardcoded in code.
- **No secrets in source control**: `appsettings.json` and `firebase-service-account.json` are git-ignored; only the `.example.json` template is committed.
