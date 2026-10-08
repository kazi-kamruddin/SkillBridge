# SkillBridge

SkillBridge is a peer-to-peer skill exchange website. Members list skills they can teach and learn, find reciprocal matches, request exchanges, confirm seven learning stages, rate completed exchanges, message one another, and post in skill communities.

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

The database schema is managed by the numbered SQL files in `database/postgres/`. The application does not run schema migrations at startup. The Core compatibility script preserves existing accounts, password hashes, messages, and application records. The message encryption format is unchanged, so the original message key must be kept.

## Checks

Run `dotnet publish SkillBridge.Core/SkillBridge.Core.csproj -c Release` to compile controllers and Razor views. Existing Playwright guest checks can run with `npm ci`, `npx playwright install chromium`, `SKILLBRIDGE_BASE_URL` pointed at the running site, and `npm run test:e2e`.

The prior MVC 5 version passed a live two-user flow against Supabase: registration, matching, request and acceptance, encrypted chat, confirmation of both skills' seven stages by both users, completion, and ratings. The temporary accounts were removed. The Core port still needs the same live flow after the compatibility SQL is applied.
