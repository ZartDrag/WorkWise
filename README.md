# WorkWise – Work Request Tracker

A small full-stack app for tracking client work requests: list, filter, search, create, change status and add notes.

| Layer    | Tech |
|----------|------|
| API      | ASP.NET Core 8 Web API, CQRS (hand-written command/query handlers, no mediator), FluentValidation, RFC 7807 ProblemDetails |
| Data     | PostgreSQL 16, EF Core 8 + Npgsql (writes + migrations), Dapper (reads) |
| Frontend | React 18 + TypeScript (Vite), Redux Toolkit + RTK Query, React Hook Form, MUI |

```
WorkWise/
├── backend/
│   ├── WorkWise.sln
│   └── WorkWise.Api/
│       ├── Domain/                     # Entities + enums (WorkRequest, WorkRequestNote, Priority, Status)
│       ├── Application/
│       │   ├── Common/                 # ICommandHandler / IQueryHandler, NotFoundException, enum parsing
│       │   └── WorkRequests/
│       │       ├── Commands/           # CreateWorkRequest, UpdateWorkRequestStatus, AddWorkRequestNote (+ validators)
│       │       ├── Queries/            # ListWorkRequests, GetWorkRequestById (Dapper)
│       │       └── Dtos/               # Request / response DTOs (entities are never exposed)
│       ├── Infrastructure/
│       │   ├── Persistence/            # DbContext, migrations, dev seeding
│       │   └── Data/                   # Npgsql connection factory for Dapper
│       ├── Api/                        # Controller + middleware (correlation id, global exception → ProblemDetails)
│       └── Program.cs
├── workwise-web/                       # React frontend
├── db/schema.sql                       # Equivalent PostgreSQL DDL + indexes
└── docker-compose.yml                  # PostgreSQL 16
```

## Running locally

**Prerequisites:** .NET 8 SDK, Node.js 20+, Docker (or any local PostgreSQL 14+ with the `pg_trgm` extension available).

### 1. Database

```bash
docker compose up -d          # Postgres on localhost:5432, db "workwise", user/password postgres/postgres
```

Using your own Postgres? Update `ConnectionStrings:WorkWise` in `backend/WorkWise.Api/appsettings.json`
(or set the env var `ConnectionStrings__WorkWise`).

### 2. API (http://localhost:5080)

```bash
cd backend/WorkWise.Api
dotnet run                    # Development profile: applies EF migrations + seeds sample data on startup
```

Swagger UI: http://localhost:5080/swagger

To apply migrations manually instead: `dotnet tool install -g dotnet-ef` then `dotnet ef database update`
(or run `db/schema.sql` with psql).

### 3. Frontend (http://localhost:5173)

```bash
cd workwise-web
npm install
npm run dev
```

The API URL defaults to `http://localhost:5080`; override with `VITE_API_BASE_URL` (see `.env.example`).
CORS on the API allows `http://localhost:5173` (configurable via `Cors:AllowedOrigins`).

## API

| Method | Route | Description | Success |
|--------|-------|-------------|---------|
| GET    | `/api/work-requests?status=&search=&page=&pageSize=` | Paged list; optional status filter, search on title or client name (case-insensitive, contains). `page` defaults to 1, `pageSize` to 10 (max 100). Ordered by due date. | 200 |
| GET    | `/api/work-requests/{id}` | Single request with its notes | 200 |
| POST   | `/api/work-requests` | Create (`title`, `clientName`, `priority`, `status`, `dueDate` required; `description`, `notes` optional) | 201 + `Location` |
| PATCH  | `/api/work-requests/{id}/status` | `{ "status": "Blocked" }` | 200 |
| POST   | `/api/work-requests/{id}/notes` | `{ "content": "..." }` | 201 |

Example create body:

```json
{
  "title": "Build reporting dashboard",
  "clientName": "Acme Corp",
  "description": "KPI dashboard",
  "priority": "High",
  "status": "New",
  "dueDate": "2026-11-01",
  "notes": "Initial scope agreed"
}
```

