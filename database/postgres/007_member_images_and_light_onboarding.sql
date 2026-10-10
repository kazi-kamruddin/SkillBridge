-- Apply once after 006_member_access_and_planning.sql, before deploying this release.
BEGIN;
ALTER TABLE skillbridge."UserInformations"
    ALTER COLUMN "Age" DROP NOT NULL,
    ALTER COLUMN "Profession" DROP NOT NULL,
    ALTER COLUMN "Location" DROP NOT NULL,
    ALTER COLUMN "Bio" DROP NOT NULL,
    ADD COLUMN IF NOT EXISTS "ProfileImageUrl" varchar(500),
    ADD COLUMN IF NOT EXISTS "ProfileImagePublicId" varchar(500);
ALTER TABLE skillbridge."CommunityPosts" ADD COLUMN IF NOT EXISTS "ImageUrl" varchar(500);
ALTER TABLE skillbridge."CommunityComments" ADD COLUMN IF NOT EXISTS "ImageUrl" varchar(500);
ALTER TABLE skillbridge."Messages"
    ADD COLUMN IF NOT EXISTS "ImagePublicId" varchar(500),
    ADD COLUMN IF NOT EXISTS "ImageFormat" varchar(20);
COMMIT;
