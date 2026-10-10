-- Apply once after 009, before deploying meeting history.
BEGIN;

CREATE TABLE skillbridge."InteractionMeetingEvents" (
    "Id" serial PRIMARY KEY,
    "InteractionId" integer NOT NULL REFERENCES skillbridge."Interactions"("Id") ON DELETE CASCADE,
    "EventType" varchar(24) NOT NULL,
    "StartsAtUtc" timestamp without time zone NOT NULL,
    "Format" varchar(20),
    "Note" varchar(300),
    "ActorUserId" varchar(128),
    "CreatedAt" timestamp without time zone NOT NULL
);
CREATE INDEX "IX_InteractionMeetingEvents_InteractionId_CreatedAt"
    ON skillbridge."InteractionMeetingEvents"("InteractionId", "CreatedAt");

-- Earlier meeting changes were overwritten. Preserve each exchange's latest saved state.
INSERT INTO skillbridge."InteractionMeetingEvents"
    ("InteractionId", "EventType", "StartsAtUtc", "Format", "Note", "ActorUserId", "CreatedAt")
SELECT "Id", 'Earlier saved state', "MeetingStartUtc",
    "MeetingFormat", "MeetingNote", "MeetingProposedByUserId", COALESCE("EndedAt", "CreatedAt")
FROM skillbridge."Interactions"
WHERE "MeetingStartUtc" IS NOT NULL;

COMMIT;
