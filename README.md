# SkillBridge

SkillBridge is a peer-to-peer skill exchange website. Members list skills they can teach and learn, find reciprocal matches, request exchanges, plan meeting times, record learning-stage notes, rate completed exchanges, message one another, save member profiles, and post in skill communities.

## Current application

`SkillBridge.Core/` is the ASP.NET Core 10 MVC application. It serves the existing Razor pages, Bootstrap/jQuery assets, C# controllers, Identity login, and SignalR chat from one URL. EF Core connects directly to the existing Supabase PostgreSQL `skillbridge` schema; the browser never receives database credentials and Supabase Auth/Data API are not used.

The original ASP.NET MVC 5/.NET Framework 4.7.2 application remains at the repository root for comparison and rollback. Its Windows hosting instructions are in [DEPLOYMENT.md](DEPLOYMENT.md). The new Render deployment steps are in [DEPLOYMENT_CORE.md](DEPLOYMENT_CORE.md).

## Local development of the Core app

1. Install the .NET 10 SDK. Docker Desktop is not needed for normal development.
2. Use the existing Supabase project. The new app needs `database/postgres/003_aspnet_core_identity.sql` applied after `001_initial.sql` and `002_seed.sql`. Existing databases must **not** rerun `001_initial.sql`.
3. Set `SKILLBRIDGE_DB_CONNECTION` to the Supabase PostgreSQL session-pooler connection string and `SKILLBRIDGE_MESSAGE_KEY` to the same stable Base64 32-byte key used by the MVC 5 app. Set these outside Git.
4. Run `dotnet run --project SkillBridge.Core/SkillBridge.Core.csproj`. The included public Supabase root CA certificate is used for full TLS verification. `SKILLBRIDGE_DB_CA_CERT` can override its file path if Supabase rotates the CA.
5. For password reset, set `SKILLBRIDGE_PUBLIC_URL`, `SKILLBRIDGE_BREVO_API_KEY`, and `SKILLBRIDGE_BREVO_FROM`. Until configured, the reset form accurately reports that delivery is unavailable. `SKILLBRIDGE_SUPPORT_EMAIL` is optional for the Contact page.
6. Before deploying the guest browsing and moderation update, run `database/postgres/004_public_browsing_and_moderation.sql` in Supabase. Set `SKILLBRIDGE_MODERATOR_EMAIL` in Render to an existing member's login email so that account can review reports at `/Moderation`. Existing profiles remain private to guests until their owners opt in.
7. Before deploying the issue #36 discovery and safety update, run `database/postgres/005_discovery_exchanges_safety.sql` once, after 004. It adds proposal details, blocking and profile reports, profile discovery hiding, and early exchange ending. Skill search uses a curated intent map for the current catalog; it does not call an AI API or require another service.
8. Before deploying Google sign-in, email verification, planning and saved members, run `database/postgres/006_member_access_and_planning.sql` once, after 005. Existing accounts keep sign-in access; new email/password accounts must confirm their email. Configure Brevo and `SKILLBRIDGE_PUBLIC_URL` **before** deploying this release, because new email/password registration waits for a confirmation message. Set `SKILLBRIDGE_GOOGLE_CLIENT_ID` and `SKILLBRIDGE_GOOGLE_CLIENT_SECRET` to enable Google sign-in; existing members link Google from their profile after signing in. Google OAuth's authorized redirect URI is `https://<your-render-service>.onrender.com/signin-google`.
9. Before deploying member and community images, run `database/postgres/007_member_images_and_light_onboarding.sql` once after 006. Set `SKILLBRIDGE_CLOUDINARY_CLOUD_NAME`, `SKILLBRIDGE_CLOUDINARY_API_KEY`, and `SKILLBRIDGE_CLOUDINARY_API_SECRET` on the server. Without these, the app can run but uploads fail. Keep the secret out of Git and browser code. Profile photos and community images are public; chat images use Cloudinary authenticated uploads and are streamed through a SkillBridge route that checks conversation membership. The Cloudinary account owner can still access chat images.
10. Before deploying exchange plan editing and availability filters, run `database/postgres/008_exchange_plans_and_discovery.sql` once after 007. It adds per-exchange milestone titles and proposals, allows catalog skills to have up to twelve stages, and adds structured available days and time zones. Both exchange partners must agree to a plan change; completed and already confirmed milestones are fixed. Custom milestones do not automatically raise a member's catalog skill level.
11. Run `database/postgres/009_catalog_expansion.sql` after 008. It adds twelve starter skills and their communities without changing existing entries, and creates a skill suggestion queue. Signed-in members can suggest skills from the directory. The account named by `SKILLBRIDGE_MODERATOR_EMAIL` can review suggestions at `/Catalog/Suggestions`, entering seven stages before approving a new skill.
12. Before deploying exchange meeting history, run `database/postgres/010_meeting_history.sql` once after 009. It stores future meeting proposals, responses, cancellations, and completions. Earlier exchanges retain only their latest saved meeting state because prior changes were overwritten. The application does not run this script automatically.

The database schema is managed by the numbered SQL files in `database/postgres/`. The application does not run schema migrations at startup. The Core compatibility script preserves existing accounts, password hashes, messages, and application records. The message encryption format is unchanged, so the original message key must be kept.

## Checks

Run `dotnet publish SkillBridge.Core/SkillBridge.Core.csproj -c Release` to compile controllers and Razor views. Existing Playwright guest checks can run with `npm ci`, `npx playwright install chromium`, `SKILLBRIDGE_BASE_URL` pointed at the running site, and `npm run test:e2e`.
Run `dotnet run --project tests/CloudinaryImageServiceChecks/CloudinaryImageServiceChecks.csproj` for local image validation and authenticated download-signature checks without real Cloudinary credentials.
Run `dotnet run --project tests/CoreModelChecks/CoreModelChecks.csproj` to verify the lazy-loading EF model can initialize without a database connection. A Release publish alone does not catch invalid proxy navigation properties.

The prior MVC 5 version passed a live two-user flow against Supabase: registration, matching, request and acceptance, encrypted chat, confirmation of both skills' seven stages by both users, completion, and ratings. The temporary accounts were removed. The Core port still needs the same live flow after the compatibility SQL is applied.
