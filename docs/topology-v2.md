# Topology and comparison foundations

The topology keeps the Version 1 graph, live runtime updates, and reverse impact analysis. Version 2 adds component type, health, and dependency source filters; existing name search remains available. An empty graph explains when a project has no components or filters hide them. Graph colors now use the light design tokens.

The Components and Environments pages let project members build a real topology through the UI. A keyboard-operable component selector can open the same detail view as selecting a graph node.

Dependencies keep their `Declared`, `Discovered`, or `Observed` source. The `DependencyProvenance` migration changes the unique key to `(SourceComponentId, TargetComponentId, Type, Source)`, so a declared edge and an observed edge can coexist. The API records `LastObservedAt` when an `Observed` edge is explicitly submitted. The caller is responsible for supplying a real observation; the application does not invent one.

`GET /api/projects/{projectId}/environments/{environmentId}/architecture-drift` groups edges by source, target, and type into `matching`, `declaredOnly`, `observedOnly`, and `discovered`. This is a **project-wide** comparison: dependencies are not scoped to environments in the current model. A declared-only edge means no observation has been recorded, not that the architecture is wrong. The topology page calls this “Potential architecture drift.”

`GET /api/projects/{projectId}/environment-comparison?leftEnvironmentId=...&rightEnvironmentId=...` returns each project component's latest successful deployed version, current recorded health, and whether any deployment or runtime record exists in each environment. A missing record does not prove the component is absent. Declared dependencies are shared project-wide and reported as such. `/projects/{projectId}/comparison` renders a table and a differences-only filter.

These endpoints were verified against both the in-memory integration suite and additive migrations on fresh and Version 1-upgraded PostgreSQL databases.