Enums are exchanged as strings (`Low|Medium|High`, `New|InProgress|Blocked|Completed`, case-insensitive on input).
Numeric values or unknown names are rejected with a 400.

### Error format

Every error is an RFC 7807 `application/problem+json` body with `traceId` and `correlationId`
(the correlation id is also returned in the `X-Correlation-ID` response header, and is taken from the request header if supplied).

Validation errors (400) use `ValidationProblemDetails` with field-level messages:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "instance": "/api/work-requests",
  "errors": {
    "priority": ["Priority must be one of: Low, Medium, High."],
    "status": ["Status must be one of: New, InProgress, Blocked, Completed."]
  },
  "traceId": "00-84c576e312d300f4f4ec22d3733539e0-de05842d7224b5ed-00",
  "correlationId": "0HNP90MG2V9OA:00000001"
}
```

| Case | Status |
|------|--------|
| Missing title / client name / priority / status / due date | 400 (one entry per field) |
| Invalid priority or status (create, status update, list filter) | 400 |
| Malformed JSON or un-parseable date | 400 |
| `page < 1` or `pageSize` outside 1–100 | 400 |
| Unknown id (get, status update, add note) | 404 |
| Unhandled exception | 500 (generic message, details only in logs) |

## Architecture notes

- **CQRS without a mediator.** Each use case is its own small handler class implementing
  `ICommandHandler<TCommand, TResult>` or `IQueryHandler<TQuery, TResult>`, registered in DI and injected
  straight into the controller action with `[FromServices]`.
- **Writes** go through EF Core (change tracking, migrations, constraints). **Reads** use Dapper with hand-written
  SQL that maps directly into read DTOs, so the list query (filter + search + paging + note count) is one round trip.
- **Validation** lives on the commands/queries (FluentValidation). Handlers call `ValidateAndThrowAsync`, and the
  global exception middleware turns `ValidationException` into `ValidationProblemDetails`.
  The React form mirrors the same rules (required fields, max lengths, allowed values) and maps any server
  field errors back onto the form.
- **Enums** are stored as text with CHECK constraints, so the data stays readable and invalid values can't get in.
- **Seeding:** 6 sample requests (some with notes) are inserted in Development when the table is empty.

## Database schema and indexes

Tables: `work_requests` (PK `id`) and `work_request_notes` (PK `id`, FK `work_request_id → work_requests.id`,
`ON DELETE CASCADE`, one-to-many). Full DDL: [`db/schema.sql`](db/schema.sql).

| Index | Why |
|-------|-----|
| `ix_work_requests_status_due_date (status, due_date)` | The main screen filters by status and sorts by due date. With status as the leading column, Postgres finds the matching rows and reads them already in due-date order, so no separate sort is needed. |
| `ix_work_requests_due_date (due_date)` | The unfiltered list (the default view) is ordered by due date; this index serves that sort and the `LIMIT/OFFSET` page without scanning and sorting the whole table. |
| `ix_work_requests_title_trgm` / `ix_work_requests_client_name_trgm` (GIN, `gin_trgm_ops`) | Search is a case-insensitive "contains" (`ILIKE '%term%'`). A B-tree or `lower()` index can't be used with a leading wildcard; a `pg_trgm` GIN index can, so search stays fast as the table grows. |
| `ix_work_request_notes_work_request_id_created_at (work_request_id, created_at)` | Loads a request's notes already in chronological order, makes the per-row note count cheap, and indexes the foreign key (Postgres doesn't do this automatically), which also speeds up cascading deletes. |

## Scope and known limitations

- No authentication/authorization (not part of the brief).
- No automated test projects (per instructions); all endpoints and UI flows were tested manually.
- Any status transition is allowed (the brief defines no workflow rules).
- Offset pagination; keyset pagination would be better for very large tables.
- With only seed data the planner will choose sequential scans; the indexes pay off as data grows.
