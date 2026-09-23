# ADR 0010: Project-wide provenance and recorded environment comparison

## Context

Version 1 dependencies belong to a project, while deployments and runtime states belong to an environment. A comparison must not imply environment-specific dependency observations that the model does not contain.

## Decision

Allow one dependency record per source provenance for the same component pair and type. Compute potential drift from explicitly stored Declared and Observed records. Compare environments using recorded deployments and runtime states, and expose the project-wide dependency scope in both API responses and UI.

## Consequences

The queries provide useful foundations without inventing observations or component presence. Environment-specific dependency drift can be added later if ingestion records acquire an environment key.
