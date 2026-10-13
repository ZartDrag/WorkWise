-- WorkWise – PostgreSQL schema
-- Equivalent to the EF Core migration "InitialCreate" (backend/WorkWise.Api/Infrastructure/Persistence/Migrations).
-- The API applies migrations automatically in Development; this file is a readable reference / manual setup script.

CREATE EXTENSION IF NOT EXISTS pg_trgm;   -- trigram matching for fast ILIKE '%term%' search

-- ---------------------------------------------------------------------------
-- work_requests
-- ---------------------------------------------------------------------------
CREATE TABLE work_requests (
    id           uuid                     NOT NULL,
    title        varchar(200)             NOT NULL,
    client_name  varchar(150)             NOT NULL,
    description  varchar(2000),
    priority     varchar(20)              NOT NULL,
    status       varchar(20)              NOT NULL,
    due_date     date                     NOT NULL,
    created_at   timestamp with time zone NOT NULL,
    updated_at   timestamp with time zone NOT NULL,
    CONSTRAINT pk_work_requests PRIMARY KEY (id),
    CONSTRAINT ck_work_requests_priority CHECK (priority IN ('Low', 'Medium', 'High')),
    CONSTRAINT ck_work_requests_status   CHECK (status   IN ('New', 'InProgress', 'Blocked', 'Completed'))
);

-- ---------------------------------------------------------------------------
-- work_request_notes (one work request -> many notes)
-- ---------------------------------------------------------------------------
CREATE TABLE work_request_notes (
    id               uuid                     NOT NULL,
    work_request_id  uuid                     NOT NULL,
    content          varchar(2000)            NOT NULL,
    created_at       timestamp with time zone NOT NULL,
    CONSTRAINT pk_work_request_notes PRIMARY KEY (id),
    CONSTRAINT fk_work_request_notes_work_requests_work_request_id
        FOREIGN KEY (work_request_id) REFERENCES work_requests (id) ON DELETE CASCADE
);

-- ---------------------------------------------------------------------------
-- Indexes
-- ---------------------------------------------------------------------------
-- Status filter + default "ORDER BY due_date" in one index scan.
CREATE INDEX ix_work_requests_status_due_date ON work_requests (status, due_date);

-- Unfiltered list ordered by due date (status index can't serve this as its leading column is status).
CREATE INDEX ix_work_requests_due_date ON work_requests (due_date);

-- Case-insensitive "contains" search (ILIKE '%term%') on title and client name.
-- A normal B-tree can't help with a leading wildcard; a GIN trigram index can.
CREATE INDEX ix_work_requests_title_trgm       ON work_requests USING gin (title gin_trgm_ops);
CREATE INDEX ix_work_requests_client_name_trgm ON work_requests USING gin (client_name gin_trgm_ops);

-- Load a request's notes in chronological order, count notes per request, and support the FK
-- (PostgreSQL does not index foreign keys automatically; this also speeds up cascading deletes).
CREATE INDEX ix_work_request_notes_work_request_id_created_at ON work_request_notes (work_request_id, created_at);
