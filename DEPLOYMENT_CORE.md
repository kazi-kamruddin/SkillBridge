# SkillBridge ASP.NET Core deployment

This is one ASP.NET Core 10 MVC app with Razor views, C# controllers, Identity, and SignalR. Render builds the repository's `Dockerfile` on its servers; Docker Desktop does not need to run on the developer's PC. The public app uses one `onrender.com` URL and connects directly to the existing Supabase PostgreSQL project.

## Before deployment

1. Export or back up the current Supabase database. The compatibility script keeps data, but its identity changes need an independent recovery copy before applying them.
2. In the existing Supabase project's SQL Editor, review and run `database/postgres/003_aspnet_core_identity.sql` **once**. Do not rerun `001_initial.sql` on an existing database. The new script adds Core Identity fields, persistent cookie/reset-token keys, and required Identity tables. It does not recreate users or application data.
   Before deploying the public-browsing update, also run `database/postgres/004_public_browsing_and_moderation.sql` once. It adds profile visibility, content moderation flags and reports. Existing profiles default to hidden from signed-out visitors. Review existing community content before publishing it to guests.
   Before deploying the issue #36 update, run `database/postgres/005_discovery_exchanges_safety.sql` once after 004. It adds exchange proposal fields, profile reports and blocks, moderation hiding, and early exchange endings. Do not rerun 001.
   Before deploying Google sign-in and exchange planning, run `database/postgres/006_member_access_and_planning.sql` once after 005. It adds availability, meeting proposals, stage notes and saved profiles. It confirms pre-existing accounts once so their logins keep working when email confirmation becomes required. Back up the database first; do not deploy the new code before this script succeeds.
   Before deploying Cloudinary uploads and lightweight onboarding, run `database/postgres/007_member_images_and_light_onboarding.sql` once after 006. It adds nullable optional profile fields and image references for profiles, community items and messages. Deploy the code only after this script succeeds.
   Before deploying exchange plan editing, availability filters, and the expanded directory, run `database/postgres/008_exchange_plans_and_discovery.sql` then `database/postgres/009_catalog_expansion.sql` in that order. The app does not apply them automatically. The catalog editor is the account configured in `SKILLBRIDGE_MODERATOR_EMAIL`; its review page is `/Catalog/Suggestions`.
   Before deploying meeting history, run `database/postgres/010_meeting_history.sql` once after 009. It preserves the latest saved state of older meeting plans and logs future meeting changes. Deploy the new code only after the SQL script succeeds.
3. Keep the existing `SKILLBRIDGE_MESSAGE_KEY`. Changing it makes old encrypted chat messages unreadable.
4. Publish and test the Core app locally against the updated database before pointing visitors to it. Existing ASP.NET Identity 2 password hashes can be verified by Core Identity; successful login may upgrade a hash, so a database backup also matters for rollback to MVC 5.

## Render setup performed by the account owner

1. Put the intended source branch on your GitHub repository yourself. No repository push is performed by these files.
2. In Render, create a **Web Service** from that GitHub repository. Select the **Docker** runtime and the **Free** service plan. Use the repository-root `Dockerfile`, root build context, and `/health` as the health-check path. The Dockerfile binds the app to port `10000` and sets the legacy app's local-time behavior to `Asia/Dhaka`.
3. Add these server-side environment variables in Render. Do not put their values in Git or in the browser:

   | Variable | Value |
   | --- | --- |
   | `SKILLBRIDGE_DB_CONNECTION` | Existing Supabase session-pooler connection string, port 5432. The app upgrades TLS to `VerifyFull` using the included public CA certificate. |
   | `SKILLBRIDGE_MESSAGE_KEY` | The existing Base64 32-byte message key. |
   | `SKILLBRIDGE_PUBLIC_URL` | The final `https://<service>.onrender.com/` origin, with a trailing slash. |
   | `SKILLBRIDGE_SUPPORT_EMAIL` | Optional support address for Contact. |
   | `SKILLBRIDGE_MODERATOR_EMAIL` | Email of an existing SkillBridge account that may review community and profile reports at `/Moderation`. No email delivery service is required for this setting. |
   | `SKILLBRIDGE_BREVO_API_KEY` | Brevo transactional email API key; required for new email/password registration in this release. |
   | `SKILLBRIDGE_BREVO_FROM` | Sender address configured and verified in Brevo; required for new email/password registration in this release. |
   | `SKILLBRIDGE_GOOGLE_CLIENT_ID` | Google web OAuth client ID, when Google sign-in is ready. |
   | `SKILLBRIDGE_GOOGLE_CLIENT_SECRET` | Matching Google web OAuth client secret, when Google sign-in is ready. |
   | `SKILLBRIDGE_CLOUDINARY_CLOUD_NAME` | Cloud name from the Cloudinary Console. |
   | `SKILLBRIDGE_CLOUDINARY_API_KEY` | Cloudinary API key for server-side uploads. |
   | `SKILLBRIDGE_CLOUDINARY_API_SECRET` | Matching secret; keep it only in Render's environment settings. |

   `SKILLBRIDGE_DB_CA_CERT` is optional. Set it only if replacing the certificate bundled at `SkillBridge.Core/certs/prod-ca-2021.crt`. The certificate is a public trust anchor, not a credential.
   For this release, **Brevo API key, verified sender and public URL are required for new email/password sign-ups**. New accounts cannot use member features before confirming their email. If Brevo is not configured, the registration form stays unavailable instead of creating accounts that cannot sign in. Password reset also uses Brevo. Google sign-in is optional; the Google buttons appear only when both Google variables are present. In Google Cloud, create a Web OAuth client and set the authorized redirect URI to `https://<your-render-service>.onrender.com/signin-google` (for this Render service: `https://skillbridge-08v4.onrender.com/signin-google`). Request only `openid`, `email` and `profile`. Keep its client secret in Render, never in Git.
   Existing members must sign in with their existing password first and use **Link Google sign-in** on their own profile. The app does not automatically merge accounts merely because Google returns a matching email. New Google accounts are created only when Google supplies a verified email and continue to profile setup. Google-only members can add a password from their profile later.
