-- =============================================================================
-- SkillBridge Demo Seed Data Script (011_demo_seed.sql)
-- Populates 8 realistic member personas, 80 mixed/varied skill enrollments,
-- 11 completed exchanges (10 two-way swaps + 1 one-way mentorship) with
-- sessions and varied ratings (8.5 - 10.0), plus 15 community feed posts
-- with 38 multi-member discussion comments.
-- Requires 012_one_way_mentorships.sql (for nullable SkillFromRequesterId).
-- Safe and idempotent to run multiple times.
-- Password for all demo accounts: DemoPass123!
-- =============================================================================
BEGIN;

SET search_path TO skillbridge, public;

-- -----------------------------------------------------------------------------
-- 0. Idempotent Cleanup of Existing Demo Seed Data (if re-running)
-- -----------------------------------------------------------------------------
DELETE FROM skillbridge."Notifications"
WHERE "UserId" LIKE 'demo-usr-%'
   OR "ReferenceId" IN (
       SELECT "Id" FROM skillbridge."Interactions"
       WHERE "User1Id" LIKE 'demo-usr-%' OR "User2Id" LIKE 'demo-usr-%'
   );

DELETE FROM skillbridge."Ratings"
WHERE "FromUserId" LIKE 'demo-usr-%' OR "ToUserId" LIKE 'demo-usr-%';

DELETE FROM skillbridge."InteractionSessionNotes"
WHERE "UserId" LIKE 'demo-usr-%'
   OR "InteractionSessionId" IN (
       SELECT s."Id" FROM skillbridge."InteractionSessions" s
       JOIN skillbridge."Interactions" i ON i."Id" = s."InteractionId"
       WHERE i."User1Id" LIKE 'demo-usr-%' OR i."User2Id" LIKE 'demo-usr-%'
   );

DELETE FROM skillbridge."InteractionMeetingEvents"
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

DELETE FROM skillbridge."Interactions"
WHERE "User1Id" LIKE 'demo-usr-%' OR "User2Id" LIKE 'demo-usr-%';

DELETE FROM skillbridge."SkillRequests"
WHERE "RequesterId" LIKE 'demo-usr-%' OR "ReceiverId" LIKE 'demo-usr-%';

DELETE FROM skillbridge."CommunityComments"
WHERE "CreatedByUserId" LIKE 'demo-usr-%';

DELETE FROM skillbridge."CommunityPosts"
WHERE "CreatedByUserId" LIKE 'demo-usr-%';

DELETE FROM skillbridge."UserSkills"
WHERE "UserId" LIKE 'demo-usr-%';

DELETE FROM skillbridge."UserRatings"
WHERE "UserId" LIKE 'demo-usr-%';

DELETE FROM skillbridge."UserInformations"
WHERE "UserId" LIKE 'demo-usr-%';

DELETE FROM skillbridge."AspNetUsers"
WHERE "Id" LIKE 'demo-usr-%';

-- -----------------------------------------------------------------------------
-- 1. Ensure Communities Exist for All Catalog Skills
-- -----------------------------------------------------------------------------
INSERT INTO skillbridge."Communities" ("SkillId", "Name", "Description")
SELECT s."Id", s."Name", 'Community for skill ' || s."Name"
FROM skillbridge."Skills" s
ON CONFLICT ("SkillId") DO NOTHING;

-- -----------------------------------------------------------------------------
-- 2. Seed AspNetUsers (8 Custom Personas)
-- Password for all: DemoPass123!
-- -----------------------------------------------------------------------------
INSERT INTO skillbridge."AspNetUsers" (
    "Id", "Email", "NormalizedEmail", "EmailConfirmed",
    "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
    "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled",
    "LockoutEndDateUtc", "LockoutEnd", "LockoutEnabled", "AccessFailedCount",
    "UserName", "NormalizedUserName"
) VALUES
(
    'demo-usr-01',
    'rakibul.islam@demo.skillbridge.invalid',
    'RAKIBUL.ISLAM@DEMO.SKILLBRIDGE.INVALID',
    true,
    'AQAAAAIAAYagAAAAEMr63N+sl22iEL9QdLt3tce5tC48vHMR7rGscj88q7lqR9RSXJNiyEFpwBtLtzE+WA==',
    'd001a111-1111-4111-8111-111111111101',
    'c001b111-1111-4111-8111-111111111101',
    NULL, false, false, NULL, NULL, true, 0,
    'rakibul.islam@demo.skillbridge.invalid',
    'RAKIBUL.ISLAM@DEMO.SKILLBRIDGE.INVALID'
),
(
    'demo-usr-02',
    'sadik.rahman@demo.skillbridge.invalid',
    'SADIK.RAHMAN@DEMO.SKILLBRIDGE.INVALID',
    true,
    'AQAAAAIAAYagAAAAEMr63N+sl22iEL9QdLt3tce5tC48vHMR7rGscj88q7lqR9RSXJNiyEFpwBtLtzE+WA==',
    'd002a222-2222-4222-8222-222222222202',
    'c002b222-2222-4222-8222-222222222202',
    NULL, false, false, NULL, NULL, true, 0,
    'sadik.rahman@demo.skillbridge.invalid',
    'SADIK.RAHMAN@DEMO.SKILLBRIDGE.INVALID'
),
(
    'demo-usr-03',
    'sumit.majumder@demo.skillbridge.invalid',
    'SUMIT.MAJUMDER@DEMO.SKILLBRIDGE.INVALID',
    true,
    'AQAAAAIAAYagAAAAEMr63N+sl22iEL9QdLt3tce5tC48vHMR7rGscj88q7lqR9RSXJNiyEFpwBtLtzE+WA==',
    'd003a333-3333-4333-8333-333333333303',
    'c003b333-3333-4333-8333-333333333303',
    NULL, false, false, NULL, NULL, true, 0,
    'sumit.majumder@demo.skillbridge.invalid',
    'SUMIT.MAJUMDER@DEMO.SKILLBRIDGE.INVALID'
),
(
    'demo-usr-04',
    'kazi.kamruddin@demo.skillbridge.invalid',
    'KAZI.KAMRUDDIN@DEMO.SKILLBRIDGE.INVALID',
    true,
    'AQAAAAIAAYagAAAAEMr63N+sl22iEL9QdLt3tce5tC48vHMR7rGscj88q7lqR9RSXJNiyEFpwBtLtzE+WA==',
    'd004a444-4444-4444-8444-444444444404',
    'c004b444-4444-4444-8444-444444444404',
    NULL, false, false, NULL, NULL, true, 0,
    'kazi.kamruddin@demo.skillbridge.invalid',
    'KAZI.KAMRUDDIN@DEMO.SKILLBRIDGE.INVALID'
),
(
    'demo-usr-05',
    'abdullah.ishtiaq@demo.skillbridge.invalid',
    'ABDULLAH.ISHTIAQ@DEMO.SKILLBRIDGE.INVALID',
    true,
    'AQAAAAIAAYagAAAAEMr63N+sl22iEL9QdLt3tce5tC48vHMR7rGscj88q7lqR9RSXJNiyEFpwBtLtzE+WA==',
    'd005a555-5555-4555-8555-555555555505',
    'c005b555-5555-4555-8555-555555555505',
    NULL, false, false, NULL, NULL, true, 0,
    'abdullah.ishtiaq@demo.skillbridge.invalid',
    'ABDULLAH.ISHTIAQ@DEMO.SKILLBRIDGE.INVALID'
),
(
    'demo-usr-06',
    'hasibur.srijon@demo.skillbridge.invalid',
    'HASIBUR.SRIJON@DEMO.SKILLBRIDGE.INVALID',
    true,
    'AQAAAAIAAYagAAAAEMr63N+sl22iEL9QdLt3tce5tC48vHMR7rGscj88q7lqR9RSXJNiyEFpwBtLtzE+WA==',
    'd006a666-6666-4666-8666-666666666606',
    'c006b666-6666-4666-8666-666666666606',
    NULL, false, false, NULL, NULL, true, 0,
    'hasibur.srijon@demo.skillbridge.invalid',
    'HASIBUR.SRIJON@DEMO.SKILLBRIDGE.INVALID'
),
(
    'demo-usr-07',
    'nafiz.tonmoy@demo.skillbridge.invalid',
    'NAFIZ.TONMOY@DEMO.SKILLBRIDGE.INVALID',
    true,
    'AQAAAAIAAYagAAAAEMr63N+sl22iEL9QdLt3tce5tC48vHMR7rGscj88q7lqR9RSXJNiyEFpwBtLtzE+WA==',
    'd007a777-7777-4777-8777-777777777707',
    'c007b777-7777-4777-8777-777777777707',
    NULL, false, false, NULL, NULL, true, 0,
    'nafiz.tonmoy@demo.skillbridge.invalid',
    'NAFIZ.TONMOY@DEMO.SKILLBRIDGE.INVALID'
),
(
    'demo-usr-08',
    'david.degea@demo.skillbridge.invalid',
    'DAVID.DEGEA@DEMO.SKILLBRIDGE.INVALID',
    true,
    'AQAAAAIAAYagAAAAEMr63N+sl22iEL9QdLt3tce5tC48vHMR7rGscj88q7lqR9RSXJNiyEFpwBtLtzE+WA==',
    'd008a888-8888-4888-8888-888888888808',
    'c008b888-8888-4888-8888-888888888808',
    NULL, false, false, NULL, NULL, true, 0,
    'david.degea@demo.skillbridge.invalid',
    'DAVID.DEGEA@DEMO.SKILLBRIDGE.INVALID'
)
ON CONFLICT ("Id") DO NOTHING;

