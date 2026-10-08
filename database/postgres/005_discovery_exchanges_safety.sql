-- Apply once after 004_public_browsing_and_moderation.sql, before deploying issue #36.
BEGIN;

ALTER TABLE skillbridge."SkillRequests"
    ADD COLUMN IF NOT EXISTS "Goal" varchar(500),
    ADD COLUMN IF NOT EXISTS "Pace" varchar(100),
    ADD COLUMN IF NOT EXISTS "FirstMeetingIdea" varchar(300);

ALTER TABLE skillbridge."Interactions"
    ADD COLUMN IF NOT EXISTS "EndReason" varchar(500),
    ADD COLUMN IF NOT EXISTS "EndedByUserId" varchar(128),
    ADD COLUMN IF NOT EXISTS "EndedAt" timestamp without time zone;

ALTER TABLE skillbridge."UserInformations"
    ADD COLUMN IF NOT EXISTS "IsHidden" boolean NOT NULL DEFAULT false;

CREATE TABLE IF NOT EXISTS skillbridge."MemberBlocks" (
    "Id" serial PRIMARY KEY,
    "BlockerId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers"("Id") ON DELETE CASCADE,
    "BlockedId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers"("Id") ON DELETE CASCADE,
    "CreatedAt" timestamp without time zone NOT NULL,
    CONSTRAINT "CK_MemberBlocks_DifferentUsers" CHECK ("BlockerId" <> "BlockedId"),
    CONSTRAINT "UQ_MemberBlocks_Pair" UNIQUE ("BlockerId", "BlockedId")
);
CREATE INDEX IF NOT EXISTS "IX_MemberBlocks_BlockedId" ON skillbridge."MemberBlocks"("BlockedId");

CREATE TABLE IF NOT EXISTS skillbridge."ProfileReports" (
    "Id" serial PRIMARY KEY,
    "ReporterId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers"("Id") ON DELETE CASCADE,
    "ReportedUserId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers"("Id") ON DELETE CASCADE,
    "Reason" varchar(500) NOT NULL,
    "Status" varchar(20) NOT NULL DEFAULT 'Pending',
    "CreatedAt" timestamp without time zone NOT NULL,
    CONSTRAINT "CK_ProfileReports_DifferentUsers" CHECK ("ReporterId" <> "ReportedUserId"),
    CONSTRAINT "CK_ProfileReports_Status" CHECK ("Status" IN ('Pending', 'Reviewed', 'Dismissed')),
    CONSTRAINT "UQ_ProfileReports_Pair" UNIQUE ("ReporterId", "ReportedUserId")
);
CREATE INDEX IF NOT EXISTS "IX_ProfileReports_Status_CreatedAt"
    ON skillbridge."ProfileReports"("Status", "CreatedAt" DESC);

COMMIT;
