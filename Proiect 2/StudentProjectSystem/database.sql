-- ============================================================
-- Student Project Administration System - PostgreSQL Schema
-- Run this in pgAdmin Query Tool or psql after:
--   CREATE DATABASE "StudentProjectDB";
-- ============================================================

DROP TABLE IF EXISTS "AuditLogs"     CASCADE;
DROP TABLE IF EXISTS "Notifications" CASCADE;
DROP TABLE IF EXISTS "Comments"      CASCADE;
DROP TABLE IF EXISTS "Grades"        CASCADE;
DROP TABLE IF EXISTS "Projects"      CASCADE;
DROP TABLE IF EXISTS "Users"         CASCADE;

-- USERS
CREATE TABLE "Users" (
    "UserId"       SERIAL        PRIMARY KEY,
    "FullName"     VARCHAR(150)  NOT NULL,
    "Email"        VARCHAR(200)  NOT NULL UNIQUE,
    "PasswordHash" VARCHAR(500)  NOT NULL,
    "Role"         VARCHAR(20)   NOT NULL CHECK ("Role" IN ('Student','Professor','Admin')),
    "IsActive"     BOOLEAN       NOT NULL DEFAULT TRUE,
    "CreatedAt"    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "UpdatedAt"    TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

-- PROJECTS
CREATE TABLE "Projects" (
    "ProjectId"   SERIAL        PRIMARY KEY,
    "Title"       VARCHAR(300)  NOT NULL,
    "Description" VARCHAR(2000) NOT NULL,
    "FileName"    VARCHAR(500),
    "FilePath"    VARCHAR(1000),
    "FileSize"    BIGINT,
    "UploadDate"  TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "Deadline"    TIMESTAMPTZ,
    "Status"      VARCHAR(30)   NOT NULL DEFAULT 'Submitted'
                                CHECK ("Status" IN ('Submitted','UnderReview','Graded','Archived')),
    "IsArchived"  BOOLEAN       NOT NULL DEFAULT FALSE,
    "StudentId"   INT           NOT NULL REFERENCES "Users"("UserId") ON DELETE RESTRICT
);

-- GRADES
CREATE TABLE "Grades" (
    "GradeId"     SERIAL        PRIMARY KEY,
    "Score"       NUMERIC(5,2)  NOT NULL CHECK ("Score" >= 1 AND "Score" <= 10),
    "Feedback"    VARCHAR(2000),
    "GradedAt"    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "UpdatedAt"   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ProjectId"   INT           NOT NULL UNIQUE REFERENCES "Projects"("ProjectId") ON DELETE CASCADE,
    "ProfessorId" INT           NOT NULL REFERENCES "Users"("UserId") ON DELETE RESTRICT
);

-- COMMENTS
CREATE TABLE "Comments" (
    "CommentId" SERIAL        PRIMARY KEY,
    "Content"   VARCHAR(2000) NOT NULL,
    "CreatedAt" TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ProjectId" INT           NOT NULL REFERENCES "Projects"("ProjectId") ON DELETE CASCADE,
    "AuthorId"  INT           NOT NULL REFERENCES "Users"("UserId") ON DELETE RESTRICT
);

-- NOTIFICATIONS
CREATE TABLE "Notifications" (
    "NotificationId" SERIAL       PRIMARY KEY,
    "Message"        VARCHAR(500) NOT NULL,
    "IsRead"         BOOLEAN      NOT NULL DEFAULT FALSE,
    "CreatedAt"      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "UserId"         INT          NOT NULL REFERENCES "Users"("UserId") ON DELETE CASCADE,
    "ProjectId"      INT          REFERENCES "Projects"("ProjectId") ON DELETE SET NULL
);

-- AUDIT LOGS
CREATE TABLE "AuditLogs" (
    "LogId"     SERIAL        PRIMARY KEY,
    "Action"    VARCHAR(200)  NOT NULL,
    "Details"   VARCHAR(1000),
    "IpAddress" VARCHAR(50),
    "CreatedAt" TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "UserId"    INT           REFERENCES "Users"("UserId") ON DELETE SET NULL
);

-- INDEXES
CREATE INDEX IX_Projects_StudentId   ON "Projects"("StudentId");
CREATE INDEX IX_Projects_Status      ON "Projects"("Status");
CREATE INDEX IX_Grades_ProfessorId   ON "Grades"("ProfessorId");
CREATE INDEX IX_Comments_ProjectId   ON "Comments"("ProjectId");
CREATE INDEX IX_Notifications_UserId ON "Notifications"("UserId");
CREATE INDEX IX_AuditLogs_UserId     ON "AuditLogs"("UserId");
CREATE INDEX IX_AuditLogs_CreatedAt  ON "AuditLogs"("CreatedAt");

-- SEED DATA
-- All accounts use password: Password123!
-- Hashes are BCrypt cost-11. The EF Core migration also seeds these automatically.
INSERT INTO "Users" ("FullName","Email","PasswordHash","Role","IsActive","CreatedAt","UpdatedAt") VALUES
('Administrator System','admin@university.ro',    '$2a$11$K7LWIJCgNjVJxZmjMTUmsuVkqxGl6VwT7ZbRbIlRXsNGVXfKTUK9i','Admin',    TRUE,'2026-01-01','2026-01-01'),
('Prof. Ionescu Andrei','ionescu@university.ro',  '$2a$11$K7LWIJCgNjVJxZmjMTUmsuVkqxGl6VwT7ZbRbIlRXsNGVXfKTUK9i','Professor',TRUE,'2026-01-01','2026-01-01'),
('Prof. Popescu Maria', 'popescu@university.ro',  '$2a$11$K7LWIJCgNjVJxZmjMTUmsuVkqxGl6VwT7ZbRbIlRXsNGVXfKTUK9i','Professor',TRUE,'2026-01-01','2026-01-01'),
('Mihai Georgescu',     'mihai.g@student.ro',     '$2a$11$K7LWIJCgNjVJxZmjMTUmsuVkqxGl6VwT7ZbRbIlRXsNGVXfKTUK9i','Student',  TRUE,'2026-01-01','2026-01-01'),
('Ana Constantin',      'ana.c@student.ro',        '$2a$11$K7LWIJCgNjVJxZmjMTUmsuVkqxGl6VwT7ZbRbIlRXsNGVXfKTUK9i','Student',  TRUE,'2026-01-01','2026-01-01'),
('Radu Florescu',       'radu.f@student.ro',       '$2a$11$K7LWIJCgNjVJxZmjMTUmsuVkqxGl6VwT7ZbRbIlRXsNGVXfKTUK9i','Student',  TRUE,'2026-01-01','2026-01-01');

INSERT INTO "Projects" ("Title","Description","StudentId","Status","UploadDate","IsArchived") VALUES
('Aplicatie Web de Gestionare Biblioteca','Sistem web pentru gestionarea imprumuturilor de carti dintr-o biblioteca universitara.',4,'Graded','2026-02-15',FALSE),
('Platforma E-Learning','Platforma online pentru cursuri si materiale educative interactive.',4,'Submitted','2026-03-01',FALSE),
('Sistem de Monitorizare IoT','Aplicatie pentru monitorizarea dispozitivelor IoT in timp real.',5,'UnderReview','2026-02-20',FALSE),
('Chatbot AI pentru Suport Tehnic','Chatbot bazat pe machine learning pentru suport tehnic automatizat.',6,'Submitted','2026-03-05',FALSE);

INSERT INTO "Grades" ("Score","Feedback","ProjectId","ProfessorId","GradedAt","UpdatedAt") VALUES
(9.5,'Proiect excelent! Implementare corecta si interfata bine realizata. Documentatia este completa.',1,2,'2026-02-28','2026-02-28');

INSERT INTO "Comments" ("Content","ProjectId","AuthorId","CreatedAt") VALUES
('Felicitari pentru structura codului! Cateva optimizari minore pot fi adaugate.',1,2,'2026-02-25'),
('Documentatia este bine realizata.',3,2,'2026-03-01');

INSERT INTO "Notifications" ("Message","UserId","ProjectId","IsRead") VALUES
('Proiectul "Aplicatie Web" a primit nota 9.5.',4,1,TRUE),
('Proiect nou incarcat: "Platforma E-Learning"',2,2,FALSE),
('Proiect nou incarcat: "Sistem de Monitorizare IoT"',2,3,FALSE);

INSERT INTO "AuditLogs" ("Action","Details","UserId") VALUES
('Login','User admin@university.ro logged in',1),
('Login','User ionescu@university.ro logged in',2),
('UploadProject','Aplicatie Web de Gestionare Biblioteca',4),
('GradeProject','Project 1 scored 9.5',2),
('UploadProject','Platforma E-Learning',4),
('UploadProject','Sistem de Monitorizare IoT',5),
('UploadProject','Chatbot AI pentru Suport Tehnic',6);

-- Quick check
SELECT 'Users' AS "Table",COUNT(*) AS "Rows" FROM "Users"
UNION ALL SELECT 'Projects',COUNT(*) FROM "Projects"
UNION ALL SELECT 'Grades',COUNT(*) FROM "Grades"
UNION ALL SELECT 'Comments',COUNT(*) FROM "Comments"
UNION ALL SELECT 'Notifications',COUNT(*) FROM "Notifications"
UNION ALL SELECT 'AuditLogs',COUNT(*) FROM "AuditLogs";
