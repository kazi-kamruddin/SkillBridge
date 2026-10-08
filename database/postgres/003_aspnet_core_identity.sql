-- Run once after 001_initial.sql (and the optional 002_seed.sql) when moving
-- an existing SkillBridge database from ASP.NET Identity 2 to ASP.NET Core.
-- Existing accounts, password hashes, application data and encrypted messages
-- are retained. Apply this in the Supabase SQL Editor before starting Core.
BEGIN;

ALTER TABLE skillbridge."AspNetUsers"
    ADD COLUMN IF NOT EXISTS "NormalizedUserName" varchar(256),
    ADD COLUMN IF NOT EXISTS "NormalizedEmail" varchar(256),
    ADD COLUMN IF NOT EXISTS "ConcurrencyStamp" text,
    ADD COLUMN IF NOT EXISTS "LockoutEnd" timestamp with time zone;

UPDATE skillbridge."AspNetUsers"
SET "NormalizedUserName" = upper("UserName"),
    "NormalizedEmail" = upper("Email"),
    "ConcurrencyStamp" = COALESCE("ConcurrencyStamp", md5(random()::text || clock_timestamp()::text)),
    "LockoutEnd" = COALESCE("LockoutEnd", "LockoutEndDateUtc" AT TIME ZONE 'UTC');

CREATE UNIQUE INDEX IF NOT EXISTS "UserNameIndexCore"
    ON skillbridge."AspNetUsers" ("NormalizedUserName");
CREATE INDEX IF NOT EXISTS "EmailIndexCore"
    ON skillbridge."AspNetUsers" ("NormalizedEmail");

ALTER TABLE skillbridge."AspNetRoles"
    ADD COLUMN IF NOT EXISTS "NormalizedName" varchar(256),
    ADD COLUMN IF NOT EXISTS "ConcurrencyStamp" text;
UPDATE skillbridge."AspNetRoles"
SET "NormalizedName" = upper("Name"),
    "ConcurrencyStamp" = COALESCE("ConcurrencyStamp", md5(random()::text || clock_timestamp()::text));
CREATE UNIQUE INDEX IF NOT EXISTS "RoleNameIndexCore"
    ON skillbridge."AspNetRoles" ("NormalizedName");

ALTER TABLE skillbridge."AspNetUserLogins"
    ADD COLUMN IF NOT EXISTS "ProviderDisplayName" text;

CREATE TABLE IF NOT EXISTS skillbridge."AspNetUserTokens" (
    "UserId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers" ("Id") ON DELETE CASCADE,
    "LoginProvider" varchar(128) NOT NULL,
    "Name" varchar(128) NOT NULL,
    "Value" text,
    PRIMARY KEY ("UserId", "LoginProvider", "Name")
);

CREATE TABLE IF NOT EXISTS skillbridge."AspNetRoleClaims" (
    "Id" serial PRIMARY KEY,
    "RoleId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetRoles" ("Id") ON DELETE CASCADE,
    "ClaimType" text,
    "ClaimValue" text
);
CREATE INDEX IF NOT EXISTS "IX_AspNetRoleClaims_RoleId"
    ON skillbridge."AspNetRoleClaims" ("RoleId");

-- Keep login cookies and password-reset tokens valid across Render restarts.
CREATE TABLE IF NOT EXISTS skillbridge."DataProtectionKeys" (
    "Id" serial PRIMARY KEY,
    "FriendlyName" text,
    "Xml" text
);

COMMIT;
