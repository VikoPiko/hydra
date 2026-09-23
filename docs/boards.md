# Boards

`/projects/{projectId}/boards` presents project work items in New, Active, Resolved, and Closed columns. Cards show the database-backed number, type, title, priority, assignee, optional story points, and tags. An explicit state selector persists a move through `PATCH /api/work-items/{id}/state`; Removed items leave the board and can be found in the backlog state filter.

The same page has a Backlog table with search and type, state, priority, assignee, and iteration filters. Selecting a card or row opens a detail panel with editable fields, tags, linked project components, and activity history. The New work item action creates a database record. Viewers can read the board and details; Contributors and Owners can edit. Assignment choices come from project membership. No board data is stored only in client state.

Tags are project-scoped with unique names and restricted palette tokens (`blue`, `green`, `amber`, `red`, `slate`, `purple`). The additive `BoardTags` migration stores tags and item associations. The following `OperationalIntegration` migration adds component links. Deployment correlation is shown through operational events; direct work-item/deployment links remain a later option.

A Compose browser smoke test created Bug #1 with an assignee, story points, and a tag; moved it to Active; and confirmed the state and card after reload. The project creation form also created a separate empty project and its first environment.

Development seeding now supplies Commerce Demo with a story, two tasks, an active payment bug, example tags, and a Payment Service link. Database-generated sequence numbers are global, so the visible numbers depend on existing work items and are not hard-coded.
