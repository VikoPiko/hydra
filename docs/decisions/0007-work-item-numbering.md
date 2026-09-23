# ADR 0007: Global work-item numbers

Status: Accepted

Use a PostgreSQL identity column and a unique index for global work-item display numbers. Keep GUIDs as primary keys and project scoping for authorization. This avoids deriving numbers from row counts and remains safe under concurrent creates. The in-memory test provider does not generate this non-key value, so the migration was also verified against PostgreSQL.
