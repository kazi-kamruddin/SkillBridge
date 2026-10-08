-- Run once in the Supabase SQL Editor against a new, empty project.
-- SkillBridge uses its own schema so ASP.NET Identity data is not exposed
-- through Supabase's public Data API. The application connects directly to Postgres.
BEGIN;

CREATE SCHEMA skillbridge;
REVOKE ALL ON SCHEMA skillbridge FROM PUBLIC, anon, authenticated, service_role;

CREATE TABLE skillbridge."AspNetUsers" (
    "Id" varchar(128) PRIMARY KEY,
    "Email" varchar(256),
    "EmailConfirmed" boolean NOT NULL,
    "PasswordHash" text,
    "SecurityStamp" text,
    "PhoneNumber" text,
    "PhoneNumberConfirmed" boolean NOT NULL,
    "TwoFactorEnabled" boolean NOT NULL,
    "LockoutEndDateUtc" timestamp without time zone,
    "LockoutEnabled" boolean NOT NULL,
    "AccessFailedCount" integer NOT NULL,
    "UserName" varchar(256) NOT NULL
);
CREATE UNIQUE INDEX "UserNameIndex" ON skillbridge."AspNetUsers" ("UserName");
CREATE INDEX "IX_AspNetUsers_Email" ON skillbridge."AspNetUsers" ("Email");

CREATE TABLE skillbridge."AspNetRoles" (
    "Id" varchar(128) PRIMARY KEY,
    "Name" varchar(256) NOT NULL
);
CREATE UNIQUE INDEX "RoleNameIndex" ON skillbridge."AspNetRoles" ("Name");

CREATE TABLE skillbridge."AspNetUserRoles" (
    "UserId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers" ("Id") ON DELETE CASCADE,
    "RoleId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetRoles" ("Id") ON DELETE CASCADE,
    PRIMARY KEY ("UserId", "RoleId")
);
CREATE INDEX "IX_AspNetUserRoles_RoleId" ON skillbridge."AspNetUserRoles" ("RoleId");

CREATE TABLE skillbridge."AspNetUserClaims" (
    "Id" serial PRIMARY KEY,
    "UserId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers" ("Id") ON DELETE CASCADE,
    "ClaimType" text,
    "ClaimValue" text
);
CREATE INDEX "IX_AspNetUserClaims_UserId" ON skillbridge."AspNetUserClaims" ("UserId");

CREATE TABLE skillbridge."AspNetUserLogins" (
    "LoginProvider" varchar(128) NOT NULL,
    "ProviderKey" varchar(128) NOT NULL,
    "UserId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers" ("Id") ON DELETE CASCADE,
    PRIMARY KEY ("LoginProvider", "ProviderKey", "UserId")
);
CREATE INDEX "IX_AspNetUserLogins_UserId" ON skillbridge."AspNetUserLogins" ("UserId");

CREATE TABLE skillbridge."SkillCategories" (
    "Id" serial PRIMARY KEY,
    "Name" varchar(100) NOT NULL UNIQUE,
    "Description" text
);
CREATE TABLE skillbridge."Skills" (
    "Id" serial PRIMARY KEY,
    "Name" varchar(100) NOT NULL UNIQUE,
    "Description" text,
    "SkillCategoryId" integer NOT NULL REFERENCES skillbridge."SkillCategories" ("Id") ON DELETE CASCADE
);
CREATE INDEX "IX_Skills_SkillCategoryId" ON skillbridge."Skills" ("SkillCategoryId");
CREATE TABLE skillbridge."SkillStages" (
    "Id" serial PRIMARY KEY,
    "StageNumber" integer NOT NULL CHECK ("StageNumber" BETWEEN 1 AND 7),
    "Description" varchar(200) NOT NULL,
    "SkillId" integer NOT NULL REFERENCES skillbridge."Skills" ("Id") ON DELETE CASCADE,
    UNIQUE ("SkillId", "StageNumber")
);

