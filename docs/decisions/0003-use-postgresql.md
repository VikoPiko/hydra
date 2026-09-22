# ADR 0003: Use PostgreSQL

## Context
Projects, topology, runtime state, and history need relational constraints and querying.

## Decision
Store the operational model in PostgreSQL through EF Core migrations.

## Consequences
The team has one durable local database. No graph or time-series database is needed for the initial scope.