4. Let Render build and deploy. Open the assigned URL and test home, About, Contact, registration, login, profile, matching, a request, chat, stages, ratings, and logout. Test password reset after Brevo is configured and the sender works. Use two accounts for the connected flows.
   For this release, also test confirmation email, resend, Google sign-in and linking, saved profiles, meeting proposal/accept/decline/cancel, both members' stage notes, and unread/read messages. Run 006 on Supabase **before** merging this code into Render's source branch.

The `/health` endpoint checks that the web process responds; it does not query PostgreSQL. The GitHub production workflow also checks the public skills and communities pages after Render marks a deployment live, so a database-backed page failure will fail the workflow. These checks still do not replace authenticated member-flow testing.

Account submissions share an in-memory limit of 40 requests per minute; selected member write actions allow 40 requests per minute per signed-in account. Excess requests return HTTP 429 with `Retry-After: 60`. Limits reset when the Render instance restarts and are intended for this single-instance deployment. ASP.NET Core sends warnings and errors to Render's log stream, including throttled requests and moderation decisions; do not put passwords, reset tokens, message contents, or connection strings in logs. The Razor frontend and backend share one origin, so no CORS policy or extra service is required.

Signed-out visitors can browse skills, public profiles whose owners opted in, and community posts. Creating posts, commenting, reporting, matching, requests, interactions, messages, notifications, and moderation require sign-in. Community posting and commenting additionally require that skill on the member's profile. Test a guest session and a member session after deploying the update. Reports require `SKILLBRIDGE_MODERATOR_EMAIL` to match an existing account; that account can dismiss a report, hide a reported post or comment, or hide a reported profile from discovery.

The app's Data Protection keys are stored in the private `skillbridge."DataProtectionKeys"` database table. This keeps sign-in cookies and reset tokens valid across Render restarts. Render's local filesystem is ephemeral, so do not add user uploads to it without separate persistent storage.

## Free-plan limits

Render's Free web service sleeps when idle, can suspend when its limits are reached, and [is intended for hobby/testing rather than production service](https://render.com/docs/free). [Supabase Free can pause an inactive project](https://supabase.com/docs/guides/platform/free-project-pausing). If strict zero billing is required, leave Render on the Free plan **without a payment method**; [Render suspends Free services instead of billing excess bandwidth when no payment method exists](https://render.com/docs/free). Do not create a Render Free PostgreSQL database for SkillBridge; its free database expires after 30 days. Keep Supabase as the database.

Render Free blocks outbound SMTP ports 25, 465 and 587. Password reset therefore uses Brevo's HTTPS API. [Brevo Free includes 300 email sends per day and has no time limit](https://help.brevo.com/hc/en-us/articles/208580669-FAQs-What-are-the-limits-of-the-Free-plan). [An authenticated sender domain is recommended for delivery](https://help.brevo.com/hc/en-us/articles/14925263522578-Comply-with-Gmail-Yahoo-and-Microsoft-s-requirements-for-email-senders); verify a real reset message before inviting visitors to rely on password recovery.