-- -----------------------------------------------------------------------------
-- 3. Seed UserInformations (Detailed Profiles & Availability)
-- -----------------------------------------------------------------------------
INSERT INTO skillbridge."UserInformations" (
    "UserId", "FullName", "Age", "Profession", "Location", "Bio",
    "IsPublic", "IsHidden", "AvailabilityNotes", "MeetingFormat", "AvailableDaysMask", "TimeZoneId"
) VALUES
(
    'demo-usr-01', 'Rakibul Islam', 25, 'Senior Frontend Architect', 'Dhaka, Bangladesh',
    'Building scalable React architectures, accessible component libraries, and modern web tooling. Love mentoring frontend developers and exchanging ideas on Python backends and product storytelling.',
    true, false, 'Evenings 7 PM - 10 PM BST and Friday/Saturday afternoons', 'Online', 101, 'Asia/Dhaka'
),
(
    'demo-usr-02', 'Sadik Rahman', 26, 'Backend & Cloud Systems Engineer', 'Chittagong, Bangladesh',
    'Distributed systems engineer specializing in Python microservices, PostgreSQL query tuning, and Git workflows. Looking to sharpen my frontend React skills and UI design intuition.',
    true, false, 'Weeknights after 8 PM BST and weekend mornings', 'Either', 127, 'Asia/Dhaka'
),
(
    'demo-usr-03', 'Sumit Majumder', 24, 'Lead UI/UX & Design Systems Specialist', 'Sylhet, Bangladesh',
    'Obsessed with neo-brutalist interfaces, Figma design tokens, micro-interactions, and WCAG accessibility. Excited to trade design critiques for hands-on React and JavaScript pair programming.',
    true, false, 'Flexible weekday afternoons and Sunday evenings', 'Online', 95, 'Asia/Dhaka'
),
(
    'demo-usr-04', 'Kazi Kamruddin', 27, 'Systems & Embedded Software Engineer', 'Rajshahi, Bangladesh',
    'Low-level C++20 and Java developer focused on memory safety, concurrency, and clean Git history. Currently exploring relational database internals and 3D CAD prototyping.',
    true, false, 'Tuesday, Thursday, and Saturday evenings', 'Online', 84, 'Asia/Dhaka'
),
(
    'demo-usr-05', 'Abdullah Ishtiaq', 25, 'Robotics & Mechatronics Engineer', 'Khulna, Bangladesh',
    'Designing custom robotic manipulators with SolidWorks, AutoCAD, and MATLAB simulations. Eager to learn Python automation and agile project management for hardware teams.',
    true, false, 'Weekends all day and Wednesday evenings', 'Either', 73, 'Asia/Dhaka'
),
(
    'demo-usr-06', 'Hasibur Srijon', 26, 'Data Scientist & Quantitative Analyst', 'Barisal, Bangladesh',
    'Turning messy datasets into actionable insights using statistical modeling, SQL window functions, and advanced Excel automation. Curious about interactive web dashboards and Figma.',
    true, false, 'Monday, Wednesday, Friday 8 PM - 11 PM BST', 'Online', 42, 'Asia/Dhaka'
),
(
    'demo-usr-07', 'Nafiz Tonmoy', 28, 'Technical Program Manager & Team Coach', 'Comilla, Bangladesh',
    'Helping engineering teams ship on time without burnout. Happy to coach on agile delivery, architecture decision records, and public speaking while I level up my SQL and data analytics.',
    true, false, 'Weekdays 6 PM - 9 PM BST', 'Either', 62, 'Asia/Dhaka'
),
(
    'demo-usr-08', 'David De Gea', 29, 'Multimedia Producer & Creative Director', 'Madrid / Remote',
    'Digital video editor, colorist, and visual storyteller with 7+ years in post-production and international communication. Trading video & graphic design expertise for web and CAD fundamentals.',
    true, false, 'Afternoons and evenings CET / UTC+1', 'Online', 119, 'Europe/Madrid'
)
ON CONFLICT ("UserId") DO NOTHING;

-- -----------------------------------------------------------------------------
-- 4. Seed UserSkills (80 Mixed & Randomized Skill Entries Across All 21 Skills)
-- Every user teaches 5 skills (varied stages 4-7) and learns 5 skills (stage 0)
-- -----------------------------------------------------------------------------
INSERT INTO skillbridge."UserSkills" ("UserId", "SkillId", "Status", "KnownUpToStage")
SELECT v.user_id, s."Id", v.status, v.stage
FROM (VALUES
    -- 1. Rakibul Islam (Teaches 5, Learns 5)
    ('demo-usr-01', 'React', 'Teaching', 7),
    ('demo-usr-01', 'JavaScript', 'Teaching', 7),
    ('demo-usr-01', 'HTML & CSS', 'Teaching', 6),
    ('demo-usr-01', 'Git & GitHub', 'Teaching', 5),
    ('demo-usr-01', 'Figma', 'Teaching', 4),
    ('demo-usr-01', 'Python', 'Learning', 0),
    ('demo-usr-01', 'Public Speaking', 'Learning', 0),
    ('demo-usr-01', 'SQL', 'Learning', 0),
    ('demo-usr-01', 'Video Editing', 'Learning', 0),
    ('demo-usr-01', 'Project Management', 'Learning', 0),

    -- 2. Sadik Rahman (Teaches 5, Learns 5)
    ('demo-usr-02', 'Python', 'Teaching', 7),
    ('demo-usr-02', 'SQL', 'Teaching', 7),
    ('demo-usr-02', 'Git & GitHub', 'Teaching', 7),
    ('demo-usr-02', 'Java', 'Teaching', 6),
    ('demo-usr-02', 'Technical Writing', 'Teaching', 5),
    ('demo-usr-02', 'React', 'Learning', 0),
    ('demo-usr-02', 'UI/UX Design', 'Learning', 0),
    ('demo-usr-02', 'C++', 'Learning', 0),
    ('demo-usr-02', 'Statistics', 'Learning', 0),
    ('demo-usr-02', 'English Conversation', 'Learning', 0),

    -- 3. Sumit Majumder (Teaches 5, Learns 5)
    ('demo-usr-03', 'UI/UX Design', 'Teaching', 7),
    ('demo-usr-03', 'Figma', 'Teaching', 7),
    ('demo-usr-03', 'Graphic Design', 'Teaching', 6),
    ('demo-usr-03', 'HTML & CSS', 'Teaching', 5),
    ('demo-usr-03', 'Video Editing', 'Teaching', 4),
    ('demo-usr-03', 'React', 'Learning', 0),
    ('demo-usr-03', 'JavaScript', 'Learning', 0),
    ('demo-usr-03', 'Python', 'Learning', 0),
    ('demo-usr-03', 'Public Speaking', 'Learning', 0),
    ('demo-usr-03', 'Excel', 'Learning', 0),

    -- 4. Kazi Kamruddin (Teaches 5, Learns 5)
    ('demo-usr-04', 'C++', 'Teaching', 7),
    ('demo-usr-04', 'Java', 'Teaching', 7),
    ('demo-usr-04', 'Git & GitHub', 'Teaching', 6),
    ('demo-usr-04', 'MATLAB', 'Teaching', 5),
    ('demo-usr-04', 'Python', 'Teaching', 5),
    ('demo-usr-04', 'SQL', 'Learning', 0),
    ('demo-usr-04', 'SolidWorks', 'Learning', 0),
    ('demo-usr-04', 'Technical Writing', 'Learning', 0),
    ('demo-usr-04', 'React', 'Learning', 0),
    ('demo-usr-04', 'AutoCAD', 'Learning', 0),

    -- 5. Abdullah Ishtiaq (Teaches 5, Learns 5)
    ('demo-usr-05', 'SolidWorks', 'Teaching', 7),
    ('demo-usr-05', 'AutoCAD', 'Teaching', 7),
    ('demo-usr-05', 'MATLAB', 'Teaching', 6),
    ('demo-usr-05', 'C++', 'Teaching', 4),
    ('demo-usr-05', 'Excel', 'Teaching', 5),
    ('demo-usr-05', 'Python', 'Learning', 0),
    ('demo-usr-05', 'Java', 'Learning', 0),
    ('demo-usr-05', 'Project Management', 'Learning', 0),
    ('demo-usr-05', 'Git & GitHub', 'Learning', 0),
    ('demo-usr-05', 'Public Speaking', 'Learning', 0),

    -- 6. Hasibur Srijon (Teaches 5, Learns 5)
    ('demo-usr-06', 'Statistics', 'Teaching', 7),
    ('demo-usr-06', 'Excel', 'Teaching', 7),
    ('demo-usr-06', 'SQL', 'Teaching', 6),
    ('demo-usr-06', 'Python', 'Teaching', 6),
    ('demo-usr-06', 'MATLAB', 'Teaching', 4),
    ('demo-usr-06', 'JavaScript', 'Learning', 0),
    ('demo-usr-06', 'Figma', 'Learning', 0),
    ('demo-usr-06', 'UI/UX Design', 'Learning', 0),
    ('demo-usr-06', 'Project Management', 'Learning', 0),
    ('demo-usr-06', 'Technical Writing', 'Learning', 0),

    -- 7. Nafiz Tonmoy (Teaches 5, Learns 5)
    ('demo-usr-07', 'Project Management', 'Teaching', 7),
    ('demo-usr-07', 'Public Speaking', 'Teaching', 7),
    ('demo-usr-07', 'Technical Writing', 'Teaching', 7),
    ('demo-usr-07', 'English Conversation', 'Teaching', 6),
    ('demo-usr-07', 'Excel', 'Teaching', 5),
    ('demo-usr-07', 'SQL', 'Learning', 0),
    ('demo-usr-07', 'Python', 'Learning', 0),
    ('demo-usr-07', 'Statistics', 'Learning', 0),
    ('demo-usr-07', 'HTML & CSS', 'Learning', 0),
    ('demo-usr-07', 'Graphic Design', 'Learning', 0),

    -- 8. David De Gea (Teaches 5, Learns 5)
    ('demo-usr-08', 'Video Editing', 'Teaching', 7),
    ('demo-usr-08', 'Graphic Design', 'Teaching', 7),
    ('demo-usr-08', 'English Conversation', 'Teaching', 7),
    ('demo-usr-08', 'Public Speaking', 'Teaching', 5),
    ('demo-usr-08', 'UI/UX Design', 'Teaching', 4),
    ('demo-usr-08', 'Figma', 'Learning', 0),
    ('demo-usr-08', 'HTML & CSS', 'Learning', 0),
    ('demo-usr-08', 'JavaScript', 'Learning', 0),
    ('demo-usr-08', 'AutoCAD', 'Learning', 0),
    ('demo-usr-08', 'SolidWorks', 'Learning', 0)
) AS v(user_id, skill_name, status, stage)
JOIN skillbridge."Skills" s ON s."Name" = v.skill_name;

