# Varsity Loop

Student marketplace platform (ASP.NET Core MVC / .NET 8 / Firebase). MVP scope: textbook listings, with architecture built to extend to Accommodation, Electronics, Services, and more.

## Build status

This solution is being built in phases. **Phase 1 (Foundation) is complete.**

- [x] Phase 1 — Project scaffold, Firebase DI wiring, generic Firestore repository layer, error pages (404/401/403/500), base layout, homepage shell with empty states
- [x] Phase 2 — Firebase Authentication (register/login/logout/forgot-password/email verification), Firestore-driven RBAC, cookie session auth
- [x] Phase 3 — Site Settings CMS + dynamic branding
- [x] Phase 4 — Listings core (Books MVP), image upload
- [x] Phase 5 — Search, filters, pagination, categories, homepage wiring
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

4. Open `VarsityLoop.sln` in Visual Studio 2022. (Bootstrap 5 loads from the jsDelivr CDN with a Subresource Integrity hash — no local restore step needed.)

5. Press F5. The app throws a clear startup error if the service account file or config is missing — that's expected until step 2/3 are done.

6. **First admin account**: in `appsettings.json`, set `AppSettings:DefaultAdminEmail` to the email you plan to register with. That account is automatically granted the `SuperAdmin` role the moment it registers, so you have a way into the Admin Panel on a fresh database. Every other sign-up defaults to the `User` role — all further role changes happen from the Admin Panel (Phase 6) from then on.

7. **Firebase Storage public read rules**: logos/favicons (Site Settings) and listing photos (Phase 4) need to be publicly viewable. In Firebase Console → Storage → Rules, paste `storage.rules` from the repo root and click **Publish** (write access stays server-only, since uploads always go through this app's service account, never the browser directly).

8. **Firestore composite indexes**: Phase 4's listing queries (browse-by-status, search, "my listings") each filter on two fields and sort by a third, which Firestore requires a composite index for. The **first time** each query runs, it throws an error containing a direct "create this index" link — click it, wait ~1-2 minutes for the index to build, then retry. This only needs to happen once per query shape (Browse, MyListings), not per listing.

9. **Create at least one category before listing anything**: go to Admin → Categories → New Category. Listings require a category to be selected, and the dropdown is empty until one exists.

> **Note on the index requirement above**: Phase 5 changed how Browse/filtering works internally — sorting now happens in the app rather than as a Firestore `orderBy`, which means the *original* Browse query from Phase 4 no longer needs a composite index at all. If you already created that index per step 8, it's now unused but harmless; if you're setting this up fresh, you likely won't hit that error for Browse/My Listings anymore. This trade-off (documented in `IListingRepository`) is fine for an MVP-sized catalogue — a large one would want to move filtering back into Firestore query clauses or a dedicated search index.

## Architecture notes

- **Generic repository layer** (`IFirestoreRepository<T>` / `FirestoreRepository<T>`): every entity (users, and later listings, categories, etc.) shares one CRUD implementation. Adding a new marketplace module is a new entity class + one DI line, not a new repository.
- **Soft deletes**: `BaseEntity.IsDeleted` — nothing is hard-deleted by default, so admin "restore" actions stay possible.
- **Roles are 100% data-driven**: `ApplicationUser.Role` is a string field in Firestore, editable from the Admin Panel. No role is ever hardcoded in code.
- **No secrets in source control**: `appsettings.json` and `firebase-service-account.json` are git-ignored; only the `.example.json` template is committed.
