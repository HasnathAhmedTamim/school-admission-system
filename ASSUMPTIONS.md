# Assumptions

Decisions made where the assignment did not specify behaviour, and why.

## Classes

- **Each class/shift is a separate admission option.** "Class 6 - Morning Shift" and "Class 6 - Day Shift" are two
  classes with their own seat limits, as the brief describes. The shift is part of the name; there is no separate
  shift field because the API only defines `name` and `totalSeats`.
- **Class names are unique, ignoring case and surrounding spaces.** Two options with the same name would be
  indistinguishable for parents. A duplicate returns `409 CLASS_NAME_TAKEN`.
- **Limits:** name 1–100 characters (trimmed), `totalSeats` a whole number from 1 to 10,000. These are generous for a
  school while rejecting obvious mistakes. They are enforced by request validation and again in the database: the
  name column is `varchar(100)` and a check constraint keeps `total_seats` between 1 and 10,000.
- **Classes cannot be edited or deleted.** The brief only defines create and read. Changing seat counts while holds
  exist would need its own rules.
- **No admin login.** The brief states that no login system is required, so the admin pages are open.

## Student identity

- **`X-Student-Id` must be exactly 17 ASCII digits** (surrounding whitespace is trimmed). A missing header returns
  `400 STUDENT_ID_REQUIRED`, any other format `400 INVALID_STUDENT_ID`. The number is not checked against any
  registry, since none exists in the brief.
- **`GET /holds/{holdId}` accepts the header but does not require it.** The brief lists the header as required for
  apply, confirm, cancel and `/me/holds`, but not for reading a hold. If the header is sent and belongs to another
  student, the API returns `403 NOT_YOUR_APPLICATION`. The UI always sends it. Hold IDs are UUID v7 values:
  time-ordered, but with 74 random bits, so they cannot practically be guessed.
- **The browser remembers the student ID** in `localStorage`, so refreshing the page or coming back later restores
  the student's applications. The parent can switch or forget the student at any time.

## Holds and statuses

- **One active application per student per class.** "Active" means `HELD` (not yet expired) or `CONFIRMED`. After a
  hold expires or is cancelled, the student may apply to the same class again. Old holds are kept as history and
  shown in "My applications".
- **The hold duration is fixed when the hold is created** (`expiresAt = createdAt + HOLD_DURATION_SECONDS`).
  Changing the variable only affects new holds.
- **An invalid `HOLD_DURATION_SECONDS`** (not a positive whole number) stops the backend at startup with a clear
  message, instead of silently falling back to 600 and running with a value nobody intended.
- **A hold expires exactly at `expiresAt`.** From that moment the API reports it as `EXPIRED` and its seat counts as
  free, even if the background job has not stored the new status yet. The job runs every 5 seconds and only keeps
  the stored status accurate; correctness never depends on it.
- **Confirming is idempotent.** Confirming an already confirmed hold returns `200` with the same result and the seat
  is counted once.
- **Confirming an expired hold returns `409 HOLD_EXPIRED`; a cancelled one `409 HOLD_CANCELLED`.**
- **Cancelling** is allowed only while the hold is `HELD` and not expired. Cancelling again returns `204`
  (idempotent, like confirm). A confirmed admission cannot be cancelled (`409 ADMISSION_ALREADY_CONFIRMED`) because
  the fee is treated as paid; refunds are outside the brief. Cancelling an expired hold returns `409 HOLD_EXPIRED`.
- **Payment is a single "Pay and confirm" action.** No amount or payment gateway is modelled, as the brief asks only
  for a simulation.

## Seat statistics

- `heldSeats` = holds that are `HELD` and not past `expiresAt`; `confirmedSeats` = `CONFIRMED` holds;
  `availableSeats` = `totalSeats - heldSeats - confirmedSeats`.
- Statistics are always calculated from the hold rows at request time rather than stored as counters, so they cannot
  drift out of sync with the holds.

## Concurrency and ordering

- **"First come" means the order in which application transactions reach the database**, not the time the parent
  clicked. Applying locks the class row (`SELECT … FOR UPDATE`), so applications for one class are processed one at a
  time in that order. Applications for different classes do not block each other.
- **Confirm and cancel lock the class row (shared) and then the hold row, and read the clock after locking.** A
  confirmation that is delayed (for example while waiting for a database connection under load) is judged at the
  time it actually runs, so it cannot confirm a hold whose seat was already given to someone else. Applying,
  confirming and cancelling all take the class lock before any hold lock, so they cannot deadlock each other. The
  expiry job is the only exception: it locks hold rows without a class lock, but with `SKIP LOCKED`, so it never waits
  for a lock and cannot take part in a deadlock.
- **Several backend instances may run against one database.** All rules are enforced by PostgreSQL locks and
  constraints, not by in-memory state. The expiry job uses `FOR UPDATE SKIP LOCKED`, so instances do not block each
  other. Server clocks are assumed to be synchronised (NTP), since expiry is evaluated with each instance's clock.
- **Database connection pool: at most 30 connections per instance** (unless the connection string sets another
  value), so several instances stay under PostgreSQL's default limit of 100 connections. If the database is
  temporarily unavailable or the pool is exhausted, the API returns `503 SERVICE_BUSY` instead of a generic `500`.

## API details

- **IDs are UUID v7 values.** A malformed ID in the URL returns `404` (such a class or hold cannot exist) rather
  than `400`.
- **Status codes:** `400` invalid input, `403` someone else's hold, `404` unknown class or hold, `409` business rule
  conflicts (no seats, already applied, already admitted, expired, cancelled, already confirmed), `503` database busy.
  Errors from the API's own endpoints have the form `{ "error": CODE, "message": text, "errors"?: { field: [messages] } }`,
  so the UI can show a clear message. Two framework-level responses are the exception: a request with an unsupported
  content type gets ASP.NET Core's default `415` problem-details body, and a URL that matches no endpoint gets `404`
  with an empty body.
- **Responses include a few extra fields** beyond the examples in the brief (`className`, `createdAt`,
  `confirmedAt`, `cancelledAt`), which the UI needs. All fields from the brief are present.
- **All timestamps are UTC (ISO 8601).** The UI shows them in the user's local time.
- `GET /classes` is ordered by name; `GET /me/holds` is newest first.
- **CORS allows any origin.** The API uses no cookies or credentials (identity is the header), and the frontend may
  be served from a different host or port than the API.
- **Migrations run automatically at backend startup**, so `docker compose up` needs no manual database step.

## Frontend

- **Live updates use polling every 4 seconds** (class seat counts, the student's applications, and a held
  application). This keeps the backend a plain REST API and meets "status updates without a page reload".
- **The countdown runs locally** from `expiresAt` using the device clock. When it reaches zero the UI immediately
  shows the hold as expired, disables payment and closes an open payment dialog, then refreshes from the server. The
  server remains the authority: if the device clock is wrong, the next server response or confirm attempt corrects
  the display.
- **`VITE_API_BASE_URL` is applied when the frontend container starts** (written to `/config.js`), so the same image
  works with any backend URL without rebuilding. It must be a URL the user's browser can reach.
- The parent and admin areas are separate pages in one app (`/` and `/admin`), with no login (see above).
