# ADR 0004: Use SSE for live updates

## Context
The UI receives one-way health, event, and deployment notifications.

## Decision
Use native EventSource and a scoped in-memory publisher for the single-instance MVP.

## Consequences
Browser reconnects are automatic and implementation is small. Multi-instance operation later requires a shared publisher and replay or refetch policy.
