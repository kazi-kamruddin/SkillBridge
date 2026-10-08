# SkillBridge first deployment

This guide documents the original MVC 5/Windows application. The ASP.NET Core 10/Render path is in [DEPLOYMENT_CORE.md](DEPLOYMENT_CORE.md).

This is one ASP.NET MVC 5 application on .NET Framework 4.7.2. Razor pages, C# controllers, authentication, and SignalR chat are deployed together to a Windows/IIS website. Visitors use one HTTPS URL. The application connects directly to PostgreSQL on Supabase; it does not use Supabase Auth or the Supabase browser API.

## Host decision

Choose a host that can run classic ASP.NET MVC 5 on Windows/IIS, set process environment variables, provide HTTPS, serve SignalR 2, and open an outbound TCP connection to the Supabase session pooler on port 5432. Check the host's current terms for the intended users and data before opening registration.

As checked on 8 October 2026:

- [MonsterASP.NET Free](https://help.monsterasp.net/getting-started/first-steps/free-vs-premium) is the closest technical fit for a portfolio or university **test/demo** with synthetic accounts. It supports .NET Framework, SignalR, environment variables, a free subdomain, and free HTTPS. Its [terms](https://www.monsterasp.net/Terms/) restrict Free hosting to development, testing, learning, and evaluation and exclude production workloads and production processing of personal data. It is unsuitable for open registration by real users unless the provider confirms the specific use is permitted. Outbound connections to an external Supabase database are not documented and must be confirmed or tested.
- [Somee Free](https://www.somee.com/FreeAspNetHosting.aspx) is another Windows/MVC 5 candidate for a light application. It inserts advertisements, has 150 MB site storage and 5 GB monthly transfer, and requires manual renewal of its free HTTPS certificate. Its [free-hosting policy](https://www.somee.com/doka/Help/Article/142/Terms_%26_Policies_for_Free_Accounts) describes learning, testing, and light applications. Confirm that the intended public use is allowed, and that its free site can set process environment variables and connect to external PostgreSQL on port 5432. Those last two capabilities are not established from its published documentation.
- Azure App Service F1 has a $0 app tier, but a new Azure Free Trial subscription must move to pay-as-you-go after the trial to keep services running. This does not satisfy a strict no-billing-exposure rule. See the [Azure account offer](https://azure.microsoft.com/en-us/pricing/purchase-options/pay-as-you-go).

There is currently no verified free Windows host that meets every requirement for unrestricted public use with real user data. This limitation applies to the legacy MVC 5 deployment path; the Core deployment path is documented separately.

## What to prepare on the chosen host

| Setting | Required value |
| --- | --- |
| `SKILLBRIDGE_DB_CONNECTION` | Supabase PostgreSQL connection string from its **Connect** panel. Prefer the IPv4 session pooler on port 5432 when the host has no IPv6 route. Use `SSL Mode=Require`; do not use `Trust Server Certificate`. |
| `SKILLBRIDGE_DB_CA_CERT` | An absolute certificate path or `App_Data/certs/supabase-ca.cer` relative to the website root. Upload the verified Supabase CA file to that location after publishing. The code resolves relative paths from the application root. |
| `SKILLBRIDGE_MESSAGE_KEY` | The existing stable, Base64-encoded 32-byte encryption key. Back it up separately. Replacing it makes existing messages unreadable. |
| `SKILLBRIDGE_PUBLIC_URL` | The final HTTPS origin with a trailing slash, for example `https://your-site.runasp.net/`. Required for password-reset email links. |
| `SKILLBRIDGE_SMTP_HOST`, `SKILLBRIDGE_SMTP_PORT`, `SKILLBRIDGE_SMTP_FROM` | An SMTP provider and verified sender. Use STARTTLS on port 587. Add `SKILLBRIDGE_SMTP_USERNAME` and `SKILLBRIDGE_SMTP_PASSWORD` if the provider requires them. Password reset reports unavailable until SMTP and the public URL are set. |
| `SKILLBRIDGE_SUPPORT_EMAIL` | Optional Contact page address. |

Set secrets in the host's server-side configuration, not in the repository or JavaScript. Do not upload a publish profile or secret-bearing files into the public website folder. Keep the CA certificate in `App_Data/certs` or another private file location; never put it in `Content` or `Scripts`.

The existing Supabase project's `skillbridge` schema was established by `database/postgres/001_initial.sql`, followed by `002_seed.sql`. `001_initial.sql` is for a new, empty database; do not rerun it against an existing populated project. New schema changes need new reviewed SQL scripts. The app does not change its schema at startup. Supabase [recommends independent exports for Free projects](https://supabase.com/docs/guides/platform/backups).

## Publish the first version

1. Build and publish `SkillBridge.csproj` from Visual Studio using the **Release** configuration and a Folder publish target. Publish the project, not the repository ZIP or the source folder. The Release transform sets `debug="false"` and enables generic remote error pages.
2. Inspect the published folder: `Web.config`, `bin`, views, styles, scripts, and static assets should be present. `notes.txt`, `.gitignore`, passwords, and the CA certificate should not be included. Do not deploy until the host's current terms and external database connection are acceptable.
3. On the host, select **.NET Framework 4.x** and the **Integrated** IIS pipeline. Enter the required environment variables in its configuration UI. Upload the published folder contents to the site's web root, then create `App_Data/certs` and upload the CA certificate there. Restart the application after changing environment variables.
4. Open the site's guest pages. Sign-in cookies are marked Secure, so login requires HTTPS. Once HTTPS is active, enable the host's HTTP-to-HTTPS redirect and set `SKILLBRIDGE_PUBLIC_URL` to that HTTPS origin. On MonsterASP.NET Free, [HTTPS activation requires a support request after content has been uploaded](https://help.monsterasp.net/domains-https/https/lets-encrypt).
5. Check the home page, registration/login, database-backed pages, and two-user matching/chat/notifications. Check password reset only after SMTP is configured. If the site fails to start, inspect host logs before changing database secrets or certificate settings.

No account creation, host deployment, database migration, SMTP setup, or public URL change is performed by these repository files. Those actions depend on the account owner and the chosen host.
