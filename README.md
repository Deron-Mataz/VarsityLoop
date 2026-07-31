# Varsity Loop

Student marketplace platform (ASP.NET Core MVC / .NET 8 / Firebase). MVP scope: textbook listings, with architecture built to extend to Accommodation, Electronics, Services, and more.

## Build status

This solution is being built in phases. **Phase 1 (Foundation) is complete.**

- [x] Phase 1 — Project scaffold, Firebase DI wiring, generic Firestore repository layer, error pages (404/401/403/500), base layout, homepage shell with empty states
- [x] Phase 2 — Firebase Authentication (register/login/logout/forgot-password/email verification), Firestore-driven RBAC, cookie session auth
- [x] Phase 3 — Site Settings CMS + dynamic branding
- [x] Phase 4 — Listings core (Books MVP), image upload
- [x] Phase 5 — Search, filters, pagination, categories, homepage wiring
- [x] Phase 6 — Admin panel: users, listing moderation, reports, activity logs, roles (Media Library deferred - see note below)
- [x] Phase 7 — Seller profiles, favorites/wishlist, profile management, Accommodation/Electronics/Services placeholders

## Section 2 — Marketplace Expansion

- [x] Phase 8 — Electronics marketplace (dynamic Type/Brand/Model/Specifications fields, reusing the existing Listing/Category architecture)
- [x] Phase 9 — Fashion marketplace (reuses Phase 8's dynamic fields entirely; just a new suggested-types list)
- [ ] Phase 10 — Study Supplies marketplace
- [ ] Phase 11 — Student Accommodation foundation
- [ ] Phase 12 — Landlord verification system
- [ ] Phase 13 — Landlord dashboard
- [ ] Phase 14 — Accommodation CMS
- [ ] Phase 15 — Real-time marketplace chat
- [ ] Phase 16 — Professional admin analytics dashboard

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

10. **Admin Panel (Phase 6)**: signed-in Admins/SuperAdmins get a dashboard at `/Admin` linking to Users (search, role assignment, deactivate/delete), Listings (search, suspend/remove/restore), Categories, Reports (from the "Report this listing" link on any listing page), and a full Activity Log recording every moderation action. Only a SuperAdmin can grant or revoke the SuperAdmin role — an Admin attempting that gets a clear error rather than a silent failure. No new Firestore indexes are needed for any of this — these queries follow the same single-equality-filter-then-sort-in-memory pattern from Phase 5.

    **Scope note**: the original spec's "Media Library" (a standalone view of all uploaded files) was intentionally left out of Phase 6 — branding assets and listing photos are already manageable through Site Settings and each listing's own edit page respectively, so a separate library view wouldn't add functionality yet. Worth revisiting once there's an actual need to browse/reuse media across listings.

11. **Republish Storage Rules again**: Phase 7 adds profile picture uploads, which need the same public-read treatment as logos/listing photos. The `storage.rules` file in the repo root has been updated — republish it in Firebase Console → Storage → Rules.

12. **Phase 7 additions**: My Profile page (bio + profile picture, under your name in the nav), public seller profile pages (linked from any listing's "Seller" card), a Favorites/Wishlist ("Save to Favorites" button on any listing you don't own), and working Accommodation/Electronics/Services placeholder pages — those three nav links existed since Phase 1 but had no controller behind them until now (they were 404ing).

13. **Phase 8 — Electronics, and the dynamic listing form**: rather than building a separate Electronics marketplace, one `Listing` entity and one form now serve both Books and Electronics (and Fashion/Study Supplies once Phases 9-10 land). Each `Category` now has a `Module` (Books/Electronics/Fashion/StudySupplies/Accommodation/Services) — when creating a category in Admin → Categories, set its Module to match what it's for. The listing form shows Book fields (Author/ISBN/Course/Faculty) or Item fields (Type/Brand/Model/dynamic Specifications list) automatically based on which category is selected, via a small vanilla-JS toggle keyed off each category's Module — no page reload, no separate forms to maintain. To try it: create a category with Module = Electronics, then create a listing under it.

14. **Phase 9 — Fashion**: no new fields or form logic needed — Fashion listings use the exact same Type/Brand/Model/Specifications fields Electronics does. The only addition is `ListingTypeSuggestions.Fashion` (Shoes/Jerseys/Jackets/Hoodies/Dresses/Watches/Bags/Jewellery/Caps/Sunglasses/Other), and the Type field's autocomplete suggestions now swap between the Electronics list and the Fashion list depending on which category's Module is selected. To try it: create a category with Module = Fashion, then create a listing under it — same form, different suggestions.

> **Note on the index requirement above**: Phase 5 changed how Browse/filtering works internally — sorting now happens in the app rather than as a Firestore `orderBy`, which means the *original* Browse query from Phase 4 no longer needs a composite index at all. If you already created that index per step 8, it's now unused but harmless; if you're setting this up fresh, you likely won't hit that error for Browse/My Listings anymore. This trade-off (documented in `IListingRepository`) is fine for an MVP-sized catalogue — a large one would want to move filtering back into Firestore query clauses or a dedicated search index.

## Architecture notes

- **Generic repository layer** (`IFirestoreRepository<T>` / `FirestoreRepository<T>`): every entity (users, and later listings, categories, etc.) shares one CRUD implementation. Adding a new marketplace module is a new entity class + one DI line, not a new repository.
- **Soft deletes**: `BaseEntity.IsDeleted` — nothing is hard-deleted by default, so admin "restore" actions stay possible.
- **Roles are 100% data-driven**: `ApplicationUser.Role` is a string field in Firestore, editable from the Admin Panel. No role is ever hardcoded in code.
- **No secrets in source control**: `appsettings.json` and `firebase-service-account.json` are git-ignored; only the `.example.json` template is committed.
