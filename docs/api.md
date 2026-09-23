# API overview

Project management (Version 2 slice): `GET /api/projects` lists active projects; `?includeArchived=true` includes archived records. `GET /api/projects/{id}` returns a project. `POST /api/projects` accepts `name`, optional `description`, optional lowercase `slug`, and `visibility` (`Private` or `Internal`). If omitted, a slug is generated from the name. `PUT /api/projects/{id}` updates those fields. `POST /api/projects/{id}/archive` archives without deleting related data. Duplicate names or slugs return 409; invalid fields return 400. The `/projects/new` UI uses the same API.

Work-item endpoints, filters, state and assignment rules are documented in [work-items.md](work-items.md). All `/api/*` endpoints require a validated OIDC bearer token; project-scoped routes also require membership.

Operational integration: `GET /api/projects/{projectId}/work-item-component-links` lists project links. `POST` or `DELETE /api/work-items/{workItemId}/components/{componentId}` adds or removes a same-project link. `GET /api/components/{componentId}/work-items` lists active linked work; `?includeClosed=true` includes history. `GET /api/projects/{projectId}/activity?limit=50` merges persisted operational and work-item events. See [operational-integration.md](operational-integration.md).

Development OpenAPI JSON is served at `/openapi/v1.json`. Responses use ordinary JSON DTOs; errors use Problem Details. All IDs are GUIDs and enum values are JSON strings.

| Capability | Endpoints |
| --- | --- |
| Projects | `GET /api/projects`, `GET /api/projects/{projectId}`, `POST /api/projects` |
| Environments | `GET`, `POST /api/projects/{projectId}/environments` |
| Components | `GET`, `POST /api/projects/{projectId}/components`; `GET /api/components/{componentId}` |
| Dependencies | `GET`, `POST /api/projects/{projectId}/dependencies` |
| Topology | `GET /api/projects/{projectId}/environments/{environmentId}/topology` |
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
