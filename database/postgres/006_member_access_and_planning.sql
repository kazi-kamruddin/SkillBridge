-- Apply once after 005_discovery_exchanges_safety.sql, before deploying this release.
-- Existing accounts retain access; new email/password accounts must verify their email.
BEGIN;

-- Grandfather accounts that existed before this release. The column marker keeps
-- a rerun from confirming accounts registered after email verification launched.
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'skillbridge' AND table_name = 'UserInformations'
          AND column_name = 'AvailabilityNotes'
    ) THEN
        UPDATE skillbridge."AspNetUsers" SET "EmailConfirmed" = true
        WHERE "EmailConfirmed" = false;
    END IF;
END $$;

ALTER TABLE skillbridge."UserInformations"
    ADD COLUMN IF NOT EXISTS "AvailabilityNotes" varchar(300),
    ADD COLUMN IF NOT EXISTS "MeetingFormat" varchar(20) NOT NULL DEFAULT 'Either';

ALTER TABLE skillbridge."Interactions"
    ADD COLUMN IF NOT EXISTS "MeetingStartUtc" timestamp without time zone,
    ADD COLUMN IF NOT EXISTS "MeetingFormat" varchar(20),
    ADD COLUMN IF NOT EXISTS "MeetingNote" varchar(300),
    ADD COLUMN IF NOT EXISTS "MeetingProposedByUserId" varchar(128),
    ADD COLUMN IF NOT EXISTS "MeetingStatus" varchar(20);

CREATE TABLE IF NOT EXISTS skillbridge."SavedProfiles" (
    "UserId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers"("Id") ON DELETE CASCADE,
    "TargetUserId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers"("Id") ON DELETE CASCADE,
    "CreatedAt" timestamp without time zone NOT NULL,
    PRIMARY KEY ("UserId", "TargetUserId"),
    CONSTRAINT "CK_SavedProfiles_DifferentUsers" CHECK ("UserId" <> "TargetUserId")
);
CREATE INDEX IF NOT EXISTS "IX_SavedProfiles_TargetUserId" ON skillbridge."SavedProfiles"("TargetUserId");

CREATE TABLE IF NOT EXISTS skillbridge."InteractionSessionNotes" (
    "Id" serial PRIMARY KEY,
    "InteractionSessionId" integer NOT NULL REFERENCES skillbridge."InteractionSessions"("Id") ON DELETE CASCADE,
    "UserId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers"("Id"),
    "WhatWeCovered" varchar(1000) NOT NULL,
    "NextStep" varchar(500),
    "UpdatedAt" timestamp without time zone NOT NULL,
    CONSTRAINT "UQ_InteractionSessionNotes_SessionUser" UNIQUE ("InteractionSessionId", "UserId")
);
CREATE INDEX IF NOT EXISTS "IX_InteractionSessionNotes_UserId" ON skillbridge."InteractionSessionNotes"("UserId");

COMMIT;
