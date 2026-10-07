-- Run after 001_initial.sql. Safe to rerun when the catalogue needs refreshing.
BEGIN;

INSERT INTO skillbridge."SkillCategories" ("Name", "Description") VALUES
    ('Programming', 'Software development related skills'),
    ('Engineering Software', 'Engineering and design tools'),
    ('Data Science / Analytics', 'Data analysis and processing skills')
ON CONFLICT ("Name") DO UPDATE SET "Description" = EXCLUDED."Description";

INSERT INTO skillbridge."Skills" ("Name", "SkillCategoryId")
SELECT item.name, category."Id"
FROM (VALUES
    ('Python', 'Programming'), ('C++', 'Programming'), ('Java', 'Programming'),
    ('AutoCAD', 'Engineering Software'), ('MATLAB', 'Engineering Software'),
    ('SolidWorks', 'Engineering Software'),
    ('Statistics', 'Data Science / Analytics'), ('Excel', 'Data Science / Analytics'),
    ('SQL', 'Data Science / Analytics')
) AS item(name, category_name)
JOIN skillbridge."SkillCategories" AS category ON category."Name" = item.category_name
WHERE true
ON CONFLICT ("Name") DO UPDATE SET "SkillCategoryId" = EXCLUDED."SkillCategoryId";

INSERT INTO skillbridge."SkillStages" ("SkillId", "StageNumber", "Description")
SELECT skill."Id", stage.number, stage.description
FROM (VALUES
    ('Python', 1, 'Python Basics & Syntax'),
    ('Python', 2, 'Variables, Data Types & Operators'),
    ('Python', 3, 'Control Flow & Loops'),
    ('Python', 4, 'Functions & Modularization'),
    ('Python', 5, 'Data Structures (Lists, Dicts, Sets, Tuples)'),
    ('Python', 6, 'File Handling & Exception Management'),
    ('Python', 7, 'Object-Oriented Programming & Mini Projects'),
    ('C++', 1, 'C++ Basics & Syntax'),
    ('C++', 2, 'Variables, Data Types & Operators'),
    ('C++', 3, 'Control Flow & Loops'),
    ('C++', 4, 'Functions & Overloading'),
    ('C++', 5, 'Arrays, Pointers & Strings'),
    ('C++', 6, 'Classes, Objects & Constructors'),
    ('C++', 7, 'Advanced OOP & Mini Projects'),
    ('Java', 1, 'Java Fundamentals & Syntax'),
    ('Java', 2, 'Variables, Data Types & Operators'),
    ('Java', 3, 'Control Flow & Conditional Logic'),
    ('Java', 4, 'Methods & Packages'),
    ('Java', 5, 'Arrays & Collections'),
    ('Java', 6, 'Classes, Objects & Inheritance'),
    ('Java', 7, 'Advanced OOP & Mini Projects'),
    ('AutoCAD', 1, 'AutoCAD Basics & Interface'),
    ('AutoCAD', 2, 'Drawing Objects & Shapes'),
    ('AutoCAD', 3, 'Object Modification Techniques'),
    ('AutoCAD', 4, 'Layers & Object Properties'),
    ('AutoCAD', 5, 'Annotation & Dimensioning'),
    ('AutoCAD', 6, 'Blocks, References & Templates'),
    ('AutoCAD', 7, 'Complete 2D Project'),
    ('MATLAB', 1, 'MATLAB Environment & Syntax'),
    ('MATLAB', 2, 'Variables, Arrays & Matrices'),
    ('MATLAB', 3, 'Scripts, Functions & Modularization'),
    ('MATLAB', 4, 'Visualization & Plotting'),
    ('MATLAB', 5, 'Control Flow & Logical Operations'),
    ('MATLAB', 6, 'Data Analysis & Simulations'),
    ('MATLAB', 7, 'Applied Project (Signal/Modeling)'),
    ('SolidWorks', 1, 'SolidWorks Basics & Interface'),
    ('SolidWorks', 2, 'Sketching & Feature Creation'),
    ('SolidWorks', 3, 'Part Modeling & Modifications'),
    ('SolidWorks', 4, 'Assemblies & Mates'),
    ('SolidWorks', 5, 'Technical Drawings & Detailing'),
    ('SolidWorks', 6, 'Simulation Basics'),
    ('SolidWorks', 7, 'Complete 3D Project'),
    ('Statistics', 1, 'Statistics Fundamentals & Data Types'),
    ('Statistics', 2, 'Descriptive Measures (Mean, Median, Mode)'),
    ('Statistics', 3, 'Probability Concepts'),
    ('Statistics', 4, 'Distributions (Normal, Binomial, etc.)'),
    ('Statistics', 5, 'Hypothesis Testing'),
    ('Statistics', 6, 'Correlation & Regression Analysis'),
    ('Statistics', 7, 'Applied Data Analysis Project'),
    ('Excel', 1, 'Excel Fundamentals & Interface'),
    ('Excel', 2, 'Data Entry, Cleaning & Formatting'),
    ('Excel', 3, 'Formulas & Functions (IF, VLOOKUP, INDEX-MATCH)'),
    ('Excel', 4, 'Charts & Visualization Techniques'),
    ('Excel', 5, 'Pivot Tables & Data Summarization'),
    ('Excel', 6, 'Advanced Formulas & Conditional Formatting'),
    ('Excel', 7, 'Data Analysis Mini Project'),
    ('SQL', 1, 'Database Fundamentals & SQL Syntax'),
    ('SQL', 2, 'Basic Queries (SELECT, WHERE, ORDER BY)'),
    ('SQL', 3, 'Joins & Subqueries'),
    ('SQL', 4, 'Aggregations & GROUP BY'),
    ('SQL', 5, 'Table Design & Modifications'),
    ('SQL', 6, 'Indexes, Views & Stored Procedures'),
    ('SQL', 7, 'SQL Project (Database Design + Queries)')
) AS stage(skill_name, number, description)
JOIN skillbridge."Skills" AS skill ON skill."Name" = stage.skill_name
WHERE true
ON CONFLICT ("SkillId", "StageNumber") DO UPDATE SET "Description" = EXCLUDED."Description";

INSERT INTO skillbridge."Communities" ("SkillId", "Name", "Description")
SELECT "Id", "Name", 'Community for skill ' || "Name"
FROM skillbridge."Skills"
WHERE true
ON CONFLICT ("SkillId") DO NOTHING;

COMMIT;
