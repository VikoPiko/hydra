# API overview

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
| SSE | `GET /api/projects/{projectId}/environments/{environmentId}/events/stream` |
| Health | `GET /health/live`, `GET /health/ready`, `GET /health` |

The runtime PUT accepts `healthStatus`, `cpuPercent`, `memoryPercent`, `requestRate`, `errorRatePercent`, and `latencyP95Ms`. It emits `runtime-state-updated` and, on a health transition, `operational-event-created`. Deployment registration emits `deployment-updated`. SSE is one-way and in-memory.

In Development only, `POST /api/dev/simulate/degradation/{componentId}?environmentId={id}` and the matching `/recovery/` endpoint drive the live demo.
