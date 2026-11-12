<div align="center">

# SkillBridge

**Skills are better when shared.**

A full-stack peer-to-peer skill exchange platform connecting learners and mentors through reciprocal swaps, one-way mentorships, structured milestones, skill communities, and real-time encrypted messaging.

**[Live Website](https://skillbridge-08v4.onrender.com/)** · **[Features](#features)** · **[Getting Started](#getting-started)**

</div>

---

## Overview

SkillBridge is a web application built to make it easier for **people who know a skill to teach** and **learners eager to grow** to connect directly on one platform. Instead of informal one-off chats, members can list what they teach and want to learn, discover mutual swaps or one-way mentorships, agree on milestone plans, schedule sessions, and learn together step by step.

SkillBridge treats **skills as peer currency rather than money**. Members trade knowledge and time directly; the platform does **not** process payments or charge fees.

## Features

| Area | What you can do |
| --- | --- |
| **Authentication & Security** | Register and sign in with email/password or Google OAuth 2.0, verify email addresses, reset passwords via Brevo transactional email, and link Google sign-in to an existing account. |
| **Profiles & Availability** | Complete or skip light onboarding, manage skills you **Teach** and want to **Learn**, promote verified learned skills to teaching, configure weekly availability, time zones, and meeting preferences, and toggle public profile visibility. |
| **Discovery & Matching** | Browse the skill catalog, search by skill name or learning intent, filter by availability and time zone, explore **Best Matches** (2-way swaps), **Partial Matches** (1-way mentorships), and general members, or save profiles for later. |
| **Exchange & Mentorship Proposals** | Send structured 2-way swap or 1-way mentorship requests, customize milestone plans, and accept, decline, or cancel proposals. |
| **Structured Exchange Workspace** | Negotiate meeting schedules with tracked meeting history, propose and approve plan updates, write private per-stage notes, and unlock stages sequentially through mutual confirmation before completing or stopping an exchange. |
| **Peer Ratings** | Submit a mutual **1–10** rating once an exchange completes, updating public rating summaries and completed-exchange counts. |
| **Real-Time Encrypted Chat** | Start conversations from any profile or discovery card via **Knock**, manage threads in the searchable `/Messages/Inbox`, receive instant messages and navbar badge updates over SignalR (`/chatHub`), share authenticated private images, and store message text encrypted at rest. |
| **Skill Communities & Catalog** | Switch between `All Posts` and `My Skills` feeds, filter by `#TopicPills`, post and comment across catalog communities with image uploads and reply notifications, and suggest new catalog skills for moderator approval. |
| **Trust, Safety & Moderation** | Block members, report profiles, posts, or comments, hide profiles from discovery, protect endpoints with anti-forgery tokens and rate limiting, and review reports or catalog suggestions through moderator tools. |
| **Supporting Pages** | Home, How It Works, About Us, and Contact. |

The interface is responsive down to `320px` mobile viewports with light and dark theme support, styled with a neo-brutalist warm-paper, sage, blue, and coral design system using Tailwind CSS v4 (`tw:` prefix) and custom component tokens.

## Technology Stack

| Layer | Technologies |
| --- | --- |
| **Backend & Views** | ASP.NET Core 10 MVC (`net10.0`), Razor Views, C# 14 |
| **Styling & Client** | Tailwind CSS v4 CLI (`@tailwindcss/cli`), custom CSS design tokens, vanilla JS & jQuery validation |
| **Database & ORM** | PostgreSQL on Supabase (`skillbridge` schema) via Entity Framework Core 10 & Npgsql |
| **Authentication & Keys** | ASP.NET Core Identity, Google OAuth 2.0, EF Core persistent `DataProtectionKeys` |
| **Real-Time Communication** | SignalR Core (`/chatHub`) |
| **Media & Email** | Cloudinary (public & authenticated image pipelines), Brevo HTTPS API |
| **Testing & QA** | .NET verification suites (`CoreModelChecks`, `CloudinaryImageServiceChecks`, `WorkflowIntegrationChecks`), Playwright E2E suite |
| **Deployment & CI/CD** | Docker (Node 22 + .NET 10 multi-stage build), Render, GitHub Actions |

### Architecture

```text
                             Browser
             Razor Views + Tailwind CSS v4 + Vanilla JS
                                |
                   HTTPS MVC Controllers +
                 ASP.NET Core Identity / OAuth 2.0
                                |
                  ASP.NET Core 10 (SkillBridge.Core)
                   /            |              \
      EF Core 10 + Npgsql   Cloudinary    Brevo HTTPS API
             |              (Media CDN)  (Verification / Reset)
     PostgreSQL on Supabase
       (skillbridge schema)

          Browser <---- SignalR (/chatHub) ----> SkillBridge.Core
                  (real-time chat & badge updates)
```

## Project Structure

```text
SkillBridge/
├── SkillBridge.Core/
│   ├── Controllers/         # MVC controllers (Account, Explore, Interactions, Messages, Communities, etc.)
│   ├── Helpers/             # AES/HMAC encryption, search ranking, block rules, progress & hub helpers
│   ├── Hubs/                # SignalR Core /chatHub for real-time messaging and badge updates
│   ├── Models/              # EF Core entities, Identity models, and view models
│   ├── Services/            # Brevo email sender, Cloudinary media service, rate-limit policies
│   ├── Styles/              # Tailwind CSS v4 entry (tailwind.css) and shared design system CSS
│   ├── Views/               # Razor views across feature folders and shared layout/partials
│   ├── certs/               # Bundled Supabase root CA certificate (prod-ca-2021.crt)
│   ├── wwwroot/             # Compiled stylesheets, client scripts, and static assets
│   ├── Program.cs           # App startup, DI, TLS PostgreSQL setup, rate limiting, SignalR & routing
│   └── SkillBridge.Core.csproj
│
├── database/
│   └── postgres/            # Numbered manual PostgreSQL migrations (001–012) & optional demo seed/rollback
│
├── tests/
│   ├── CoreModelChecks/              # Offline EF model, search, progress & helper checks
│   ├── CloudinaryImageServiceChecks/ # Offline image validation, signed URL & encryption checks
│   ├── WorkflowIntegrationChecks/    # Opt-in live-DB multi-step exchange & concurrency checks
│   └── e2e/                          # Playwright end-to-end & multi-viewport audit specs
│
├── .github/
│   └── workflows/           # Render deployment tracking & live smoke-check workflow
├── Dockerfile               # Multi-stage Node 22 CSS build + .NET 10 publish (port 10000)
├── DEPLOYMENT_CORE.md       # Production Render, Supabase, Brevo, Google OAuth & Cloudinary runbook
├── package.json             # Tailwind CSS v4 build/watch scripts & Playwright CLI
├── playwright.config.js     # Playwright E2E configuration
├── LICENSE
└── README.md
```

## Getting Started

### Prerequisites

- **.NET 10 SDK** (or the workspace-local SDK if present at `.\.dotnet\dotnet.exe`)
- **Node.js** (20+ / 22 LTS) and npm (for Tailwind CSS v4 compilation and Playwright)
- Access to a **PostgreSQL database** (such as Supabase PostgreSQL) with TLS enabled
- *(Optional for full features)* **Cloudinary**, **Brevo**, and **Google OAuth 2.0** credentials

### 1. Clone the repository

```bash
git clone https://github.com/kazi-kamruddin/SkillBridge.git
cd SkillBridge
```

### 2. Set up the PostgreSQL database

SkillBridge manages database schema changes exclusively with numbered SQL scripts in `database/postgres/` — EF Core does not run automatic migrations at startup.

- **New empty database:** Run `001_initial.sql` through `010_meeting_history.sql`, then `012_one_way_mentorships.sql` in order in your PostgreSQL / Supabase SQL Editor. **Never** rerun `001_initial.sql` on an existing database.
- **Existing database upgrade:** Run only the remaining numbered scripts in strict sequence (`003` → `010` → `012`).
- **Optional demo data:** After `012_one_way_mentorships.sql`, run `011_demo_seed.sql` to populate showcase personas and sample exchanges (or `011_demo_rollback.sql` to remove demo rows cleanly).

### 3. Build the stylesheet

From the repository root:

```bash
npm ci
npm run build:css
```

This compiles `SkillBridge.Core/Styles/tailwind.css` into the minified stylesheet at `SkillBridge.Core/wwwroot/Content/tailwind-app.css`. During active UI development, run `npm run watch:css`.

> **PowerShell note:** If your workspace uses a local npm cache, run `$env:npm_config_cache = (Join-Path (Get-Location).Path '.npm-cache')` before `npm ci`.

### 4. Configure environment variables

Set server-side environment variables outside Git before launching `SkillBridge.Core`.

**Required:**

```dotenv
SKILLBRIDGE_DB_CONNECTION=Host=your-supabase-pooler-host;Port=5432;Database=postgres;Username=your-user;Password=your-password
SKILLBRIDGE_MESSAGE_KEY=replace-with-a-base64-encoded-32-byte-aes-key
```

The app verifies PostgreSQL TLS (`SslMode.VerifyFull`) using the bundled `SkillBridge.Core/certs/prod-ca-2021.crt` certificate by default.

**Optional feature configuration:**

| Variable | Purpose |
| --- | --- |
| `SKILLBRIDGE_PUBLIC_URL` | Base public origin (`https://.../`) used in confirmation and password-reset links. |
| `SKILLBRIDGE_DB_CA_CERT` | Overrides the bundled Supabase root CA certificate path if rotated. |
| `SKILLBRIDGE_BREVO_API_KEY` | Brevo HTTPS transactional email API key (verification & password resets). |
| `SKILLBRIDGE_BREVO_FROM` | Verified Brevo sender email address. |
| `SKILLBRIDGE_GOOGLE_CLIENT_ID` | Google Web OAuth client ID (enables Google sign-in buttons). |
| `SKILLBRIDGE_GOOGLE_CLIENT_SECRET` | Matching Google Web OAuth client secret. |
| `SKILLBRIDGE_CLOUDINARY_CLOUD_NAME` | Cloudinary cloud name for avatars, community media, and chat images. |
| `SKILLBRIDGE_CLOUDINARY_API_KEY` | Cloudinary API key. |
| `SKILLBRIDGE_CLOUDINARY_API_SECRET` | Cloudinary API secret. |
| `SKILLBRIDGE_MODERATOR_EMAIL` | Existing member email granted `/Moderation` and `/Catalog/Suggestions` access. |
| `SKILLBRIDGE_SUPPORT_EMAIL` | Optional support address shown on the Contact page. |

> **Note:** When Brevo or `SKILLBRIDGE_PUBLIC_URL` is not configured locally, new email/password registrations auto-confirm for local testing, while password reset stays safely disabled. Google sign-in buttons appear only when both Google OAuth variables are set. Never commit secrets, keys, or connection strings to Git.

### 5. Run the application locally

```bash
dotnet run --project SkillBridge.Core/SkillBridge.Core.csproj
```

*(If using the local workspace SDK on Windows, replace `dotnet` with `.\.dotnet\dotnet.exe`.)*

### 6. Build for production

```bash
npm run build:css
dotnet publish SkillBridge.Core/SkillBridge.Core.csproj -c Release
```

### 7. Running automated tests

**Offline .NET verification checks (no database or Cloudinary account required):**

```bash
dotnet run --project tests/CoreModelChecks/CoreModelChecks.csproj
dotnet run --project tests/CloudinaryImageServiceChecks/CloudinaryImageServiceChecks.csproj
```

**Live-database integration checks (opt-in):**

```bash
# Requires SKILLBRIDGE_DB_CONNECTION, SKILLBRIDGE_TEST_ACCOUNTS_FILE, and SKILLBRIDGE_MUTATING_TESTS=1
dotnet run --project tests/WorkflowIntegrationChecks/WorkflowIntegrationChecks.csproj
```

**End-to-end tests (Playwright):**

```bash
npx playwright install chromium
# Point SKILLBRIDGE_BASE_URL to your running local or deployed instance
npm run test:e2e
```

Authenticated and mutating browser suites (`tests/e2e/member.spec.js` and `tests/e2e/completion-workflows.spec.js`) read disposable credentials from `SKILLBRIDGE_TEST_ACCOUNTS_FILE` and require `SKILLBRIDGE_MUTATING_TESTS=1` (plus `SKILLBRIDGE_MEDIA_TESTS=1` for Cloudinary upload checks).

## Deployment

The live application is hosted at **[skillbridge-08v4.onrender.com](https://skillbridge-08v4.onrender.com/)**.

- **Web Service (Render + Docker):** The root multi-stage `Dockerfile` compiles Tailwind CSS v4 with Node 22, publishes `SkillBridge.Core` on .NET 10, binds to port `10000`, exposes `/health`, and persists ASP.NET Data Protection keys in PostgreSQL across container restarts.
- **Database (Supabase PostgreSQL):** Connects over TLS (`VerifyFull`) via session pooler; numbered SQL scripts in `database/postgres/` must be applied manually prior to deploying dependent releases.
- **Media & Email:** Cloudinary stores avatars, community attachments, and private authenticated chat images (avoiding ephemeral container disk storage), while Brevo handles transactional emails over HTTPS.
- **CI/CD:** `.github/workflows/production-deployment.yml` tracks Render deployments on pushes to `main` and runs live smoke checks against `/health`, `/Explore/Skills`, and `/Communities`.

For complete environment setup, rate-limit behavior, OAuth callback configuration, and free-tier caveats, see **[DEPLOYMENT_CORE.md](DEPLOYMENT_CORE.md)**.

## Development Team

| Team member | Role |
| --- | --- |
| **Kazi Kamruddin Ahmed** | Project Leader |
| **Rakibul Islam Rahi** | Frontend |
| **Abdullah Ishtiaq** | Testing |

## Acknowledgments

Supervised by **Ms. Tanjila Broti** and **Ms. Tahsina Muthaki**.

## License

This repository is licensed under the **[MIT License](LICENSE)**.

---

<div align="center">

**SkillBridge — skills are better when shared.**

[Explore SkillBridge](https://skillbridge-08v4.onrender.com/) · [View Source](https://github.com/kazi-kamruddin/SkillBridge)

</div>