-- -----------------------------------------------------------------------------
-- 5. Seed Completed Interactions, Sessions, Ratings & UserRatings
-- 10 completed peer exchanges producing varied member ratings (8.5 to 10.0)
-- -----------------------------------------------------------------------------
DO $$
DECLARE
    v_int_id integer;
    s_react integer;
    s_python integer;
    s_uiux integer;
    s_js integer;
    s_solidworks integer;
    s_cpp integer;
    s_stats integer;
    s_pm integer;
    s_video integer;
    s_figma integer;
    s_html integer;
    s_excel integer;
    s_sql integer;
    s_git integer;
    s_speaking integer;
    s_matlab integer;
    s_english integer;
    s_graphic integer;
BEGIN
    SELECT "Id" INTO s_react FROM skillbridge."Skills" WHERE "Name" = 'React';
    SELECT "Id" INTO s_python FROM skillbridge."Skills" WHERE "Name" = 'Python';
    SELECT "Id" INTO s_uiux FROM skillbridge."Skills" WHERE "Name" = 'UI/UX Design';
    SELECT "Id" INTO s_js FROM skillbridge."Skills" WHERE "Name" = 'JavaScript';
    SELECT "Id" INTO s_solidworks FROM skillbridge."Skills" WHERE "Name" = 'SolidWorks';
    SELECT "Id" INTO s_cpp FROM skillbridge."Skills" WHERE "Name" = 'C++';
    SELECT "Id" INTO s_stats FROM skillbridge."Skills" WHERE "Name" = 'Statistics';
    SELECT "Id" INTO s_pm FROM skillbridge."Skills" WHERE "Name" = 'Project Management';
    SELECT "Id" INTO s_video FROM skillbridge."Skills" WHERE "Name" = 'Video Editing';
    SELECT "Id" INTO s_figma FROM skillbridge."Skills" WHERE "Name" = 'Figma';
    SELECT "Id" INTO s_html FROM skillbridge."Skills" WHERE "Name" = 'HTML & CSS';
    SELECT "Id" INTO s_excel FROM skillbridge."Skills" WHERE "Name" = 'Excel';
    SELECT "Id" INTO s_sql FROM skillbridge."Skills" WHERE "Name" = 'SQL';
    SELECT "Id" INTO s_git FROM skillbridge."Skills" WHERE "Name" = 'Git & GitHub';
    SELECT "Id" INTO s_speaking FROM skillbridge."Skills" WHERE "Name" = 'Public Speaking';
    SELECT "Id" INTO s_matlab FROM skillbridge."Skills" WHERE "Name" = 'MATLAB';
    SELECT "Id" INTO s_english FROM skillbridge."Skills" WHERE "Name" = 'English Conversation';
    SELECT "Id" INTO s_graphic FROM skillbridge."Skills" WHERE "Name" = 'Graphic Design';

    -- Interaction 1: Rakibul Islam (React) <-> Sadik Rahman (Python)
    INSERT INTO skillbridge."Interactions" ("User1Id", "User2Id", "SkillFromTeacherId", "SkillFromRequesterId", "Status", "CreatedAt", "EndedAt")
    VALUES ('demo-usr-01', 'demo-usr-02', s_python, s_react, 'Completed', NOW() - INTERVAL '28 days', NOW() - INTERVAL '20 days')
    RETURNING "Id" INTO v_int_id;

    INSERT INTO skillbridge."InteractionSessions" ("InteractionId", "SkillId", "StageNumber", "Title", "CatalogStageNumber", "User1Confirmed", "User2Confirmed", "Status") VALUES
    (v_int_id, s_react, 1, 'Components, JSX and props', 1, true, true, 'Confirmed'),
    (v_int_id, s_react, 2, 'State and events', 2, true, true, 'Confirmed'),
    (v_int_id, s_python, 1, 'Python Basics & Syntax', 1, true, true, 'Confirmed'),
    (v_int_id, s_python, 2, 'Functions & Modularization', 4, true, true, 'Confirmed');

    INSERT INTO skillbridge."Ratings" ("InteractionId", "FromUserId", "ToUserId", "RatingValue", "Comment", "CreatedAt") VALUES
    (v_int_id, 'demo-usr-02', 'demo-usr-01', 10, 'Outstanding mentor! Rakibul broke down React state reconciliation and custom hook patterns with crystal-clear diagrams and live debugging.', NOW() - INTERVAL '20 days'),
    (v_int_id, 'demo-usr-01', 'demo-usr-02', 10, 'Sadik is a phenomenal backend engineer. Learned so much about idiomatic Python structure, virtual environments, and async generators.', NOW() - INTERVAL '20 days');

    -- Interaction 2: Sumit Majumder (UI/UX Design) <-> Rakibul Islam (JavaScript)
    INSERT INTO skillbridge."Interactions" ("User1Id", "User2Id", "SkillFromTeacherId", "SkillFromRequesterId", "Status", "CreatedAt", "EndedAt")
    VALUES ('demo-usr-03', 'demo-usr-01', s_js, s_uiux, 'Completed', NOW() - INTERVAL '24 days', NOW() - INTERVAL '16 days')
    RETURNING "Id" INTO v_int_id;

    INSERT INTO skillbridge."InteractionSessions" ("InteractionId", "SkillId", "StageNumber", "Title", "CatalogStageNumber", "User1Confirmed", "User2Confirmed", "Status") VALUES
    (v_int_id, s_uiux, 1, 'Visual hierarchy and layout', 3, true, true, 'Confirmed'),
    (v_int_id, s_uiux, 2, 'Usability review and iteration', 6, true, true, 'Confirmed'),
    (v_int_id, s_js, 1, 'DOM and events', 4, true, true, 'Confirmed'),
    (v_int_id, s_js, 2, 'Asynchronous code and APIs', 5, true, true, 'Confirmed');

    INSERT INTO skillbridge."Ratings" ("InteractionId", "FromUserId", "ToUserId", "RatingValue", "Comment", "CreatedAt") VALUES
    (v_int_id, 'demo-usr-01', 'demo-usr-03', 10, 'Sumit completely transformed how I think about visual hierarchy, contrast ratios, and Neo-Brutalist spacing systems.', NOW() - INTERVAL '16 days'),
    (v_int_id, 'demo-usr-03', 'demo-usr-01', 9, 'Patient, structured, and super practical JavaScript DOM & event delegation walkthroughs. Highly recommend pairing with Rakibul!', NOW() - INTERVAL '16 days');

    -- Interaction 3: Abdullah Ishtiaq (SolidWorks) <-> Kazi Kamruddin (C++)
    INSERT INTO skillbridge."Interactions" ("User1Id", "User2Id", "SkillFromTeacherId", "SkillFromRequesterId", "Status", "CreatedAt", "EndedAt")
    VALUES ('demo-usr-05', 'demo-usr-04', s_cpp, s_solidworks, 'Completed', NOW() - INTERVAL '22 days', NOW() - INTERVAL '14 days')
    RETURNING "Id" INTO v_int_id;

    INSERT INTO skillbridge."InteractionSessions" ("InteractionId", "SkillId", "StageNumber", "Title", "CatalogStageNumber", "User1Confirmed", "User2Confirmed", "Status") VALUES
    (v_int_id, s_solidworks, 1, '2D Sketching & Constraints', 2, true, true, 'Confirmed'),
    (v_int_id, s_solidworks, 2, 'Part Modeling (Extrude, Revolve, Loft)', 3, true, true, 'Confirmed'),
    (v_int_id, s_cpp, 1, 'Pointers & Memory Management', 5, true, true, 'Confirmed'),
    (v_int_id, s_cpp, 2, 'Object-Oriented Programming (Classes & Objects)', 6, true, true, 'Confirmed');

    INSERT INTO skillbridge."Ratings" ("InteractionId", "FromUserId", "ToUserId", "RatingValue", "Comment", "CreatedAt") VALUES
    (v_int_id, 'demo-usr-04', 'demo-usr-05', 10, 'Abdullah made 3D parametric CAD modeling intuitive. We modeled a complete custom enclosure and sensor mount from scratch.', NOW() - INTERVAL '14 days'),
    (v_int_id, 'demo-usr-05', 'demo-usr-04', 9, 'Kazi has deep mastery of C++ pointers and RAII memory management. Helped me eliminate memory leaks in my robotics controller.', NOW() - INTERVAL '14 days');

    -- Interaction 4: Hasibur Srijon (Statistics) <-> Nafiz Tonmoy (Project Management)
    INSERT INTO skillbridge."Interactions" ("User1Id", "User2Id", "SkillFromTeacherId", "SkillFromRequesterId", "Status", "CreatedAt", "EndedAt")
    VALUES ('demo-usr-06', 'demo-usr-07', s_pm, s_stats, 'Completed', NOW() - INTERVAL '19 days', NOW() - INTERVAL '11 days')
    RETURNING "Id" INTO v_int_id;

    INSERT INTO skillbridge."InteractionSessions" ("InteractionId", "SkillId", "StageNumber", "Title", "CatalogStageNumber", "User1Confirmed", "User2Confirmed", "Status") VALUES
    (v_int_id, s_stats, 1, 'Distributions (Normal, Binomial, etc.)', 4, true, true, 'Confirmed'),
    (v_int_id, s_stats, 2, 'Hypothesis Testing', 5, true, true, 'Confirmed'),
    (v_int_id, s_pm, 1, 'Breaking work into tasks', 2, true, true, 'Confirmed'),
    (v_int_id, s_pm, 2, 'Risks and blockers', 5, true, true, 'Confirmed');

    INSERT INTO skillbridge."Ratings" ("InteractionId", "FromUserId", "ToUserId", "RatingValue", "Comment", "CreatedAt") VALUES
    (v_int_id, 'demo-usr-07', 'demo-usr-06', 9, 'Hasibur simplified p-values, confidence intervals, and sample sizing so I can apply them directly to product delivery metrics.', NOW() - INTERVAL '11 days'),
    (v_int_id, 'demo-usr-06', 'demo-usr-07', 10, 'Nafiz gave executive-level coaching on agile sprint scoping and stakeholder alignment. Best exchange experience so far!', NOW() - INTERVAL '11 days');

    -- Interaction 5: David De Gea (Video Editing) <-> Sumit Majumder (Figma)
    INSERT INTO skillbridge."Interactions" ("User1Id", "User2Id", "SkillFromTeacherId", "SkillFromRequesterId", "Status", "CreatedAt", "EndedAt")
    VALUES ('demo-usr-08', 'demo-usr-03', s_figma, s_video, 'Completed', NOW() - INTERVAL '17 days', NOW() - INTERVAL '9 days')
    RETURNING "Id" INTO v_int_id;

    INSERT INTO skillbridge."InteractionSessions" ("InteractionId", "SkillId", "StageNumber", "Title", "CatalogStageNumber", "User1Confirmed", "User2Confirmed", "Status") VALUES
    (v_int_id, s_video, 1, 'Cutting and pacing', 2, true, true, 'Confirmed'),
    (v_int_id, s_video, 2, 'Audio cleanup and levels', 3, true, true, 'Confirmed'),
    (v_int_id, s_figma, 1, 'Auto layout and constraints', 2, true, true, 'Confirmed'),
    (v_int_id, s_figma, 2, 'Components and variants', 4, true, true, 'Confirmed');

    INSERT INTO skillbridge."Ratings" ("InteractionId", "FromUserId", "ToUserId", "RatingValue", "Comment", "CreatedAt") VALUES
    (v_int_id, 'demo-usr-03', 'demo-usr-08', 9, 'Great hands-on tips for timeline pacing, J/L cuts, and dialogue EQ cleanup. Very practical session!', NOW() - INTERVAL '9 days'),
    (v_int_id, 'demo-usr-08', 'demo-usr-03', 9, 'Sumit showed me how Figma Auto Layout and component variants save hours when mocking up video lower-thirds and storyboards.', NOW() - INTERVAL '9 days');

    -- Interaction 6: Rakibul Islam (HTML & CSS) <-> Hasibur Srijon (Excel)
    INSERT INTO skillbridge."Interactions" ("User1Id", "User2Id", "SkillFromTeacherId", "SkillFromRequesterId", "Status", "CreatedAt", "EndedAt")
    VALUES ('demo-usr-01', 'demo-usr-06', s_excel, s_html, 'Completed', NOW() - INTERVAL '15 days', NOW() - INTERVAL '8 days')
    RETURNING "Id" INTO v_int_id;

    INSERT INTO skillbridge."InteractionSessions" ("InteractionId", "SkillId", "StageNumber", "Title", "CatalogStageNumber", "User1Confirmed", "User2Confirmed", "Status") VALUES
    (v_int_id, s_html, 1, 'Flexbox and grid', 4, true, true, 'Confirmed'),
    (v_int_id, s_excel, 1, 'Pivot Tables & Data Summarization', 5, true, true, 'Confirmed');

    INSERT INTO skillbridge."Ratings" ("InteractionId", "FromUserId", "ToUserId", "RatingValue", "Comment", "CreatedAt") VALUES
    (v_int_id, 'demo-usr-06', 'demo-usr-01', 10, 'Finally mastered CSS Grid and responsive Flexbox layouts thanks to Rakibul''s live CodePen exercises.', NOW() - INTERVAL '8 days'),
    (v_int_id, 'demo-usr-01', 'demo-usr-06', 9, 'Hasibur''s XLOOKUP and Pivot Table shortcuts saved me hours on my monthly project tracking sheets.', NOW() - INTERVAL '8 days');

    -- Interaction 7: Sadik Rahman (SQL) <-> Kazi Kamruddin (Git & GitHub)
    INSERT INTO skillbridge."Interactions" ("User1Id", "User2Id", "SkillFromTeacherId", "SkillFromRequesterId", "Status", "CreatedAt", "EndedAt")
    VALUES ('demo-usr-02', 'demo-usr-04', s_git, s_sql, 'Completed', NOW() - INTERVAL '13 days', NOW() - INTERVAL '6 days')
    RETURNING "Id" INTO v_int_id;

    INSERT INTO skillbridge."InteractionSessions" ("InteractionId", "SkillId", "StageNumber", "Title", "CatalogStageNumber", "User1Confirmed", "User2Confirmed", "Status") VALUES
    (v_int_id, s_sql, 1, 'Joins & Subqueries', 3, true, true, 'Confirmed'),
    (v_int_id, s_sql, 2, 'Indexes, Views & Stored Procedures', 6, true, true, 'Confirmed'),
    (v_int_id, s_git, 1, 'Resolving merge conflicts', 6, true, true, 'Confirmed');

    INSERT INTO skillbridge."Ratings" ("InteractionId", "FromUserId", "ToUserId", "RatingValue", "Comment", "CreatedAt") VALUES
    (v_int_id, 'demo-usr-04', 'demo-usr-02', 9, 'Sadik walked me through PostgreSQL EXPLAIN ANALYZE and composite indexing with real query plans.', NOW() - INTERVAL '6 days'),
    (v_int_id, 'demo-usr-02', 'demo-usr-04', 10, 'Kazi''s interactive rebase and bisect workflow tips were super clean and immediately useful for our team repo.', NOW() - INTERVAL '6 days');

    -- Interaction 8: Nafiz Tonmoy (Public Speaking) <-> Abdullah Ishtiaq (MATLAB)
    INSERT INTO skillbridge."Interactions" ("User1Id", "User2Id", "SkillFromTeacherId", "SkillFromRequesterId", "Status", "CreatedAt", "EndedAt")
    VALUES ('demo-usr-07', 'demo-usr-05', s_matlab, s_speaking, 'Completed', NOW() - INTERVAL '11 days', NOW() - INTERVAL '5 days')
    RETURNING "Id" INTO v_int_id;

    INSERT INTO skillbridge."InteractionSessions" ("InteractionId", "SkillId", "StageNumber", "Title", "CatalogStageNumber", "User1Confirmed", "User2Confirmed", "Status") VALUES
    (v_int_id, s_speaking, 1, 'Structuring a short talk', 1, true, true, 'Confirmed'),
    (v_int_id, s_speaking, 2, 'Handling questions', 6, true, true, 'Confirmed'),
    (v_int_id, s_matlab, 1, '2D & 3D Data Visualization', 5, true, true, 'Confirmed');

    INSERT INTO skillbridge."Ratings" ("InteractionId", "FromUserId", "ToUserId", "RatingValue", "Comment", "CreatedAt") VALUES
    (v_int_id, 'demo-usr-05', 'demo-usr-07', 10, 'Nafiz coached me right before my robotics thesis defense. His pacing and Q&A framing advice made all the difference!', NOW() - INTERVAL '5 days'),
    (v_int_id, 'demo-usr-07', 'demo-usr-05', 9, 'Abdullah explained MATLAB matrix plotting and script automation clearly and patiently.', NOW() - INTERVAL '5 days');

    -- Interaction 9: Sadik Rahman (Python) <-> David De Gea (English Conversation)
    INSERT INTO skillbridge."Interactions" ("User1Id", "User2Id", "SkillFromTeacherId", "SkillFromRequesterId", "Status", "CreatedAt", "EndedAt")
    VALUES ('demo-usr-02', 'demo-usr-08', s_english, s_python, 'Completed', NOW() - INTERVAL '9 days', NOW() - INTERVAL '4 days')
    RETURNING "Id" INTO v_int_id;

    INSERT INTO skillbridge."InteractionSessions" ("InteractionId", "SkillId", "StageNumber", "Title", "CatalogStageNumber", "User1Confirmed", "User2Confirmed", "Status") VALUES
    (v_int_id, s_python, 1, 'Python Basics & Syntax', 1, true, true, 'Confirmed'),
    (v_int_id, s_english, 1, 'Work and study scenarios', 6, true, true, 'Confirmed');

    INSERT INTO skillbridge."Ratings" ("InteractionId", "FromUserId", "ToUserId", "RatingValue", "Comment", "CreatedAt") VALUES
    (v_int_id, 'demo-usr-08', 'demo-usr-02', 9, 'Sadik helped me write my first Python script to batch-rename media files automatically.', NOW() - INTERVAL '4 days'),
    (v_int_id, 'demo-usr-02', 'demo-usr-08', 8, 'Enjoyable conversation practice focused on international remote engineering standups.', NOW() - INTERVAL '4 days');

    -- Interaction 10: Sumit Majumder (Graphic Design) <-> Hasibur Srijon (SQL)
    INSERT INTO skillbridge."Interactions" ("User1Id", "User2Id", "SkillFromTeacherId", "SkillFromRequesterId", "Status", "CreatedAt", "EndedAt")
    VALUES ('demo-usr-03', 'demo-usr-06', s_sql, s_graphic, 'Completed', NOW() - INTERVAL '7 days', NOW() - INTERVAL '2 days')
    RETURNING "Id" INTO v_int_id;

    INSERT INTO skillbridge."InteractionSessions" ("InteractionId", "SkillId", "StageNumber", "Title", "CatalogStageNumber", "User1Confirmed", "User2Confirmed", "Status") VALUES
    (v_int_id, s_graphic, 1, 'Color palettes and contrast', 2, true, true, 'Confirmed'),
    (v_int_id, s_sql, 1, 'Aggregations & GROUP BY', 4, true, true, 'Confirmed');

    INSERT INTO skillbridge."Ratings" ("InteractionId", "FromUserId", "ToUserId", "RatingValue", "Comment", "CreatedAt") VALUES
    (v_int_id, 'demo-usr-06', 'demo-usr-03', 10, 'Sumit''s color theory tips made my data science charts and slide decks look 10x more professional.', NOW() - INTERVAL '2 days'),
    (v_int_id, 'demo-usr-03', 'demo-usr-06', 10, 'Hasibur made SQL GROUP BY and window functions click in a single session!', NOW() - INTERVAL '2 days');

    -- Interaction 11 (Showcase 1-Way Mentorship): Hasibur Srijon mentors Abdullah Ishtiaq in Python
    INSERT INTO skillbridge."Interactions" ("User1Id", "User2Id", "SkillFromTeacherId", "SkillFromRequesterId", "Status", "CreatedAt", "EndedAt")
    VALUES ('demo-usr-06', 'demo-usr-05', s_python, NULL, 'Completed', NOW() - INTERVAL '4 days', NOW() - INTERVAL '1 day')
    RETURNING "Id" INTO v_int_id;

    INSERT INTO skillbridge."InteractionSessions" ("InteractionId", "SkillId", "StageNumber", "Title", "CatalogStageNumber", "User1Confirmed", "User2Confirmed", "Status") VALUES
    (v_int_id, s_python, 1, 'Python Basics & Syntax', 1, true, true, 'Confirmed'),
    (v_int_id, s_python, 2, 'Control Flow & Loops', 2, true, true, 'Confirmed');

    UPDATE skillbridge."UserSkills"
    SET "KnownUpToStage" = GREATEST(COALESCE("KnownUpToStage", 0), 2)
    WHERE "UserId" = 'demo-usr-05' AND "SkillId" = s_python AND "Status" = 'Learning';

    INSERT INTO skillbridge."Ratings" ("InteractionId", "FromUserId", "ToUserId", "RatingValue", "Comment", "CreatedAt") VALUES
    (v_int_id, 'demo-usr-05', 'demo-usr-06', 10, 'Hasibur generously mentored me one-way through Python fundamentals with practical robotics automation exercises.', NOW() - INTERVAL '1 day'),
    (v_int_id, 'demo-usr-06', 'demo-usr-05', 9, 'Abdullah picked up Python basics and loops rapidly during our focused mentorship sessions.', NOW() - INTERVAL '1 day');
