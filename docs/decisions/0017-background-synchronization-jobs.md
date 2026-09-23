# ADR 0017: Persisted background scans

Context: Repository scanning must not hold an HTTP request open, and failures must be visible.

Decision: Store scan and synchronization-job status in PostgreSQL; a hosted worker processes queued scans. Interrupted Running work is marked Failed on restart.

Consequences: The UI can show real phases and retry failures. The single-worker claim is suitable for the current one-instance deployment; multi-instance claiming needs a database lock or queue.
