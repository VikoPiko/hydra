# ADR 0009: Link work items to components

## Context

Version 2 should let developers move from a board item to the operational component it affects without changing the Version 1 topology model.

## Decision

Store a many-to-many `WorkItemComponentLink` with a composite key. Enforce that both records belong to the same project in the API. Append link changes to `WorkItemActivity`. Merge work-item activity with `OperationalEvent` for the project overview, while keeping each subsystem's existing persistence model.

## Consequences

Component details can show active linked work and the overview can show a unified chronology. Deployment linking and inferred causation remain outside this slice.
