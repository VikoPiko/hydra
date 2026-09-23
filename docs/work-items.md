# Work items

Work items belong to projects and use a global PostgreSQL identity sequence for human-readable numbers. The primary key remains a GUID. Types are UserStory, Task, and Bug; states are New, Active, Resolved, Closed, and Removed; priorities are Critical, High, Medium, and Low. Optional fields include assignee, parent, story points, estimates, area, and iteration. Descriptions are stored as plain text.

The API exposes `GET/POST /api/projects/{projectId}/work-items`, `GET/PUT /api/work-items/{id}`, `PATCH /api/work-items/{id}/state`, and `PATCH /api/work-items/{id}/assignment`. List filters include `type`, `state`, `priority`, `assignedTo=me` or a user GUID, `iteration=current` or `backlog` or an iteration GUID, and `search`. Results are capped at 500. `GET /api/work-items/{id}/activity` returns append-only audit entries. Basic iteration creation and listing are under `/api/projects/{projectId}/iterations`.

Only project members may read items; Contributors and Owners may create or change them. An assignee must be a member of the same project, and parent and iteration references must stay in the project. A state change updates `ClosedAt` for Closed or Removed and appends an activity in the same database save. Removed items are omitted from the default list but remain queryable with `?state=Removed`.

The additive `WorkItems` migration creates the sequence-backed table and indexes. The board slice adds the creation and editing UI plus project-scoped tags through the `BoardTags` migration. Comments and component links belong to later Version 2 slices.
