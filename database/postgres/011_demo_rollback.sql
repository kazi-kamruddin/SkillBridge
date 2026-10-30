-- =============================================================================
-- SkillBridge Demo Seed Data Rollback Script (011_demo_rollback.sql)
-- Safely removes all demo personas (demo-usr-01 through demo-usr-08) and their
-- associated interactions, sessions, ratings, posts, comments, and skills
-- without affecting real registered users.
-- =============================================================================
BEGIN;

SET search_path TO skillbridge, public;

-- 1. Remove notifications referencing or belonging to demo users
DELETE FROM skillbridge."Notifications"
WHERE "UserId" LIKE 'demo-usr-%'
   OR "ReferenceId" IN (
       SELECT "Id" FROM skillbridge."Interactions"
       WHERE "User1Id" LIKE 'demo-usr-%' OR "User2Id" LIKE 'demo-usr-%'
   );

-- 2. Remove ratings given or received by demo users
DELETE FROM skillbridge."Ratings"
WHERE "FromUserId" LIKE 'demo-usr-%' OR "ToUserId" LIKE 'demo-usr-%';

-- 3. Remove session notes, meeting events, plan proposals, and sessions
DELETE FROM skillbridge."InteractionSessionNotes"
WHERE "UserId" LIKE 'demo-usr-%'
   OR "InteractionSessionId" IN (
       SELECT s."Id" FROM skillbridge."InteractionSessions" s
       JOIN skillbridge."Interactions" i ON i."Id" = s."InteractionId"
       WHERE i."User1Id" LIKE 'demo-usr-%' OR i."User2Id" LIKE 'demo-usr-%'
   );

DELETE FROM skillbridge."MeetingEvents"
WHERE "ActorUserId" LIKE 'demo-usr-%'
   OR "InteractionId" IN (
       SELECT "Id" FROM skillbridge."Interactions"
       WHERE "User1Id" LIKE 'demo-usr-%' OR "User2Id" LIKE 'demo-usr-%'
   );

DELETE FROM skillbridge."InteractionPlanProposals"
WHERE "ProposedByUserId" LIKE 'demo-usr-%'
   OR "InteractionId" IN (
       SELECT "Id" FROM skillbridge."Interactions"
       WHERE "User1Id" LIKE 'demo-usr-%' OR "User2Id" LIKE 'demo-usr-%'
   );

DELETE FROM skillbridge."InteractionSessions"
WHERE "InteractionId" IN (
    SELECT "Id" FROM skillbridge."Interactions"
    WHERE "User1Id" LIKE 'demo-usr-%' OR "User2Id" LIKE 'demo-usr-%'
);

-- 4. Remove interactions and skill requests
DELETE FROM skillbridge."Interactions"
WHERE "User1Id" LIKE 'demo-usr-%' OR "User2Id" LIKE 'demo-usr-%';

DELETE FROM skillbridge."SkillRequests"
WHERE "RequesterId" LIKE 'demo-usr-%' OR "ReceiverId" LIKE 'demo-usr-%';

-- 5. Remove messages and conversations
DELETE FROM skillbridge."Messages"
WHERE "FromUserId" LIKE 'demo-usr-%' OR "ToUserId" LIKE 'demo-usr-%';

DELETE FROM skillbridge."Conversations"
WHERE "User1Id" LIKE 'demo-usr-%' OR "User2Id" LIKE 'demo-usr-%';

-- 6. Remove community reports, comments, and posts
DELETE FROM skillbridge."CommunityReports"
WHERE "ReporterId" LIKE 'demo-usr-%';

DELETE FROM skillbridge."CommunityComments"
WHERE "CreatedByUserId" LIKE 'demo-usr-%';

DELETE FROM skillbridge."CommunityPosts"
WHERE "CreatedByUserId" LIKE 'demo-usr-%';

-- 7. Remove saved profiles, blocks, profile reports, and skill suggestions
DELETE FROM skillbridge."SavedProfiles"
WHERE "UserId" LIKE 'demo-usr-%' OR "TargetUserId" LIKE 'demo-usr-%';

DELETE FROM skillbridge."MemberBlocks"
WHERE "BlockerId" LIKE 'demo-usr-%' OR "BlockedId" LIKE 'demo-usr-%';

DELETE FROM skillbridge."ProfileReports"
WHERE "ReporterId" LIKE 'demo-usr-%' OR "ReportedUserId" LIKE 'demo-usr-%';

DELETE FROM skillbridge."SkillSuggestions"
WHERE "UserId" LIKE 'demo-usr-%';

-- 8. Remove user skills, ratings, profile info, and identity accounts
DELETE FROM skillbridge."UserSkills"
WHERE "UserId" LIKE 'demo-usr-%';

DELETE FROM skillbridge."UserRatings"
WHERE "UserId" LIKE 'demo-usr-%';

DELETE FROM skillbridge."UserInformations"
WHERE "UserId" LIKE 'demo-usr-%';

DELETE FROM skillbridge."AspNetUsers"
WHERE "Id" LIKE 'demo-usr-%';

COMMIT;
