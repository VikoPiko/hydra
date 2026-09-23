# Vision

Distributed systems scatter deployment, health, and dependency information across tools. Developers, operators, and project stakeholders need a shared way to see what exists, where it runs, what changed, and what may be affected by a failure.

The MVP unifies project metadata, environments, components, dependency provenance, deployment records, current health and basic metrics, operational events, and planning work. Its board and topology let a developer move from a bug to the component it affects, then inspect recorded runtime context and potential downstream impact. Version 2 adds real project creation, OIDC sign-in, ownership, work items, and the planning-to-operations link while preserving the Version 1 live topology.

The work is now planned for one developer in incremental RUP Construction and Elaboration refinement slices. The goal remains a demonstrable unified operational model; persistence, project authorization, and board state were prioritized before topology comparisons and polish. See [iteration-plan-v2.md](iteration-plan-v2.md).

The product does not execute CI/CD, manage Kubernetes, store general logs or time series, diagnose root cause, or replace existing monitoring and source control products. Keycloak is a local OIDC provider for development and can be replaced without changing project ownership keys.
