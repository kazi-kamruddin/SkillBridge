-- Apply after 008, before deploying this release. Safe to run again; existing skills and stages are kept.
BEGIN;

CREATE TABLE IF NOT EXISTS skillbridge."SkillSuggestions" (
    "Id" serial PRIMARY KEY,
    "UserId" varchar(128) NOT NULL REFERENCES skillbridge."AspNetUsers"("Id") ON DELETE CASCADE,
    "Name" varchar(100) NOT NULL,
    "CategoryName" varchar(100) NOT NULL,
    "Reason" varchar(500) NOT NULL,
    "Status" varchar(20) NOT NULL DEFAULT 'Pending',
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS "IX_SkillSuggestions_Status" ON skillbridge."SkillSuggestions"("Status");

INSERT INTO skillbridge."SkillCategories" ("Name", "Description") VALUES
    ('Web Development', 'Building and maintaining websites'),
    ('Creative Design', 'Visual design and content creation'),
    ('Communication', 'Speaking, writing, and language practice'),
    ('Workplace Skills', 'Planning, collaboration, and practical tools')
ON CONFLICT ("Name") DO NOTHING;

INSERT INTO skillbridge."Skills" ("Name", "SkillCategoryId")
SELECT item.name, category."Id"
FROM (VALUES
    ('JavaScript', 'Web Development'), ('HTML & CSS', 'Web Development'),
    ('React', 'Web Development'), ('Git & GitHub', 'Web Development'),
    ('UI/UX Design', 'Creative Design'), ('Graphic Design', 'Creative Design'),
    ('Video Editing', 'Creative Design'), ('Figma', 'Creative Design'),
    ('Public Speaking', 'Communication'), ('English Conversation', 'Communication'),
    ('Technical Writing', 'Communication'), ('Project Management', 'Workplace Skills')
) AS item(name, category_name)
JOIN skillbridge."SkillCategories" AS category ON category."Name" = item.category_name
WHERE true
ON CONFLICT ("Name") DO NOTHING;

INSERT INTO skillbridge."SkillStages" ("SkillId", "StageNumber", "Description")
SELECT skill."Id", stage.number, stage.description
FROM (VALUES
    ('JavaScript', 1, 'Syntax, variables and data types'), ('JavaScript', 2, 'Control flow and functions'),
    ('JavaScript', 3, 'Arrays and objects'), ('JavaScript', 4, 'DOM and events'),
    ('JavaScript', 5, 'Asynchronous code and APIs'), ('JavaScript', 6, 'Modules and tooling'),
    ('JavaScript', 7, 'Build a small interactive app'),
    ('HTML & CSS', 1, 'Semantic HTML'), ('HTML & CSS', 2, 'Forms and accessible content'),
    ('HTML & CSS', 3, 'Selectors and the box model'), ('HTML & CSS', 4, 'Flexbox and grid'),
    ('HTML & CSS', 5, 'Responsive layouts'), ('HTML & CSS', 6, 'Transitions and design systems'),
    ('HTML & CSS', 7, 'Build a responsive page'),
    ('React', 1, 'Components and JSX'), ('React', 2, 'Props and composition'),
    ('React', 3, 'State and events'), ('React', 4, 'Effects and data fetching'),
    ('React', 5, 'Forms and routing'), ('React', 6, 'Testing and accessibility'),
    ('React', 7, 'Build a complete interface'),
    ('Git & GitHub', 1, 'Repositories and commits'), ('Git & GitHub', 2, 'Branches and history'),
    ('Git & GitHub', 3, 'Merging and conflicts'), ('Git & GitHub', 4, 'Remote collaboration'),
    ('Git & GitHub', 5, 'Pull requests and review'), ('Git & GitHub', 6, 'Issues and project flow'),
    ('Git & GitHub', 7, 'Collaborative project practice'),
    ('UI/UX Design', 1, 'User goals and problem framing'), ('UI/UX Design', 2, 'Research and personas'),
    ('UI/UX Design', 3, 'Flows and information architecture'), ('UI/UX Design', 4, 'Wireframes'),
    ('UI/UX Design', 5, 'Visual hierarchy and interaction'), ('UI/UX Design', 6, 'Usability testing'),
    ('UI/UX Design', 7, 'Prototype and revise a flow'),
    ('Graphic Design', 1, 'Composition and visual hierarchy'), ('Graphic Design', 2, 'Color and contrast'),
    ('Graphic Design', 3, 'Typography'), ('Graphic Design', 4, 'Images and layout'),
    ('Graphic Design', 5, 'Brand consistency'), ('Graphic Design', 6, 'Exporting for different media'),
    ('Graphic Design', 7, 'Create a small design set'),
    ('Video Editing', 1, 'Organize footage and story'), ('Video Editing', 2, 'Cuts and pacing'),
    ('Video Editing', 3, 'Sound and voice'), ('Video Editing', 4, 'Titles and graphics'),
    ('Video Editing', 5, 'Color correction'), ('Video Editing', 6, 'Captions and accessibility'),
    ('Video Editing', 7, 'Edit and export a short video'),
    ('Figma', 1, 'Interface and frames'), ('Figma', 2, 'Shapes, text and styles'),
    ('Figma', 3, 'Auto layout'), ('Figma', 4, 'Components and variants'),
    ('Figma', 5, 'Prototype interactions'), ('Figma', 6, 'Team feedback and handoff'),
    ('Figma', 7, 'Build an interactive prototype'),
    ('Public Speaking', 1, 'Audience and purpose'), ('Public Speaking', 2, 'Structure a talk'),
    ('Public Speaking', 3, 'Voice and delivery'), ('Public Speaking', 4, 'Visual support'),
    ('Public Speaking', 5, 'Manage nerves and timing'), ('Public Speaking', 6, 'Questions and feedback'),
    ('Public Speaking', 7, 'Deliver a short talk'),
    ('English Conversation', 1, 'Greetings and introductions'), ('English Conversation', 2, 'Everyday questions'),
    ('English Conversation', 3, 'Listening and clarification'), ('English Conversation', 4, 'Describing experiences'),
    ('English Conversation', 5, 'Explaining opinions'), ('English Conversation', 6, 'Work and study scenarios'),
    ('English Conversation', 7, 'Sustained conversation practice'),
    ('Technical Writing', 1, 'Audience and writing goals'), ('Technical Writing', 2, 'Clear sentences and structure'),
    ('Technical Writing', 3, 'How-to instructions'), ('Technical Writing', 4, 'Examples and diagrams'),
    ('Technical Writing', 5, 'API and reference material'), ('Technical Writing', 6, 'Review and editing'),
    ('Technical Writing', 7, 'Publish a short guide'),
    ('Project Management', 1, 'Goals and scope'), ('Project Management', 2, 'Tasks and priorities'),
    ('Project Management', 3, 'Timelines and dependencies'), ('Project Management', 4, 'Team communication'),
    ('Project Management', 5, 'Risks and changes'), ('Project Management', 6, 'Tracking progress'),
    ('Project Management', 7, 'Plan and review a small project')
) AS stage(skill_name, number, description)
JOIN skillbridge."Skills" AS skill ON skill."Name" = stage.skill_name
WHERE true
ON CONFLICT ("SkillId", "StageNumber") DO NOTHING;

INSERT INTO skillbridge."Communities" ("SkillId", "Name", "Description")
SELECT skill."Id", skill."Name", 'Community for skill ' || skill."Name"
FROM skillbridge."Skills" AS skill
WHERE skill."Name" IN ('JavaScript', 'HTML & CSS', 'React', 'Git & GitHub',
    'UI/UX Design', 'Graphic Design', 'Video Editing', 'Figma', 'Public Speaking',
    'English Conversation', 'Technical Writing', 'Project Management')
ON CONFLICT ("SkillId") DO NOTHING;

COMMIT;