END $$;

-- Synchronize UserRatings counters accurately with the seeded completed exchanges and ratings
INSERT INTO skillbridge."UserRatings" ("UserId", "InteractionsCompleted", "RatingsReceived", "AccumulatedRating") VALUES
('demo-usr-01', 3, 3, 29), -- Avg: 9.7 / 10 (10, 9, 10)
('demo-usr-02', 3, 3, 28), -- Avg: 9.3 / 10 (10, 9, 9)
('demo-usr-03', 3, 3, 29), -- Avg: 9.7 / 10 (10, 9, 10)
('demo-usr-04', 2, 2, 19), -- Avg: 9.5 / 10 (9, 10)
('demo-usr-05', 3, 3, 28), -- Avg: 9.3 / 10 (10, 9, 9)
('demo-usr-06', 4, 4, 38), -- Avg: 9.5 / 10 (9, 9, 10, 10)
('demo-usr-07', 2, 2, 20), -- Avg: 10.0 / 10 (10, 10)
('demo-usr-08', 2, 2, 17)  -- Avg: 8.5 / 10 (9, 8)
ON CONFLICT ("UserId") DO UPDATE SET
    "InteractionsCompleted" = EXCLUDED."InteractionsCompleted",
    "RatingsReceived" = EXCLUDED."RatingsReceived",
    "AccumulatedRating" = EXCLUDED."AccumulatedRating";

