# Version 3 iteration record

Version 3 extends the working Version 1 operations view and Version 2 planning workspace. The bundled Compose repository is the first complete discovery source; the external providers and delivery features in the V3 specification are subsequent slices. Each completed slice must pass backend build and tests, frontend lint and build, migration checks, and documentation review before the next slice.

| Slice | Delivered | Verification |
| --- | --- | --- |
| V3 board interaction | Ticket drag between New, Active, Resolved, and Closed with persisted state; keyboard Move menu retained | Browser moved a story Active → New → Active and reloaded after each move; backend tests and frontend gates passed |
| V3 repository foundation | Connection, scan, synchronization job, evidence and component provenance tables; Compose parser, background scan, preview and idempotent apply; import and integrations UI; topology evidence | Parser unit tests, authenticated endpoint integration test, fresh and V2-upgrade PostgreSQL migrations, EF model check, backend and frontend gates |

Next slices in specification order: real read-only repository source, Azure DevOps connection and build synchronization, delivery traceability, architecture intelligence, secondary parsers, and hardening. No external provider is represented as connected until credentials and live configuration exist.
