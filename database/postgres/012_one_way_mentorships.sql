-- Apply once after 010_meeting_history.sql (and 011_demo_seed.sql if used), before deploying Phase 2.
-- Allows one-way mentorship interactions where the requester learns from the teacher
-- without offering a reciprocal skill in return ("SkillFromRequesterId" IS NULL).
--
-- Rollback (only safe when no rows have "SkillFromRequesterId" IS NULL):
--   BEGIN;
--   SET search_path TO skillbridge, public;
--   ALTER TABLE skillbridge."Interactions" ALTER COLUMN "SkillFromRequesterId" SET NOT NULL;
--   COMMIT;

BEGIN;

SET search_path TO skillbridge, public;

ALTER TABLE skillbridge."Interactions"
    ALTER COLUMN "SkillFromRequesterId" DROP NOT NULL;

COMMIT;
