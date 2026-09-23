# Planning and operational context

Work items can link to one or more components in the same project. The board detail drawer manages these links. `WorkItemComponentLink` has a composite key, foreign keys to both records, and an index on `ComponentId`. Cross-project links are rejected. Linking and unlinking append work-item activity in the same database save.

Selecting a topology component shows its active linked work items. Closed and Removed items are omitted by default; the component API accepts `includeClosed=true` when history is needed. The board remains the place to edit a work item.

The project overview combines work metrics and a board snapshot with runtime health, deployments, and recent activity. `GET /api/projects/{projectId}/activity` merges existing operational events and work-item activity into a timestamp-ordered DTO with actor, target IDs, and summary. Deployment and health changes appear through their existing operational events. This is a read query over persisted records, not an event store. Time proximity between a deployment, health change, and bug is correlation, not proof of causation.

The `OperationalIntegration` migration is additive and was applied to both a clean PostgreSQL database and a database upgraded from Version 1.
