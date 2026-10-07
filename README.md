# SkillBridge

SkillBridge is a peer-to-peer skill exchange website. Members list skills they can teach and learn, find reciprocal matches, request exchanges, confirm seven learning stages, rate completed exchanges, message one another, and post in skill communities.

## Technology

- ASP.NET MVC 5 and Razor views on .NET Framework 4.7.2
- Entity Framework 6 and ASP.NET Identity 2
- PostgreSQL through Npgsql (Supabase is the intended managed database)
- SignalR 2 for live chat, Bootstrap and jQuery for the interface

The frontend and backend are one web application. Supabase Auth and the Supabase browser Data API are not used; the server connects to PostgreSQL directly.

## Run locally

1. On Windows, install Visual Studio with the ASP.NET and web development workload and the .NET Framework 4.7.2 targeting pack.
2. Restore the NuGet packages in `SkillBridge.sln` and build the solution.
3. Create a new Supabase project. In its SQL Editor, run `database/postgres/001_initial.sql` and then `database/postgres/002_seed.sql`. These scripts create the app tables in the private `skillbridge` schema. Do not expose that schema in Supabase API settings.
4. Set the `SKILLBRIDGE_DB_CONNECTION` environment variable for the web app process. Use the connection details from Supabase's **Connect** panel, not a connection string committed to Git. A persistent server can use the direct connection if it supports IPv6; otherwise use the session pooler on port 5432. Use `SSL Mode=Require` at minimum. Example shape: `Host=<pooler-host>;Port=5432;Database=postgres;Username=postgres.<project-ref>;Password=<password>;SSL Mode=Require`.
5. Set `SKILLBRIDGE_MESSAGE_KEY` to a stable, random 32-byte key encoded as Base64. Keep it secret and backed up; changing it makes existing messages unreadable.
6. Start the MVC application with IIS Express or IIS.

The database scripts are for a **new, empty** PostgreSQL database. Existing SQL Server data needs a separate data migration; the historical SQL Server migrations in `Migrations/` are retained for reference and are excluded from the PostgreSQL build. Schema changes after this baseline should be added as numbered PostgreSQL SQL scripts and reviewed before running. The app does not modify its schema at startup.

## Current development limits

- The database connection and a two-user end-to-end flow still need verification against a real PostgreSQL project.
- Password reset email delivery is not implemented.
- The `Contact` page and some footer links still have placeholder content.
- Legacy ASP.NET MVC 5 requires Windows hosting or a compatible Windows container.
