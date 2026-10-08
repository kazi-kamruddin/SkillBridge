# SkillBridge ASP.NET Core deployment

This is one ASP.NET Core 10 MVC app with Razor pages, C# controllers, Identity, and SignalR. Render builds the repository's `Dockerfile` on its servers; Docker Desktop does not need to run on the developer's PC. The public app uses one `onrender.com` URL and connects directly to the existing Supabase PostgreSQL project.

## Before deployment

1. Export or back up the current Supabase database. The compatibility script keeps data, but its identity changes need an independent recovery copy before applying them.
2. In the existing Supabase project's SQL Editor, review and run `database/postgres/003_aspnet_core_identity.sql` **once**. Do not rerun `001_initial.sql` on an existing database. The new script adds Core Identity fields, persistent cookie/reset-token keys, and required Identity tables. It does not recreate users or application data.
   Before deploying the public-browsing update, also run `database/postgres/004_public_browsing_and_moderation.sql` once. It adds profile visibility, content moderation flags and reports. Existing profiles default to hidden from signed-out visitors. Review existing community content before publishing it to guests.
   Before deploying the issue #36 update, run `database/postgres/005_discovery_exchanges_safety.sql` once after 004. It adds exchange proposal fields, profile reports and blocks, moderation hiding, and early exchange endings. Do not rerun 001.
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
   | `SKILLBRIDGE_MODERATOR_EMAIL` | Email of an existing SkillBridge account that may review community reports at `/Moderation`. No email delivery service is required for this setting. |
   | `SKILLBRIDGE_BREVO_API_KEY` | Brevo transactional email API key, when ready. |
   | `SKILLBRIDGE_BREVO_FROM` | Sender address configured and verified in Brevo, when ready. |

   `SKILLBRIDGE_DB_CA_CERT` is optional. Set it only if replacing the certificate bundled at `SkillBridge.Core/certs/prod-ca-2021.crt`. The certificate is a public trust anchor, not a credential.
4. Let Render build and deploy. Open the assigned URL and test home, About, Contact, registration, login, profile, matching, a request, chat, stages, ratings, and logout. Test password reset after Brevo is configured and the sender works. Use two accounts for the connected flows.

Signed-out visitors can browse skills, public profiles whose owners opted in, and community posts. Creating posts, commenting, reporting, matching, requests, interactions, messages, notifications, and moderation require sign-in. Community posting and commenting additionally require that skill on the member's profile. Test a guest session and a member session after deploying the update. Reports require `SKILLBRIDGE_MODERATOR_EMAIL` to match an existing account; that account can dismiss a report or hide the reported post or comment.

The app's Data Protection keys are stored in the private `skillbridge."DataProtectionKeys"` database table. This keeps sign-in cookies and reset tokens valid across Render restarts. Render's local filesystem is ephemeral, so do not add user uploads to it without separate persistent storage.

## Free-plan limits

Render's Free web service sleeps when idle, can suspend when its limits are reached, and [is intended for hobby/testing rather than production service](https://render.com/docs/free). [Supabase Free can pause an inactive project](https://supabase.com/docs/guides/platform/free-project-pausing). If strict zero billing is required, leave Render on the Free plan **without a payment method**; [Render suspends Free services instead of billing excess bandwidth when no payment method exists](https://render.com/docs/free). Do not create a Render Free PostgreSQL database for SkillBridge; its free database expires after 30 days. Keep Supabase as the database.

Render Free blocks outbound SMTP ports 25, 465 and 587. Password reset therefore uses Brevo's HTTPS API. [Brevo Free includes 300 email sends per day and has no time limit](https://help.brevo.com/hc/en-us/articles/208580669-FAQs-What-are-the-limits-of-the-Free-plan). [An authenticated sender domain is recommended for delivery](https://help.brevo.com/hc/en-us/articles/14925263522578-Comply-with-Gmail-Yahoo-and-Microsoft-s-requirements-for-email-senders); verify a real reset message before inviting visitors to rely on password recovery.
