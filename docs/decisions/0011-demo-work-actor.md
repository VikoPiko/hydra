# ADR 0011: Use a local system actor for demo work

## Context

Seeded work items require a creator, but Commerce Demo must not acquire a fabricated human owner before anyone signs in.

## Decision

Development seeding creates a deterministic `UserProfile` with a seed-only issuer and no project membership. It uses this actor for example work-item history. The first real authenticated user can still claim ownership of an unowned Commerce Demo.

## Consequences

Example activity has an explicit actor and the seed remains repeatable. The actor cannot authenticate through Keycloak or receive project access through the seed.
