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
- [x] Pre-Phase-10 Stabilization — robust listing-creation error handling, Marketplace/Module/Category architecture, dynamic per-module placeholders, module-aware category icons, redesigned Marketplace browsing (search/module nav/category chips/AJAX results/curated home feed)
- [ ] Phase 10 — Study Supplies marketplace
- [ ] Phase 11 — Student Accommodation foundation
- [ ] Phase 12 — Landlord verification system
- [ ] Phase 13 — Landlord dashboard
- [ ] Phase 14 — Accommodation CMS
- [ ] Phase 15 — Real-time marketplace chat
- [ ] Phase 16 — Professional admin analytics dashboard

Later phases will build directly on top of what's here — nothing below needs to be redone.

## Architecture notes

- **Generic repository layer** (`IFirestoreRepository<T>` / `FirestoreRepository<T>`): every entity (users, and later listings, categories, etc.) shares one CRUD implementation. Adding a new marketplace module is a new entity class + one DI line, not a new repository.
- **Soft deletes**: `BaseEntity.IsDeleted` — nothing is hard-deleted by default, so admin "restore" actions stay possible.
- **Roles are 100% data-driven**: `ApplicationUser.Role` is a string field in Firestore, editable from the Admin Panel. No role is ever hardcoded in code.
- **No secrets in source control**: `appsettings.json` and `firebase-service-account.json` are git-ignored; only the `.example.json` template is committed.
