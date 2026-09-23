# Architecture

Version 2 adds an OIDC boundary: Auth.js in Next.js holds the Keycloak session and proxies authenticated `/api/*` calls. ASP.NET validates Keycloak access tokens, resolves local users by issuer and subject, and checks project membership on project-scoped routes. See `docs/authentication.md` and ADR 0006.

One Git monorepo contains a Next.js frontend and an ASP.NET Core 10 backend. The backend is a modular monolith: Domain defines the model, Application contains impact traversal and the live event port, Infrastructure implements an in-memory event bus, Persistence implements EF Core/PostgreSQL, and API composes services and HTTP use cases. PostgreSQL is the source of truth. The topology query joins current components, dependencies, successful deployment versions, and runtime states into a UI DTO.

```mermaid
flowchart LR
  User[Developer or operator] --> Web[Next.js UI / Auth.js BFF / React Flow]
  Web -->|OIDC code + PKCE| IdP[Keycloak in development]
  Web -->|Bearer token, REST and SSE proxy| Api[ASP.NET Core API]
  Api -->|EF Core| Db[(PostgreSQL)]
  Api -->|publish| Bus[In-memory SSE bus]
  Bus -->|updates| Api
  Api -->|SSE through BFF| Web
  Agent[Demo telemetry client] -->|Runtime PUT| Api
```

Edges point from a dependent to the component it needs. The impact engine traverses edges in reverse and tracks visited nodes, so cycles terminate and each dependent appears once. It reports potential impact only.

SSE is scoped to a project and environment. It is in-memory, so multiple API instances would require a shared transport and replay or refetch strategy. The browser refetches on initial SSE connection; while disconnected the UI still supports manual refresh. The telemetry boundary is the runtime update API; no external telemetry provider is connected yet.

The monolith keeps solo development and local integration simple while preserving clear boundaries for future source-control, deployment, and telemetry adapters. Work-item/component links and the merged project activity query connect planning records to the operational model without moving either into a new service. Dependency provenance and environment comparison are read queries over the same PostgreSQL model.