-- -----------------------------------------------------------------------------
-- 6. Seed Lively Community Feed: 15 Posts & 38 Comments Across Communities
-- -----------------------------------------------------------------------------
DO $$
DECLARE
    v_comm_id integer;
    v_post_id integer;
BEGIN
    -- Post 1: Python Community (by Sadik Rahman)
    SELECT c."Id" INTO v_comm_id FROM skillbridge."Communities" c JOIN skillbridge."Skills" s ON s."Id" = c."SkillId" WHERE s."Name" = 'Python';
    IF v_comm_id IS NOT NULL THEN
        INSERT INTO skillbridge."CommunityPosts" ("CommunityId", "CreatedByUserId", "Title", "Content", "CreatedAt", "IsHidden")
        VALUES (
            v_comm_id, 'demo-usr-02',
            'Understanding Python Asyncio Event Loop & DB Connection Pooling',
            E'## Why Synchronous Drivers Block Your Event Loop\nWhen building async web services or worker scripts in Python, a common pitfall is calling blocking database drivers inside an **async def** function.\n\nAlways pair **asyncio.gather()** with an async-native connection pool (like **asyncpg** or SQLAlchemy AsyncSession) and cap your pool size to match your PostgreSQL worker threads. Anyone working on async Python scripts this week? Happy to review code in a 1-on-1 exchange!',
            NOW() - INTERVAL '6 days 4 hours', false
        ) RETURNING "Id" INTO v_post_id;

        INSERT INTO skillbridge."CommunityComments" ("PostId", "CreatedByUserId", "Content", "CreatedAt", "IsHidden") VALUES
        (v_post_id, 'demo-usr-01', 'This bit me last month when I was testing a FastAPI prototype! Switching from psycopg2 to asyncpg dropped our p95 latency from 180ms to 22ms.', NOW() - INTERVAL '6 days 2 hours', false),
        (v_post_id, 'demo-usr-06', 'Great breakdown Sadik. Do you also use connection pgbouncer in front of Supabase when running batch analytics jobs?', NOW() - INTERVAL '5 days 20 hours', false),
        (v_post_id, 'demo-usr-02', 'Yes! Transaction pooling mode on port 6543 works great as long as you disable prepared statement caching when necessary.', NOW() - INTERVAL '5 days 18 hours', false);
    END IF;

    -- Post 2: React Community (by Rakibul Islam)
    SELECT c."Id" INTO v_comm_id FROM skillbridge."Communities" c JOIN skillbridge."Skills" s ON s."Id" = c."SkillId" WHERE s."Name" = 'React';
    IF v_comm_id IS NOT NULL THEN
        INSERT INTO skillbridge."CommunityPosts" ("CommunityId", "CreatedByUserId", "Title", "Content", "CreatedAt", "IsHidden")
        VALUES (
            v_comm_id, 'demo-usr-01',
            'Server State vs Client State in Modern React: Stop Overusing useEffect',
            E'## The #1 Anti-Pattern I See in React Code Reviews\nAlmost 70% of **useEffect** hooks in beginner codebases are just manually fetching data and syncing state variables.\n\n**Rule of thumb:**\n1. Use derived values during render instead of syncing state with useEffect.\n2. Separate **UI state** (modals, tabs, theme toggles) from **server cache state**.\n3. Keep custom hooks small and focused on a single responsibility.',
            NOW() - INTERVAL '5 days 14 hours', false
        ) RETURNING "Id" INTO v_post_id;

        INSERT INTO skillbridge."CommunityComments" ("PostId", "CreatedByUserId", "Content", "CreatedAt", "IsHidden") VALUES
        (v_post_id, 'demo-usr-03', 'Guilty of the useEffect state-syncing habit! Replacing three chained effects with a simple derived const during our session yesterday cleaned up 60 lines of code.', NOW() - INTERVAL '5 days 11 hours', false),
        (v_post_id, 'demo-usr-02', 'Coming from backend engineering, thinking of frontend data as a cache with invalidation rules rather than local state made React make so much more sense.', NOW() - INTERVAL '5 days 6 hours', false),
        (v_post_id, 'demo-usr-04', 'Clean mental model. How do you handle optimistic UI updates when a form submission might fail validation?', NOW() - INTERVAL '4 days 22 hours', false);
    END IF;

    -- Post 3: UI/UX Design Community (by Sumit Majumder)
    SELECT c."Id" INTO v_comm_id FROM skillbridge."Communities" c JOIN skillbridge."Skills" s ON s."Id" = c."SkillId" WHERE s."Name" = 'UI/UX Design';
    IF v_comm_id IS NOT NULL THEN
        INSERT INTO skillbridge."CommunityPosts" ("CommunityId", "CreatedByUserId", "Title", "Content", "CreatedAt", "IsHidden")
        VALUES (
            v_comm_id, 'demo-usr-03',
            'The 60-30-10 Color Rule in Neo-Brutalist Web Interfaces',
            E'## Balancing Bold Borders Without Visual Fatigue\nNeo-brutalist design uses crisp **2px solid ink borders** and hard offset shadows, which can quickly overwhelm users if every card is screaming in neon.\n\nTry the **60-30-10 ratio**:\n- **60% Warm Paper / Night Canvas**: Calm neutral surface.\n- **30% Card Surface & Structured Typography**: High-legibility containers.\n- **10% Sage / Coral / Amber Accents**: Reserved strictly for primary CTAs, active badges, and status indicators.',
            NOW() - INTERVAL '5 days 3 hours', false
        ) RETURNING "Id" INTO v_post_id;

        INSERT INTO skillbridge."CommunityComments" ("PostId", "CreatedByUserId", "Content", "CreatedAt", "IsHidden") VALUES
        (v_post_id, 'demo-usr-01', 'Implementing these exact CSS custom tokens in Tailwind v4 made supporting both light and dark themes effortless.', NOW() - INTERVAL '4 days 21 hours', false),
        (v_post_id, 'demo-usr-08', 'This applies to video motion graphics too! Having a neutral background makes lower-third callouts pop without needing oversaturated colors.', NOW() - INTERVAL '4 days 16 hours', false),
        (v_post_id, 'demo-usr-06', 'Bookmarking this for my analytics dashboards. I always struggled with picking colors for multi-series bar charts.', NOW() - INTERVAL '4 days 12 hours', false);
    END IF;

    -- Post 4: C++ Community (by Kazi Kamruddin)
    SELECT c."Id" INTO v_comm_id FROM skillbridge."Communities" c JOIN skillbridge."Skills" s ON s."Id" = c."SkillId" WHERE s."Name" = 'C++';
    IF v_comm_id IS NOT NULL THEN
        INSERT INTO skillbridge."CommunityPosts" ("CommunityId", "CreatedByUserId", "Title", "Content", "CreatedAt", "IsHidden")
        VALUES (
            v_comm_id, 'demo-usr-04',
            'RAII & Smart Pointers: Why Raw new/delete Should Be Extinct in Modern C++',
            E'## Deterministic Resource Cleanup\nIf you are still writing manual **delete** calls in C++, a single early return or thrown exception will leak memory.\n\nPrefer **std::unique_ptr** by default for exclusive ownership (zero runtime overhead compared to a raw pointer!) and **std::shared_ptr** only when shared lifetime across threads is genuinely required.',
            NOW() - INTERVAL '4 days 15 hours', false
        ) RETURNING "Id" INTO v_post_id;

        INSERT INTO skillbridge."CommunityComments" ("PostId", "CreatedByUserId", "Content", "CreatedAt", "IsHidden") VALUES
        (v_post_id, 'demo-usr-05', 'Switching our sensor buffer buffers to std::unique_ptr during our exchange session completely fixed our Raspberry Pi memory creep!', NOW() - INTERVAL '4 days 10 hours', false),
        (v_post_id, 'demo-usr-02', 'How does std::unique_ptr interact with C libraries like OpenSSL or libpq that return raw struct pointers?', NOW() - INTERVAL '4 days 5 hours', false),
        (v_post_id, 'demo-usr-04', 'You can pass a custom deleter functor or function pointer as the second template argument to unique_ptr—super clean and zero overhead!', NOW() - INTERVAL '4 days 2 hours', false);
    END IF;

    -- Post 5: SolidWorks Community (by Abdullah Ishtiaq)
    SELECT c."Id" INTO v_comm_id FROM skillbridge."Communities" c JOIN skillbridge."Skills" s ON s."Id" = c."SkillId" WHERE s."Name" = 'SolidWorks';
    IF v_comm_id IS NOT NULL THEN
        INSERT INTO skillbridge."CommunityPosts" ("CommunityId", "CreatedByUserId", "Title", "Content", "CreatedAt", "IsHidden")
        VALUES (
            v_comm_id, 'demo-usr-05',
            'Parametric Design Equations: Designing 3D Printable Enclosures That Scale',
            E'## Stop Hardcoding Sketch Dimensions\nNothing hurts more than finishing a 40-feature CAD part only to realize your 3D printer nozzle tolerance needs 0.2mm extra clearance on every snap-fit tab.\n\nSet up **Global Variables** (`WallThickness`, `Clearance`, `BoltHoleDia`) in the Equations manager before extruding your first boss. Changing one variable rebuilds the entire assembly cleanly!',
            NOW() - INTERVAL '4 days 1 hour', false
        ) RETURNING "Id" INTO v_post_id;

        INSERT INTO skillbridge."CommunityComments" ("PostId", "CreatedByUserId", "Content", "CreatedAt", "IsHidden") VALUES
        (v_post_id, 'demo-usr-04', 'That Global Variables trick saved my PCB case print on the very first try. 0.25mm clearance was spot on for PLA.', NOW() - INTERVAL '3 days 20 hours', false),
        (v_post_id, 'demo-usr-08', 'I want to design a custom camera rig mount for my desk setup—would SolidWorks or AutoCAD be better to start with?', NOW() - INTERVAL '3 days 15 hours', false),
        (v_post_id, 'demo-usr-05', 'Definitely SolidWorks for 3D mechanical brackets and 3D printing! Send me a request anytime, happy to walk you through sketch constraints.', NOW() - INTERVAL '3 days 12 hours', false);
    END IF;

    -- Post 6: Statistics Community (by Hasibur Srijon)
    SELECT c."Id" INTO v_comm_id FROM skillbridge."Communities" c JOIN skillbridge."Skills" s ON s."Id" = c."SkillId" WHERE s."Name" = 'Statistics';
    IF v_comm_id IS NOT NULL THEN
        INSERT INTO skillbridge."CommunityPosts" ("CommunityId", "CreatedByUserId", "Title", "Content", "CreatedAt", "IsHidden")
        VALUES (
            v_comm_id, 'demo-usr-06',
            'Why Mean Averages Lie on Latency & Income Data (And When to Use Percentiles)',
            E'## Skewed Distributions in the Real World\nWhenever data has a long right tail—like API response times or customer spend—the arithmetic **mean** gets pulled upward by extreme outliers.\n\nAlways report the **median (p50)** alongside **p90** and **p99**, and use the Interquartile Range (IQR) to understand spread before running parametric t-tests.',
            NOW() - INTERVAL '3 days 18 hours', false
        ) RETURNING "Id" INTO v_post_id;

        INSERT INTO skillbridge."CommunityComments" ("PostId", "CreatedByUserId", "Content", "CreatedAt", "IsHidden") VALUES
        (v_post_id, 'demo-usr-07', 'We switched our sprint cycle-time reporting from average days to p50/p85 after our session and stakeholders immediately understood delivery predictability better.', NOW() - INTERVAL '3 days 14 hours', false),
        (v_post_id, 'demo-usr-02', '100% true in systems engineering too. Average latency can look fine at 40ms while 1% of users are waiting 4 seconds.', NOW() - INTERVAL '3 days 9 hours', false);
    END IF;

    -- Post 7: Project Management Community (by Nafiz Tonmoy)
    SELECT c."Id" INTO v_comm_id FROM skillbridge."Communities" c JOIN skillbridge."Skills" s ON s."Id" = c."SkillId" WHERE s."Name" = 'Project Management';
    IF v_comm_id IS NOT NULL THEN
        INSERT INTO skillbridge."CommunityPosts" ("CommunityId", "CreatedByUserId", "Title", "Content", "CreatedAt", "IsHidden")
        VALUES (
            v_comm_id, 'demo-usr-07',
            'Why Daily Standups Turn Into Boring Status Reports (And 3 Fixes)',
            E'## Walk the Board, Not the Person\nInstead of going around the room asking *"What did you do yesterday?"*, try walking the Kanban board from **right to left** (closest to Done first).\n\n1. Focus on **blocked items** and PRs waiting for review.\n2. Keep deep technical debugging for a 10-minute parking lot right after.\n3. End every check-in with clear ownership on the next milestone.',
            NOW() - INTERVAL '3 days 6 hours', false
        ) RETURNING "Id" INTO v_post_id;

        INSERT INTO skillbridge."CommunityComments" ("PostId", "CreatedByUserId", "Content", "CreatedAt", "IsHidden") VALUES
        (v_post_id, 'demo-usr-01', 'Walking the board right-to-left cut our morning standup from 25 minutes to 8 minutes flat.', NOW() - INTERVAL '3 days 2 hours', false),
        (v_post_id, 'demo-usr-05', 'Trying this with our university robotics team this week—we always get sidetracked debugging hardware mid-meeting!', NOW() - INTERVAL '2 days 21 hours', false),
        (v_post_id, 'demo-usr-03', 'Love the parking lot concept for design-to-dev handoff questions too.', NOW() - INTERVAL '2 days 18 hours', false);
    END IF;

    -- Post 8: Video Editing Community (by David De Gea)
    SELECT c."Id" INTO v_comm_id FROM skillbridge."Communities" c JOIN skillbridge."Skills" s ON s."Id" = c."SkillId" WHERE s."Name" = 'Video Editing';
    IF v_comm_id IS NOT NULL THEN
        INSERT INTO skillbridge."CommunityPosts" ("CommunityId", "CreatedByUserId", "Title", "Content", "CreatedAt", "IsHidden")
        VALUES (
            v_comm_id, 'demo-usr-08',
            'The Secret to Seamless Video Edits: J-Cuts, L-Cuts, and Audio Room Tone',
            E'## Viewers Forgive Bad Video, But Never Bad Audio\nIf your cuts feel jarring on tutorial videos or product demos, stop cutting audio and video on the exact same frame!\n\n- **J-Cut**: Bring the next clip''s audio in 6–10 frames *before* the visual cut.\n- **L-Cut**: Let the speaker''s voice trail over B-roll footage.\n- **Room Tone**: Always layer a -45dB ambient room tone track underneath dialogue cuts so silences don''t drop to dead digital zero.',
            NOW() - INTERVAL '2 days 19 hours', false
        ) RETURNING "Id" INTO v_post_id;

        INSERT INTO skillbridge."CommunityComments" ("PostId", "CreatedByUserId", "Content", "CreatedAt", "IsHidden") VALUES
        (v_post_id, 'demo-usr-03', 'The room tone tip was a revelation when editing my portfolio case study videos!', NOW() - INTERVAL '2 days 14 hours', false),
        (v_post_id, 'demo-usr-01', 'I need to record a 3-minute project demo video next week—definitely going to apply J-cuts when switching from slides to terminal.', NOW() - INTERVAL '2 days 9 hours', false);
    END IF;

    -- Post 9: SQL Community (by Sadik Rahman)
    SELECT c."Id" INTO v_comm_id FROM skillbridge."Communities" c JOIN skillbridge."Skills" s ON s."Id" = c."SkillId" WHERE s."Name" = 'SQL';
    IF v_comm_id IS NOT NULL THEN
        INSERT INTO skillbridge."CommunityPosts" ("CommunityId", "CreatedByUserId", "Title", "Content", "CreatedAt", "IsHidden")
        VALUES (
            v_comm_id, 'demo-usr-02',
            'Composite Indexes in PostgreSQL: Why Column Order Matters',
            E'## Leftmost Prefix Principle\nWhen creating a multi-column B-tree index like **CREATE INDEX ON Orders (Status, CreatedAt DESC)**, PostgreSQL sorts first by **Status**, and then by **CreatedAt** within each status.\n\nPut equality filter columns (`=`) first and range/sort columns (`>`, `<`, `ORDER BY`) second to eliminate costly in-memory Sort nodes!',
            NOW() - INTERVAL '2 days 12 hours', false
        ) RETURNING "Id" INTO v_post_id;

        INSERT INTO skillbridge."CommunityComments" ("PostId", "CreatedByUserId", "Content", "CreatedAt", "IsHidden") VALUES
        (v_post_id, 'demo-usr-06', 'Combining this with partial indexes (WHERE IsHidden = false) makes feed queries fly even on large tables.', NOW() - INTERVAL '2 days 7 hours', false),
        (v_post_id, 'demo-usr-04', 'Seeing the Index Scan replace Seq Scan + Sort in EXPLAIN ANALYZE during our session was super satisfying.', NOW() - INTERVAL '2 days 3 hours', false);
    END IF;

    -- Post 10: Figma Community (by Sumit Majumder)
    SELECT c."Id" INTO v_comm_id FROM skillbridge."Communities" c JOIN skillbridge."Skills" s ON s."Id" = c."SkillId" WHERE s."Name" = 'Figma';
    IF v_comm_id IS NOT NULL THEN
        INSERT INTO skillbridge."CommunityPosts" ("CommunityId", "CreatedByUserId", "Title", "Content", "CreatedAt", "IsHidden")
        VALUES (
            v_comm_id, 'demo-usr-03',
            'Structuring Figma Variables for Seamless Developer Handoff',
            E'## Primitive vs Semantic Tokens\nNever bind a component directly to a raw hex code or primitive token like `Green-300`.\n\nInstead, create a two-tier variable collection:\n1. **Primitives**: `Sage-200`, `Ink-900`, `Paper-100`\n2. **Semantics**: `bg-surface-card`, `border-strong`, `text-primary`\nWhen your engineer inspects the frame, the Figma token names map 1:1 to their CSS custom properties!',
            NOW() - INTERVAL '2 days 2 hours', false
        ) RETURNING "Id" INTO v_post_id;

        INSERT INTO skillbridge."CommunityComments" ("PostId", "CreatedByUserId", "Content", "CreatedAt", "IsHidden") VALUES
        (v_post_id, 'demo-usr-08', 'Auto-switching between Light and Dark mode frames using variable modes feels like magic once you set this up.', NOW() - INTERVAL '1 day 21 hours', false),
        (v_post_id, 'demo-usr-06', 'Can''t wait to learn this workflow in our upcoming Figma exchange!', NOW() - INTERVAL '1 day 18 hours', false);
    END IF;

    -- Post 11: Git & GitHub Community (by Kazi Kamruddin)
    SELECT c."Id" INTO v_comm_id FROM skillbridge."Communities" c JOIN skillbridge."Skills" s ON s."Id" = c."SkillId" WHERE s."Name" = 'Git & GitHub';
    IF v_comm_id IS NOT NULL THEN
        INSERT INTO skillbridge."CommunityPosts" ("CommunityId", "CreatedByUserId", "Title", "Content", "CreatedAt", "IsHidden")
        VALUES (
            v_comm_id, 'demo-usr-04',
            'Using git bisect to Find Regressions in 7 Steps Instead of 200',
            E'## Binary Search Your Commit History\nWhen a bug mysteriously appears and there have been 120 commits since last week''s working tag, don''t check them one by one!\n\nRun **git bisect start**, mark the current commit **git bisect bad**, and mark last week''s tag **git bisect good**. Git checks out the midpoint automatically—finding the exact offending commit in `log2(120) ≈ 7` checks!',
            NOW() - INTERVAL '1 day 16 hours', false
        ) RETURNING "Id" INTO v_post_id;

        INSERT INTO skillbridge."CommunityComments" ("PostId", "CreatedByUserId", "Content", "CreatedAt", "IsHidden") VALUES
        (v_post_id, 'demo-usr-01', 'Pro tip: pair it with `git bisect run npm test` and Git will find the broken commit completely hands-free while you grab coffee!', NOW() - INTERVAL '1 day 12 hours', false),
        (v_post_id, 'demo-usr-05', 'Wait, you can automate git bisect with a test script?! Mind blown.', NOW() - INTERVAL '1 day 9 hours', false);
    END IF;

    -- Post 12: Excel Community (by Hasibur Srijon)
    SELECT c."Id" INTO v_comm_id FROM skillbridge."Communities" c JOIN skillbridge."Skills" s ON s."Id" = c."SkillId" WHERE s."Name" = 'Excel';
    IF v_comm_id IS NOT NULL THEN
        INSERT INTO skillbridge."CommunityPosts" ("CommunityId", "CreatedByUserId", "Title", "Content", "CreatedAt", "IsHidden")
        VALUES (
            v_comm_id, 'demo-usr-06',
            'Retiring VLOOKUP: Why XLOOKUP + FILTER Are Game Changers',
            E'## Resilient Spreadsheets\nLegacy `VLOOKUP` breaks the moment someone inserts a new column into your source table because of hardcoded column index numbers.\n\n**XLOOKUP** references the lookup and return arrays directly, defaults to exact match, handles missing values natively without `IFERROR` wrappers, and can search bottom-to-top!',
            NOW() - INTERVAL '1 day 8 hours', false
        ) RETURNING "Id" INTO v_post_id;

        INSERT INTO skillbridge."CommunityComments" ("PostId", "CreatedByUserId", "Content", "CreatedAt", "IsHidden") VALUES
        (v_post_id, 'demo-usr-07', 'Dynamic array spill ranges with FILTER() eliminated 4 manual helper tabs from my resource capacity workbook.', NOW() - INTERVAL '1 day 4 hours', false),
        (v_post_id, 'demo-usr-01', 'And no more counting columns from A to Z with your finger on the monitor!', NOW() - INTERVAL '22 hours', false);
    END IF;

    -- Post 13: Public Speaking Community (by Nafiz Tonmoy)
    SELECT c."Id" INTO v_comm_id FROM skillbridge."Communities" c JOIN skillbridge."Skills" s ON s."Id" = c."SkillId" WHERE s."Name" = 'Public Speaking';
    IF v_comm_id IS NOT NULL THEN
        INSERT INTO skillbridge."CommunityPosts" ("CommunityId", "CreatedByUserId", "Title", "Content", "CreatedAt", "IsHidden")
        VALUES (
            v_comm_id, 'demo-usr-07',
            'How to Ace Live Technical Demos & Interviews: The What-So What-Now What Framework',
            E'## Structure Beats Memorization\nWhen presenting a software project to an interviewer or stakeholder, avoid rambling through every menu item.\n\nUse the **3-Part Pitch**:\n1. **What**: The core problem the platform solves (e.g., reciprocal peer skill exchange).\n2. **So What**: The engineering & UX decisions that make it trustworthy (atomic state machines, verified ratings, responsive design).\n3. **Now What**: Live walkthrough of a real user flow in under 2 minutes.',
            NOW() - INTERVAL '18 hours', false
        ) RETURNING "Id" INTO v_post_id;

        INSERT INTO skillbridge."CommunityComments" ("PostId", "CreatedByUserId", "Content", "CreatedAt", "IsHidden") VALUES
        (v_post_id, 'demo-usr-05', 'Used this exact framing in my capstone presentation last week and the panel loved how concise it was.', NOW() - INTERVAL '15 hours', false),
        (v_post_id, 'demo-usr-03', 'Pausing for 2 full seconds instead of saying "um" between sections also makes you sound 10x more confident.', NOW() - INTERVAL '12 hours', false);
    END IF;

    -- Post 14: MATLAB Community (by Abdullah Ishtiaq)
    SELECT c."Id" INTO v_comm_id FROM skillbridge."Communities" c JOIN skillbridge."Skills" s ON s."Id" = c."SkillId" WHERE s."Name" = 'MATLAB';
    IF v_comm_id IS NOT NULL THEN
        INSERT INTO skillbridge."CommunityPosts" ("CommunityId", "CreatedByUserId", "Title", "Content", "CreatedAt", "IsHidden")
        VALUES (
            v_comm_id, 'demo-usr-05',
            'Vectorizing Nested For-Loops in MATLAB for 20x Faster Kinematics Simulations',
            E'## Pre-Allocation & Element-Wise Operations\nIf your MATLAB simulation takes 30 seconds to run, check if you are dynamically growing an array inside a `for` loop.\n\nAlways pre-allocate matrices with **zeros(N, M)** and use element-wise operators (`.*`, `.^`, `./`) so MATLAB delegates the computation directly to multithreaded BLAS routines.',
            NOW() - INTERVAL '10 hours', false
        ) RETURNING "Id" INTO v_post_id;

        INSERT INTO skillbridge."CommunityComments" ("PostId", "CreatedByUserId", "Content", "CreatedAt", "IsHidden") VALUES
        (v_post_id, 'demo-usr-06', 'Same principle applies to NumPy in Python! Vectorized array operations beat interpreted loops every time.', NOW() - INTERVAL '7 hours', false),
        (v_post_id, 'demo-usr-04', 'Pre-allocating our trajectory matrix cut simulation time from 14s down to 0.4s.', NOW() - INTERVAL '5 hours', false);
    END IF;

    -- Post 15: Technical Writing Community (by Nafiz Tonmoy)
    SELECT c."Id" INTO v_comm_id FROM skillbridge."Communities" c JOIN skillbridge."Skills" s ON s."Id" = c."SkillId" WHERE s."Name" = 'Technical Writing';
    IF v_comm_id IS NOT NULL THEN
        INSERT INTO skillbridge."CommunityPosts" ("CommunityId", "CreatedByUserId", "Title", "Content", "CreatedAt", "IsHidden")
        VALUES (
            v_comm_id, 'demo-usr-07',
            'Writing Architecture Decision Records (ADRs) That Future Teammates Actually Read',
            E'## Capture the "Why", Not Just the "What"\nCode tells you *how* a system works today, but only good documentation tells you *why* alternative approaches were rejected.\n\nKeep ADRs under 1 page with four sections: **Context**, **Options Considered**, **Decision**, and **Trade-offs Accepted**.',
            NOW() - INTERVAL '4 hours', false
        ) RETURNING "Id" INTO v_post_id;

        INSERT INTO skillbridge."CommunityComments" ("PostId", "CreatedByUserId", "Content", "CreatedAt", "IsHidden") VALUES
        (v_post_id, 'demo-usr-02', 'The "Trade-offs Accepted" section is gold—it stops new engineers from re-proposing an approach that was already tested and ruled out.', NOW() - INTERVAL '2 hours', false),
        (v_post_id, 'demo-usr-04', 'Short, markdown-based docs right inside the repo always stay way more up to date than external wikis.', NOW() - INTERVAL '1 hour', false);
    END IF;
END $$;

COMMIT;