CREATE TABLE skillbridge."UserInformations" (
    "UserId" varchar(128) PRIMARY KEY REFERENCES skillbridge."AspNetUsers" ("Id"),
    "FullName" varchar(150) NOT NULL,
    "Age" integer NOT NULL CHECK ("Age" BETWEEN 1 AND 150),
    "Profession" varchar(100) NOT NULL,
    "Location" varchar(100) NOT NULL,
    "Bio" varchar(500) NOT NULL
);
CREATE TABLE skillbridge."UserSkills" (
    "Id" serial PRIMARY KEY,
    "UserId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers" ("Id") ON DELETE CASCADE,
    "SkillId" integer NOT NULL REFERENCES skillbridge."Skills" ("Id") ON DELETE CASCADE,
    "Status" varchar(10) NOT NULL,
    "KnownUpToStage" integer CHECK ("KnownUpToStage" BETWEEN 0 AND 7)
);
CREATE INDEX "IX_UserSkills_UserId" ON skillbridge."UserSkills" ("UserId");
CREATE INDEX "IX_UserSkills_SkillId" ON skillbridge."UserSkills" ("SkillId");

CREATE TABLE skillbridge."SkillRequests" (
    "Id" serial PRIMARY KEY,
    "RequesterId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers" ("Id"),
    "ReceiverId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers" ("Id"),
    "SkillId" integer NOT NULL REFERENCES skillbridge."Skills" ("Id"),
    "Status" varchar(20) NOT NULL,
    "CreatedAt" timestamp without time zone NOT NULL
);
CREATE INDEX "IX_SkillRequests_RequesterId" ON skillbridge."SkillRequests" ("RequesterId");
CREATE INDEX "IX_SkillRequests_ReceiverId" ON skillbridge."SkillRequests" ("ReceiverId");
CREATE INDEX "IX_SkillRequests_SkillId" ON skillbridge."SkillRequests" ("SkillId");

CREATE TABLE skillbridge."Interactions" (
    "Id" serial PRIMARY KEY,
    "User1Id" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers" ("Id"),
    "User2Id" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers" ("Id"),
    "SkillFromTeacherId" integer NOT NULL REFERENCES skillbridge."Skills" ("Id"),
    "SkillFromRequesterId" integer NOT NULL REFERENCES skillbridge."Skills" ("Id"),
    "Status" varchar(20) NOT NULL,
    "CreatedAt" timestamp without time zone NOT NULL
);
CREATE INDEX "IX_Interactions_User1Id" ON skillbridge."Interactions" ("User1Id");
CREATE INDEX "IX_Interactions_User2Id" ON skillbridge."Interactions" ("User2Id");
CREATE INDEX "IX_Interactions_SkillFromTeacherId" ON skillbridge."Interactions" ("SkillFromTeacherId");
CREATE INDEX "IX_Interactions_SkillFromRequesterId" ON skillbridge."Interactions" ("SkillFromRequesterId");

CREATE TABLE skillbridge."InteractionSessions" (
    "Id" serial PRIMARY KEY,
    "InteractionId" integer NOT NULL REFERENCES skillbridge."Interactions" ("Id") ON DELETE CASCADE,
    "SkillId" integer NOT NULL REFERENCES skillbridge."Skills" ("Id") ON DELETE CASCADE,
    "StageNumber" integer NOT NULL CHECK ("StageNumber" BETWEEN 1 AND 7),
    "User1Confirmed" boolean NOT NULL DEFAULT false,
    "User2Confirmed" boolean NOT NULL DEFAULT false,
    "Status" varchar(20) NOT NULL,
    UNIQUE ("InteractionId", "SkillId", "StageNumber")
);
CREATE INDEX "IX_InteractionSessions_SkillId" ON skillbridge."InteractionSessions" ("SkillId");

CREATE TABLE skillbridge."Ratings" (
    "Id" serial PRIMARY KEY,
    "InteractionId" integer NOT NULL REFERENCES skillbridge."Interactions" ("Id") ON DELETE CASCADE,
    "FromUserId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers" ("Id"),
    "ToUserId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers" ("Id"),
    "RatingValue" integer NOT NULL CHECK ("RatingValue" BETWEEN 1 AND 10),
    "Comment" text,
    "CreatedAt" timestamp without time zone NOT NULL,
    UNIQUE ("InteractionId", "FromUserId")
);
CREATE INDEX "IX_Ratings_FromUserId" ON skillbridge."Ratings" ("FromUserId");
CREATE INDEX "IX_Ratings_ToUserId" ON skillbridge."Ratings" ("ToUserId");

