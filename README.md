# School Admission System

First-come, first-served online school admission with time-limited seat holds.

School administrators create classes (each class/shift is its own admission option with its own seat limit).
Parents identify a student by the 17-digit birth registration number, apply for a class, get a seat held for a
limited time, and confirm admission by (simulated) payment before the hold expires. Seats are never oversold, even
under many simultaneous applications.

| Part | Technology |
|---|---|
| Backend | ASP.NET Core Web API on .NET 9, EF Core 9 with Npgsql, FluentValidation |
| Database | PostgreSQL 16 |
| Frontend | React 19, TypeScript, Vite, Tailwind CSS, shadcn/ui (Base UI), TanStack Query, React Router |
| Tests | xUnit + Testcontainers (backend, real PostgreSQL), Vitest + React Testing Library (frontend) |
| Runtime | Docker Compose: `postgres`, `backend`, `frontend` (nginx) |

## Running the project

Requirements: Docker with Docker Compose v2. Nothing else needs to be installed.

```bash
docker compose up
```

The first run builds the backend and frontend images, which takes a few minutes. Then open:

| What | URL |
|---|---|
| Parent portal (apply, pay, cancel) | http://localhost:3000 |
| Parent's applications | http://localhost:3000/applications |
| Admin dashboard (create and monitor classes) | http://localhost:3000/admin |
| Backend API | http://localhost:8080 |
| Backend health check | http://localhost:8080/health |

Ports `3000` and `8080` must be free on the host.

The backend applies database migrations automatically on startup (retrying while PostgreSQL is starting), so no
manual database step is needed. Data is stored in the named Docker volume `postgres-data`
(`school-admission_postgres-data`) and survives `docker compose down` / `up` and container restarts.

Useful commands:

```bash
docker compose up -d --build   # rebuild after code changes, run in the background
docker compose logs -f backend # follow backend logs
docker compose down            # stop; data is kept
docker compose down -v         # stop and DELETE all data (removes the volume)
```

### Trying the workflow

1. Open http://localhost:3000/admin and create a class, e.g. `Class 6 - Morning Shift` with 120 seats.
2. Open http://localhost:3000, enter a 17-digit birth registration number (e.g. `20152691234567801`) and click
   **Apply and reserve seat**. The application page shows the countdown.
3. Click **Pay admission fee** → **Pay and confirm** to confirm, or **Cancel application** to release the seat.
4. If the countdown reaches zero, the page shows **Reservation expired** and payment is disabled.
5. **My applications** lists all of the student's applications and their current status.

To see expiry quickly, start with a short hold duration:

```bash
HOLD_DURATION_SECONDS=60 docker compose up -d --build backend   # bash
$env:HOLD_DURATION_SECONDS=60; docker compose up -d --build backend   # PowerShell
```

## Environment variables

`docker compose up` works without any configuration. To override defaults, copy `.env.example` to `.env` (read
automatically by Docker Compose) or set the variables in the shell.

| Variable | Default | Used by | Purpose |
|---|---|---|---|
| `POSTGRES_DB` | `school_admission` | postgres, backend | Database name |
| `POSTGRES_USER` | `postgres` | postgres, backend | Database user |
| `POSTGRES_PASSWORD` | `postgres` | postgres, backend | Database password (local development default only) |
| `ConnectionStrings__DefaultConnection` | built by compose from the `POSTGRES_*` values | backend | PostgreSQL connection string. Required when running the backend outside Docker. |
| `HOLD_DURATION_SECONDS` | `600` | backend | How long a seat is held before it expires. Must be a positive whole number; the backend refuses to start otherwise. |
| `ASPNETCORE_ENVIRONMENT` | `Production` | backend | ASP.NET Core environment |
| `VITE_API_BASE_URL` | `http://localhost:8080` | frontend | Backend URL used **by the browser**, so it must be reachable from the user's machine (not the `backend` service name). |

`VITE_API_BASE_URL` is applied when the frontend container starts (it writes `/config.js`), so changing it only
needs a container restart, not an image rebuild. For `npm run dev` / `npm run build` outside Docker it is read from
the environment or a `frontend/.env` file in the usual Vite way.

## API overview

All bodies are JSON. Parent operations identify the student with the header
`X-Student-Id: <17-digit birth registration number>`. IDs are UUIDs.

| Method | Path | Header | Success | Main errors |
|---|---|---|---|---|
| `POST` | `/classes` | – | `201` class with seat stats | `400` invalid name/seats, `409` `CLASS_NAME_TAKEN` |
| `GET` | `/classes` | – | `200` list of classes with seat stats | – |
| `GET` | `/classes/{classId}` | – | `200` class with seat stats | `404` `CLASS_NOT_FOUND` |
| `POST` | `/classes/{classId}/holds` | required | `201` hold (`HELD`, `expiresAt`) | `400` missing/invalid ID, `404`, `409` `NO_SEATS_AVAILABLE` / `ALREADY_APPLIED` / `ALREADY_ADMITTED` |
| `GET` | `/holds/{holdId}` | optional | `200` hold with current status | `400` invalid ID format (if header sent), `403` `NOT_YOUR_APPLICATION` (if header sent), `404` |
| `POST` | `/holds/{holdId}/confirm` | required | `200` `{ holdId, classId, status: "CONFIRMED", confirmedAt }` (repeatable) | `400` missing/invalid ID, `403`, `404`, `409` `HOLD_EXPIRED` / `HOLD_CANCELLED` |
| `DELETE` | `/holds/{holdId}` | required | `204` (repeatable) | `400` missing/invalid ID, `403`, `404`, `409` `ADMISSION_ALREADY_CONFIRMED` / `HOLD_EXPIRED` |
| `GET` | `/me/holds` | required | `200` the student's holds, newest first, with `className` | `400` missing/invalid ID |

