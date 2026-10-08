# SkillBridge

SkillBridge is a peer-to-peer skill exchange website. Members list skills they can teach and learn, find reciprocal matches, request exchanges, confirm seven learning stages, rate completed exchanges, message one another, and post in skill communities.

## Technology

- ASP.NET MVC 5 and Razor views on .NET Framework 4.7.2
- Entity Framework 6 and ASP.NET Identity 2
- PostgreSQL through Npgsql (Supabase is the intended managed database)
- SignalR 2 for live chat, Bootstrap and jQuery for the interface

The frontend and backend are one web application. Supabase Auth and the Supabase browser Data API are not used; the server connects to PostgreSQL directly.

For host selection, production settings, and first-publish steps, see [DEPLOYMENT.md](DEPLOYMENT.md).

## Run locally

1. On Windows, install Visual Studio with the ASP.NET and web development workload and the .NET Framework 4.7.2 targeting pack.
2. Restore the NuGet packages in `SkillBridge.sln` and build the solution.
3. Create a new Supabase project. In its SQL Editor, run `database/postgres/001_initial.sql` and then `database/postgres/002_seed.sql`. These scripts create the app tables in the private `skillbridge` schema. Do not expose that schema in Supabase API settings.
4. Set the `SKILLBRIDGE_DB_CONNECTION` environment variable for the web app process. Use the connection details from Supabase's **Connect** panel, not a connection string committed to Git. A persistent server can use the direct connection if it supports IPv6; otherwise use the session pooler on port 5432. Use `SSL Mode=Require` and do not set `Trust Server Certificate`. Example shape: `Host=<pooler-host>;Port=5432;Database=postgres;Username=postgres.<project-ref>;Password=<password>;SSL Mode=Require`.
5. Download the server CA certificate from Supabase and keep it outside the repository. Set `SKILLBRIDGE_DB_CA_CERT` to its absolute path, or to a path relative to the application root such as `App_Data/certs/supabase-ca.cer`. For the relative option, place the certificate in the host's `App_Data/certs` folder after publishing; the folder is excluded from Git and is not served to browsers. Npgsql 4.1 does not support a `Root Certificate` connection string option, so the application's connection factory validates the server hostname and certificate chain against this CA. On Windows with .NET Framework, a DER-encoded `.cer` file is suitable.
6. Set `SKILLBRIDGE_MESSAGE_KEY` to a stable, random 32-byte key encoded as Base64. Keep it secret and backed up; changing it makes existing messages unreadable.
7. Start the MVC application with IIS Express or IIS.

## Password reset and contact settings

The password reset form sends a one-hour link through SMTP. Set these environment variables on the web app process before offering password resets:

- `SKILLBRIDGE_PUBLIC_URL`: the site's public HTTPS origin, such as `https://your-site.example/`. The app uses this fixed origin in reset links instead of trusting the request's Host header. HTTP is allowed only for localhost testing.
- `SKILLBRIDGE_SMTP_HOST`, `SKILLBRIDGE_SMTP_PORT`, `SKILLBRIDGE_SMTP_FROM`: the mail server, its submission port, and a verified sender address. Use a provider that supports STARTTLS on port 587; .NET Framework's SMTP client does not use implicit TLS on port 465.
- `SKILLBRIDGE_SMTP_USERNAME` and `SKILLBRIDGE_SMTP_PASSWORD`: credentials if the provider requires authentication. Keep them outside Git.
- `SKILLBRIDGE_SUPPORT_EMAIL`: the address to show on the Contact page, if available.

SMTP encryption is enabled by default. `SKILLBRIDGE_SMTP_SSL=false` works only with `localhost` or `127.0.0.1` for local email-catcher tests. When SMTP or the public URL is missing, the reset form shows an unavailable message instead of claiming an email was sent. A local SMTP catcher has verified the complete reset path; a real provider and sender address still need to be configured and checked before public deployment.

## Browser smoke checks

Install Node.js, run `npm ci`, then `npx playwright install chromium`. With the site running, set `SKILLBRIDGE_BASE_URL` to its URL (or use the IIS Express default `https://localhost:44364`) and run `npm run test:e2e`. The current checks cover guest pages and redirects; a live database and test accounts are needed for member flows.

The database scripts are for a **new, empty** PostgreSQL database. Existing SQL Server data needs a separate data migration; the historical SQL Server migrations in `Migrations/` are retained for reference and are excluded from the PostgreSQL build. Schema changes after this baseline should be added as numbered PostgreSQL SQL scripts and reviewed before running. The app does not modify its schema at startup.

A live two-user browser check passed against Supabase PostgreSQL: registration, reciprocal matching, skill request and acceptance, encrypted chat, confirmation of both skills' seven stages by both users, interaction completion, and both ratings. The temporary test accounts and their data were removed afterward.

## Current development limits

- Production password-reset delivery needs SMTP provider settings and a verified sender address.
- Set a support email and verify that the issue tracker is accessible to visitors before opening the site publicly.
- Legacy ASP.NET MVC 5 requires Windows hosting or a compatible Windows container.