CREATE TABLE skillbridge."Notifications" (
    "Id" serial PRIMARY KEY,
    "UserId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers" ("Id") ON DELETE CASCADE,
    "Type" varchar(50) NOT NULL,
    "ReferenceId" integer,
    "Message" text NOT NULL,
    "IsRead" boolean NOT NULL DEFAULT false,
    "CreatedAt" timestamp without time zone NOT NULL
);
CREATE INDEX "IX_Notifications_UserId_CreatedAt" ON skillbridge."Notifications" ("UserId", "CreatedAt" DESC);

CREATE TABLE skillbridge."UserRatings" (
    "Id" serial PRIMARY KEY,
    "UserId" varchar(128) UNIQUE REFERENCES skillbridge."AspNetUsers" ("Id"),
    "InteractionsCompleted" integer NOT NULL DEFAULT 0,
    "RatingsReceived" integer NOT NULL DEFAULT 0,
    "AccumulatedRating" integer NOT NULL DEFAULT 0
);

CREATE TABLE skillbridge."Communities" (
    "Id" serial PRIMARY KEY,
    "SkillId" integer NOT NULL UNIQUE REFERENCES skillbridge."Skills" ("Id") ON DELETE CASCADE,
    "Name" text,
    "Description" text
);
CREATE TABLE skillbridge."CommunityPosts" (
    "Id" serial PRIMARY KEY,
    "CommunityId" integer NOT NULL REFERENCES skillbridge."Communities" ("Id") ON DELETE CASCADE,
    "CreatedByUserId" varchar(128) REFERENCES skillbridge."AspNetUsers" ("Id"),
    "Title" text,
    "Content" text,
    "CreatedAt" timestamp without time zone NOT NULL,
    "UpdatedAt" timestamp without time zone
);
CREATE INDEX "IX_CommunityPosts_CommunityId_CreatedAt" ON skillbridge."CommunityPosts" ("CommunityId", "CreatedAt" DESC);
CREATE INDEX "IX_CommunityPosts_CreatedByUserId" ON skillbridge."CommunityPosts" ("CreatedByUserId");
CREATE TABLE skillbridge."CommunityComments" (
    "Id" serial PRIMARY KEY,
    "PostId" integer NOT NULL REFERENCES skillbridge."CommunityPosts" ("Id") ON DELETE CASCADE,
    "CreatedByUserId" varchar(128) REFERENCES skillbridge."AspNetUsers" ("Id"),
    "Content" text,
    "CreatedAt" timestamp without time zone NOT NULL
);
CREATE INDEX "IX_CommunityComments_PostId" ON skillbridge."CommunityComments" ("PostId");
CREATE INDEX "IX_CommunityComments_CreatedByUserId" ON skillbridge."CommunityComments" ("CreatedByUserId");

CREATE TABLE skillbridge."Conversations" (
    "Id" serial PRIMARY KEY,
    "User1Id" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers" ("Id"),
    "User2Id" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers" ("Id"),
    "CreatedAt" timestamp without time zone NOT NULL,
    "LastMessageAt" timestamp without time zone,
    CONSTRAINT "IX_Convo_UserPair" UNIQUE ("User1Id", "User2Id")
);
CREATE INDEX "IX_Conversations_User2Id" ON skillbridge."Conversations" ("User2Id");
CREATE TABLE skillbridge."Messages" (
    "Id" serial PRIMARY KEY,
    "ConversationId" integer NOT NULL REFERENCES skillbridge."Conversations" ("Id") ON DELETE CASCADE,
    "FromUserId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers" ("Id"),
    "ToUserId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers" ("Id"),
    "Ciphertext" bytea NOT NULL,
    "IV" bytea NOT NULL,
    "Hmac" bytea NOT NULL,
    "CreatedAt" timestamp without time zone NOT NULL,
    "IsRead" boolean NOT NULL DEFAULT false
);
CREATE INDEX "IX_Messages_Conversation_CreatedAt" ON skillbridge."Messages" ("ConversationId", "CreatedAt");
CREATE INDEX "IX_Messages_ToUser_IsRead" ON skillbridge."Messages" ("ToUserId", "IsRead");
CREATE INDEX "IX_Messages_FromUserId" ON skillbridge."Messages" ("FromUserId");

COMMIT;
