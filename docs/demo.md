# V3 discovery demo

1. Start the Compose stack and sign in as described in [README.md](../README.md).
2. Open Projects → Import existing project. Enter a unique project name and select the bundled Commerce System source.
3. Start the scan. The persisted status changes from Pending to Running to Succeeded or PartiallySucceeded.
4. Review eight service candidates, seven `depends_on` edges, their Compose source locations, and any warnings. Apply the scan.
5. Open Topology. Select an edge to see its `DockerCompose` evidence and `compose.yaml` location. Select a node to see its discovery origin and last scan time.
6. Open Components, correct a discovered component type, then open Integrations and run a second scan. Apply it; the correction remains and topology contains no duplicate nodes or edges.
7. Open Boards and drag a ticket to another state column. Reload to confirm that the move persisted. The Move menu remains available for keyboard use.

This slice does not require an external DevOps account. Azure DevOps synchronization and delivery traceability are planned as separate V3 slices; the demo does not fabricate pipeline or build records.