Class with seat statistics:

```json
{
  "id": "0193f0c2-…",
  "name": "Class 6 - Morning Shift",
  "totalSeats": 120,
  "heldSeats": 3,
  "confirmedSeats": 10,
  "availableSeats": 107,
  "createdAt": "2026-11-09T08:00:00Z"
}
```

Hold, as returned by `GET /holds/{holdId}` and `GET /me/holds`. `POST /classes/{classId}/holds` returns `holdId`,
`classId`, `className`, `studentId`, `status`, `expiresAt` and `createdAt` (no `confirmedAt`/`cancelledAt`).

```json
{
  "holdId": "0193f0c3-…",
  "classId": "0193f0c2-…",
  "className": "Class 6 - Morning Shift",
  "status": "HELD",
  "expiresAt": "2026-11-09T08:10:00Z",
  "createdAt": "2026-11-09T08:00:00Z",
  "confirmedAt": null,
  "cancelledAt": null
}
```

Every error uses the same shape, with `errors` only present for field validation:

```json
{ "error": "VALIDATION_FAILED", "message": "Total seats must be a whole number between 1 and 10000.", "errors": { "totalSeats": ["…"] } }
```

Statuses are exactly `HELD`, `CONFIRMED`, `EXPIRED` and `CANCELLED`. A `HELD` hold past its `expiresAt` is reported
as `EXPIRED` immediately, and its seat is free immediately, even before the background job stores the new status.

### How overbooking is prevented

- Applying for a class runs in one transaction that locks the class row (`SELECT … FOR UPDATE`), counts confirmed
  holds plus unexpired held holds, and inserts the new hold only if a seat is left. Applications for the same class
  are therefore handled one at a time, in arrival order, across any number of backend instances.
- Confirming or cancelling locks the class row (shared) and then the hold row, and checks expiry using the time
  *after* the locks are acquired, so a delayed confirmation can never revive a hold whose seat was already given away.
- A partial unique index on `(class_id, student_id)` for `HELD`/`CONFIRMED` rows enforces one active application per
  student per class at the database level. Check constraints enforce valid statuses and student IDs.
- A background service marks overdue holds `EXPIRED` every 5 seconds using `FOR UPDATE SKIP LOCKED`, so several
  backend instances can run it at the same time without conflicts.

## Running the tests

### Backend (xUnit, 47 tests)

The tests run against a real PostgreSQL database, never an in-memory substitute. They include the required
concurrency tests (for example 100 simultaneous applications for 10 seats, two API instances sharing one database,
and a confirmation delayed past expiry while another student applies).

With Docker only, using the compose PostgreSQL (the tests use a separate `school_admission_tests` database that is
recreated on every run, so application data is not touched):

```bash
docker compose --profile test run --rm --build backend-tests
```

With the .NET 9 SDK installed (Testcontainers starts a temporary PostgreSQL container, so Docker must be running):

```bash
cd backend
dotnet test
```

To use an existing PostgreSQL server instead of Testcontainers, set `TEST_DATABASE_CONNECTION` to a connection
string. The database named in it is dropped and recreated by the tests.

### Frontend (Vitest + React Testing Library, 29 tests)

With Docker only:

```bash
docker compose --profile test run --rm --build frontend-tests
```

With Node.js 24 installed:

```bash
cd frontend
npm ci
npm test            # run once
npm run test:watch  # watch mode
```

Other frontend checks: `npm run typecheck`, `npm run lint`, `npm run build`.

## Local development without Docker (optional)

Backend (needs the .NET 9 SDK and a PostgreSQL server; the compose PostgreSQL does not publish port 5432 to the host):

```bash
cd backend
# bash
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=school_admission;Username=postgres;Password=postgres" \
ASPNETCORE_URLS=http://localhost:8080 \
dotnet run --project src/SchoolAdmission.API
```

Frontend (needs Node.js 24; the dev server uses port 3000, so stop the Docker frontend first):

```bash
cd frontend
npm ci
npm run dev
```

## Repository layout

```
backend/
  src/SchoolAdmission.API             controllers, error handling, startup
  src/SchoolAdmission.Application     use cases (class and admission services), validation, DTOs
  src/SchoolAdmission.Core            entities and hold state rules
  src/SchoolAdmission.Infrastructure  EF Core, migrations, repositories, hold expiry background service
  tests/SchoolAdmission.Tests         domain, API, expiry, persistence and concurrency tests
  Dockerfile                          runtime and test images
frontend/
  src/pages                           parent and admin pages
  src/features                        classes, admissions and dashboard components, hooks and API calls
  src/components                      shared components and shadcn/ui primitives
  Dockerfile, nginx.conf              runtime (nginx) and test images
docker-compose.yml                    postgres, backend, frontend; backend-tests and frontend-tests (profile "test")
ASSUMPTIONS.md                        decisions where the brief was not explicit
AI_USAGE.md                           how AI tools were used
```
