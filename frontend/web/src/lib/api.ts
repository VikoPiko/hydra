export type Health = "Healthy" | "Degraded" | "Unhealthy" | "Unknown";
export type Project = { id: string; name: string; description: string; slug: string; visibility: "Private" | "Internal"; archivedAt: string | null };
export type Environment = { id: string; projectId: string; name: string; type: string };
export type Component = { id: string; projectId: string; name: string; description: string; type: string; repositoryUrl: string | null; owner: string | null };
export type Metrics = { cpuPercent: number | null; memoryPercent: number | null; requestRate: number | null; errorRatePercent: number | null; latencyP95Ms: number | null };
export type TopologyNode = { id: string; name: string; type: string; health: Health; version: string | null; description: string | null; repositoryUrl: string | null; owner: string | null; metrics: Metrics };
export type TopologyEdge = { id: string; source: string; target: string; type: string; sourceKind: string };
export type Topology = { projectId: string; environmentId: string; generatedAt: string; nodes: TopologyNode[]; edges: TopologyEdge[] };
export type RuntimeState = { componentId: string; healthStatus: Health; cpuPercent: number | null; memoryPercent: number | null; requestRate: number | null; errorRatePercent: number | null; latencyP95Ms: number | null };
export type OperationalEvent = { id: string; componentId: string | null; type: string; severity: string; title: string; description: string | null; occurredAt: string };
export type Deployment = { id: string; componentId: string; environmentId: string; version: string; status: string; startedAt: string; completedAt: string | null };
export type ImpactComponent = { id: string; name: string; distance: number };
export type Impact = { componentId: string; directDependents: ImpactComponent[]; indirectDependents: ImpactComponent[]; affectedPaths: { componentIds: string[] }[] };
export type WorkItemType = "UserStory" | "Task" | "Bug";
export type WorkItemState = "New" | "Active" | "Resolved" | "Closed" | "Removed";
export type WorkItemPriority = "Critical" | "High" | "Medium" | "Low";
export type WorkItem = { id: string; projectId: string; sequenceNumber: number; type: WorkItemType; title: string; description: string; state: WorkItemState; priority: WorkItemPriority; assignedToUserId: string | null; createdByUserId: string; parentId: string | null; storyPoints: number | null; originalEstimateHours: number | null; remainingWorkHours: number | null; iterationId: string | null; area: string | null; createdAt: string; updatedAt: string; closedAt: string | null };
export type WorkItemActivity = { id: string; type: string; fieldName: string | null; oldValue: string | null; newValue: string | null; occurredAt: string };
export type ProjectMember = { userId: string; displayName: string; email: string | null; role: "Owner" | "Contributor" | "Viewer" };
export type Iteration = { id: string; name: string; state: "Planned" | "Current" | "Completed" };
export type Tag = { id: string; name: string; colorToken: string | null };
export type WorkItemTag = { workItemId: string; tagId: string };
export type WorkItemComponentLink = { workItemId: string; componentId: string };
export type ProjectActivity = { id: string; occurredAt: string; kind: "Operational" | "WorkItem"; summary: string; actorName: string | null; componentId: string | null; workItemId: string | null; environmentId: string | null };
export type DriftEdge = { sourceComponentId: string; sourceName: string; targetComponentId: string; targetName: string; type: string; lastObservedAt: string | null };
export type Drift = { scope: "Project"; matching: DriftEdge[]; declaredOnly: DriftEdge[]; observedOnly: DriftEdge[]; discovered: DriftEdge[] };
export type ComponentComparison = { componentId: string; name: string; leftHasData: boolean; rightHasData: boolean; leftVersion: string | null; rightVersion: string | null; leftHealth: Health | null; rightHealth: Health | null; different: boolean };
export type EnvironmentComparison = { leftEnvironmentId: string; leftEnvironmentName: string; rightEnvironmentId: string; rightEnvironmentName: string; componentPresenceMeaning: string; dependencyScope: "Project"; declaredDependencyCount: number; components: ComponentComparison[] };

export const apiBase = "";

export async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(`${apiBase}${path}`, { cache: "no-store" });
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string } | null;
    throw new Error(problem?.detail ?? `API request failed (${response.status})`);
  }
  return response.json() as Promise<T>;
}

export async function postJson<T>(path: string, body?: unknown): Promise<T> {
  const response = await fetch(`${apiBase}${path}`, { method: "POST", headers: { "Content-Type": "application/json" }, body: body ? JSON.stringify(body) : undefined });
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string } | null;
    throw new Error(problem?.detail ?? `API request failed (${response.status})`);
  }
  return response.json() as Promise<T>;
}

export async function sendJson<T>(method: "POST" | "PUT" | "PATCH" | "DELETE", path: string, body?: unknown): Promise<T> {
  const response = await fetch(`${apiBase}${path}`, { method, headers: body === undefined ? undefined : { "Content-Type": "application/json" }, body: body === undefined ? undefined : JSON.stringify(body) });
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string } | null;
    throw new Error(problem?.detail ?? `API request failed (${response.status})`);
  }
  return response.status === 204 ? undefined as T : response.json() as Promise<T>;
}
