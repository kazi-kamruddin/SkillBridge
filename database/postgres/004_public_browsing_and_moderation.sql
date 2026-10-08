-- Apply once in Supabase SQL Editor before deploying the public-browsing update.
-- Existing profiles remain private to signed-out visitors until members opt in.
BEGIN;

ALTER TABLE skillbridge."UserInformations"
    ADD COLUMN IF NOT EXISTS "IsPublic" boolean NOT NULL DEFAULT false;

ALTER TABLE skillbridge."CommunityPosts"
    ADD COLUMN IF NOT EXISTS "IsHidden" boolean NOT NULL DEFAULT false;
ALTER TABLE skillbridge."CommunityComments"
    ADD COLUMN IF NOT EXISTS "IsHidden" boolean NOT NULL DEFAULT false;

CREATE TABLE IF NOT EXISTS skillbridge."CommunityReports" (
    "Id" serial PRIMARY KEY,
    "ReporterId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers" ("Id"),
    "PostId" integer REFERENCES skillbridge."CommunityPosts" ("Id") ON DELETE CASCADE,
    "CommentId" integer REFERENCES skillbridge."CommunityComments" ("Id") ON DELETE CASCADE,
    "Reason" varchar(500) NOT NULL,
    "Status" varchar(20) NOT NULL DEFAULT 'Pending',
    "CreatedAt" timestamp without time zone NOT NULL,
    CONSTRAINT "CK_CommunityReports_OneTarget" CHECK
        (("PostId" IS NOT NULL) <> ("CommentId" IS NOT NULL)),
    CONSTRAINT "CK_CommunityReports_Status" CHECK
        ("Status" IN ('Pending', 'Dismissed', 'Hidden'))
);
CREATE INDEX IF NOT EXISTS "IX_CommunityReports_Status_CreatedAt"
    ON skillbridge."CommunityReports" ("Status", "CreatedAt" DESC);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_CommunityReports_Reporter_Post"
    ON skillbridge."CommunityReports" ("ReporterId", "PostId") WHERE "PostId" IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS "IX_CommunityReports_Reporter_Comment"
    ON skillbridge."CommunityReports" ("ReporterId", "CommentId") WHERE "CommentId" IS NOT NULL;

COMMIT;
