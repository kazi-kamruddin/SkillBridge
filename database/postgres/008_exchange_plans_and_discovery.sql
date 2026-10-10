-- Apply once after 007_member_images_and_light_onboarding.sql, before deploying this release.
BEGIN;

ALTER TABLE skillbridge."UserInformations"
    ADD COLUMN "AvailableDaysMask" integer NOT NULL DEFAULT 0
        CHECK ("AvailableDaysMask" BETWEEN 0 AND 127),
    ADD COLUMN "TimeZoneId" varchar(80);

-- The earlier catalog, profile and exchange checks assumed every skill had seven stages.
DO $$
DECLARE constraint_name text;
BEGIN
    FOR constraint_name IN
        SELECT conname FROM pg_constraint
        WHERE conrelid = 'skillbridge."SkillStages"'::regclass AND contype = 'c'
          AND pg_get_constraintdef(oid) LIKE '%StageNumber%'
    LOOP
        EXECUTE format('ALTER TABLE skillbridge."SkillStages" DROP CONSTRAINT %I', constraint_name);
    END LOOP;
    FOR constraint_name IN
        SELECT conname FROM pg_constraint
        WHERE conrelid = 'skillbridge."UserSkills"'::regclass AND contype = 'c'
          AND pg_get_constraintdef(oid) LIKE '%KnownUpToStage%'
    LOOP
        EXECUTE format('ALTER TABLE skillbridge."UserSkills" DROP CONSTRAINT %I', constraint_name);
    END LOOP;
    FOR constraint_name IN
        SELECT conname FROM pg_constraint
        WHERE conrelid = 'skillbridge."InteractionSessions"'::regclass AND contype = 'c'
          AND pg_get_constraintdef(oid) LIKE '%StageNumber%'
    LOOP
        EXECUTE format('ALTER TABLE skillbridge."InteractionSessions" DROP CONSTRAINT %I', constraint_name);
    END LOOP;
END $$;

ALTER TABLE skillbridge."SkillStages"
    ADD CONSTRAINT "CK_SkillStages_StageNumber" CHECK ("StageNumber" BETWEEN 1 AND 12);
ALTER TABLE skillbridge."UserSkills"
    ADD CONSTRAINT "CK_UserSkills_KnownUpToStage" CHECK ("KnownUpToStage" BETWEEN 0 AND 12);
ALTER TABLE skillbridge."InteractionSessions"
    ADD CONSTRAINT "CK_InteractionSessions_StageNumber" CHECK ("StageNumber" > 0),
    ADD COLUMN "Title" varchar(200),
    ADD COLUMN "CatalogStageNumber" integer;

UPDATE skillbridge."InteractionSessions" AS session
SET "Title" = stage."Description", "CatalogStageNumber" = session."StageNumber"
FROM skillbridge."SkillStages" AS stage
WHERE stage."SkillId" = session."SkillId" AND stage."StageNumber" = session."StageNumber";

CREATE TABLE skillbridge."InteractionPlanProposals" (
    "InteractionId" integer NOT NULL REFERENCES skillbridge."Interactions"("Id") ON DELETE CASCADE,
    "SkillId" integer NOT NULL REFERENCES skillbridge."Skills"("Id"),
    "ProposedByUserId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers"("Id"),
    "StepsJson" text NOT NULL,
    "OriginalStepsJson" text NOT NULL,
    "CreatedAt" timestamp without time zone NOT NULL,
    PRIMARY KEY ("InteractionId", "SkillId")
);

COMMIT;
