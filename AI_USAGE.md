# AI Usage

## Tools

- **Cursor** (AI coding agent in the Cursor editor): implementation, verification and code review.
- **ChatGPT**: understanding the assignment PDF, planning the workflow, writing implementation prompts for Cursor,
  and reviewing Cursor's reports and code as a second opinion.

## What AI was used for

- Understanding the brief and turning it into a plan (ChatGPT and Cursor): technology choices, folder structure, and
  twelve milestones (backend foundation, classes API, holds API, expiry job, backend tests, backend Docker, frontend
  foundation, admin panel, parent portal, frontend Docker, frontend tests, documentation).
- Writing code for the backend, frontend, tests, Dockerfiles and `docker-compose.yml`, and drafting these documents
  (Cursor).
- Verifying each milestone (Cursor): building, running the test suites, sending real HTTP requests (including invalid
  input and simultaneous-request bursts) to the API running against PostgreSQL in Docker, and clicking through the
  UI in a browser.
- Reviewing the finished code against the assignment requirements (Cursor), and getting an independent second
  opinion on those review reports to decide what to fix next (ChatGPT).

I worked milestone by milestone: the AI implemented and verified one step, I reviewed the summary and changes, and
only then approved committing it.

## Where AI made mistakes and how they were caught

1. **`GET /classes` returned `500`.** The first version sorted after projecting into a result record, which EF Core
   cannot translate to SQL. A functional test script caught the 500; the server log showed the translation error. Fixed
   by sorting the entities before the projection.

2. **Expected errors were logged as crashes.** Every `400`/`404`/`409` also produced an "unhandled exception" error
   log from ASP.NET Core's exception middleware, which would hide real failures. Noticed while reading the logs. That
   logger is now silenced and genuine `500`s are still logged by the app's own exception handler.

3. **The first concurrency check may not have been concurrent.** The load script ran in Windows PowerShell 5.1,
   whose HTTP stack allows only two connections per server by default, so "100 simultaneous requests" could have run
   two at a time. The limit was raised and the burst repeated; PostgreSQL then reported real concurrent connections.

4. **Connection pool exhaustion.** Under that real burst, the backend's default pool (100 connections) used every
   PostgreSQL connection slot, so even `psql` could not connect. Fixed by capping the pool at 30 per instance and
   returning `503 SERVICE_BUSY` for transient database errors instead of `500`. A burst split across two backend
   instances was then run to confirm no overbooking and no connection errors.

5. **Proving the tests can fail.** Passing tests alone do not prove much, so the main protections were removed on
   purpose: without the `FOR UPDATE` lock the concurrency tests failed (19 holds for 10 seats, 30 for 20), and
   breaking the frontend expiry logic made its test fail. Both changes were reverted.

6. **Payment and cancel success messages never appeared.** The toasts were in per-call mutation callbacks, but a
   successful payment changes the status, which unmounts the dialog, and TanStack Query skips those callbacks for
   unmounted components. Found while testing in the browser; moved into the mutation hooks, which always run.

7. **A stale development server hid the real result.** A leftover Vite dev server was still listening on
   `localhost:3000` (IPv6), so the browser showed it instead of the Docker frontend. Found by checking which process
   owned the port; it was stopped and the Docker frontend re-tested.

8. **Race between confirmation and expiry (found in the final review).** Confirming read the current time before
   waiting for the database lock and did not lock the class. A confirmation delayed past the deadline could therefore
   still succeed after the seat had been given to another student, leaving the class one seat over its limit. This
   was found by reviewing the code against the seat-limit rule, then reproduced with a PostgreSQL test that holds the
   row lock to delay a confirmation past expiry while another student applies (the test failed: the expired hold was
   confirmed). Fixed by locking the class row and then the hold row, and reading the clock only after the locks are
   held. The same test now passes, alongside a mixed confirm/cancel/apply load test that checks for deadlocks.

## How AI output was checked

- No milestone was accepted on the basis of compiling alone: each was run against a real PostgreSQL database, and
  the UI was exercised in a browser against the Docker stack.
- All backend tests run against real PostgreSQL (Testcontainers or the compose database), never an in-memory
  database, because locking behaviour is what matters.
- Generated dependencies were checked (for example, that the `cn` package used by the generated shadcn/ui code is the
  legitimate one), and `npm audit` reports no vulnerabilities in production dependencies.
- A clean start (`docker compose up` with a new project name and an empty volume) was tested to simulate a
  reviewer's machine.
