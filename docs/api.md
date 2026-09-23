# API overview

Project management (Version 2 slice): `GET /api/projects` lists active projects; `?includeArchived=true` includes archived records. `GET /api/projects/{id}` returns a project. `POST /api/projects` accepts `name`, optional `description`, optional lowercase `slug`, and `visibility` (`Private` or `Internal`). If omitted, a slug is generated from the name. `PUT /api/projects/{id}` updates those fields. `POST /api/projects/{id}/archive` archives without deleting related data. Duplicate names or slugs return 409; invalid fields return 400. The `/projects/new` UI uses the same API.

`GET /api/projects/{id}/members` lists members for any project member. Owners may use `POST /api/projects/{id}/members/by-email` with `email` and `role` after that person has signed in once. Components and Environments pages use the existing create/list APIs, and the Components form submits Declared dependencies.

Work-item endpoints, filters, state and assignment rules are documented in [work-items.md](work-items.md). All `/api/*` endpoints require a validated OIDC bearer token; project-scoped routes also require membership.

Operational integration: `GET /api/projects/{projectId}/work-item-component-links` lists project links. `POST` or `DELETE /api/work-items/{workItemId}/components/{componentId}` adds or removes a same-project link. `GET /api/components/{componentId}/work-items` lists active linked work; `?includeClosed=true` includes history. `GET /api/projects/{projectId}/activity?limit=50` merges persisted operational and work-item events. See [operational-integration.md](operational-integration.md).

Topology comparison: `GET /api/projects/{projectId}/environments/{environmentId}/architecture-drift` reports project-wide dependency provenance. `GET /api/projects/{projectId}/environment-comparison?leftEnvironmentId={id}&rightEnvironmentId={id}` compares recorded versions and health. Both are explained in [topology-v2.md](topology-v2.md).

V3 discovery: `POST /api/projects/import-demo` creates a private project, Production environment, bundled repository connection, and queued scan in one transaction. `GET /api/projects/{id}/repository-connections` lists safe connection metadata. Owners can create a bundled connection, request scans, apply completed previews, disconnect, and reconnect through `/api/projects/{id}/repository-connections/{connectionId}`. `GET .../scans?limit=25`, `GET .../scans/{scanId}`, and `GET .../scans/{scanId}/preview` expose persisted status, history, normalized candidates, warnings, and conflicts. `POST .../scans/{scanId}/apply` is idempotent. `PATCH /api/components/{componentId}/discovery-override` saves manual name/type corrections. The topology response includes component origin and an `evidence` array on each edge. See [discovery.md](discovery.md).

Development OpenAPI JSON is served at `/openapi/v1.json`. Responses use ordinary JSON DTOs; errors use Problem Details. All IDs are GUIDs and enum values are JSON strings.

| Capability | Endpoints |
| --- | --- |
| Projects | `GET /api/projects`, `GET /api/projects/{projectId}`, `POST /api/projects` |
| Environments | `GET`, `POST /api/projects/{projectId}/environments` |
| Components | `GET`, `POST /api/projects/{projectId}/components`; `GET /api/components/{componentId}` |
| Dependencies | `GET`, `POST /api/projects/{projectId}/dependencies` |
| Topology | `GET /api/projects/{projectId}/environments/{environmentId}/topology` |
| Topology comparison | `GET /api/projects/{projectId}/environments/{environmentId}/architecture-drift`, `GET /api/projects/{projectId}/environment-comparison` |
| Potential impact | `GET /api/projects/{projectId}/components/{componentId}/impact` |
| Deployments | `GET /api/projects/{projectId}/environments/{environmentId}/deployments`, `GET /api/components/{componentId}/deployments`, `POST /api/projects/{projectId}/deployments` |
| Runtime state | `GET /api/projects/{projectId}/environments/{environmentId}/runtime`, `PUT /api/projects/{projectId}/environments/{environmentId}/components/{componentId}/runtime` |
| Events | `GET /api/projects/{projectId}/environments/{environmentId}/events?limit=50` |
| Project activity | `GET /api/projects/{projectId}/activity?limit=50` |
| Component work | `GET /api/components/{componentId}/work-items`, `POST`/`DELETE /api/work-items/{workItemId}/components/{componentId}` |
| SSE | `GET /api/projects/{projectId}/environments/{environmentId}/events/stream` |
| Health | `GET /health/live`, `GET /health/ready`, `GET /health` |

The runtime PUT accepts `healthStatus`, `cpuPercent`, `memoryPercent`, `requestRate`, `errorRatePercent`, and `latencyP95Ms`. It emits `runtime-state-updated` and, on a health transition, `operational-event-created`. Deployment registration emits `deployment-updated`. SSE is one-way and in-memory.

In Development only, `POST /api/dev/simulate/degradation/{componentId}?environmentId={id}` and the matching `/recovery/` endpoint drive the live demo.
