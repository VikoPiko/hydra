# ADR 0002: Use a modular monolith

## Context
The platform's MVP needs several domains but no independent service scaling.

## Decision
Use one ASP.NET Core API with Domain, Application, Infrastructure, Persistence, and API projects.

## Consequences
Deployment and debugging stay simple. Module boundaries remain explicit; splitting services later requires concrete evidence.
