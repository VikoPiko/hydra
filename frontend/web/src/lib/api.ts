export type Health = "Healthy" | "Degraded" | "Unhealthy" | "Unknown";
export type Project = { id: string; name: string; description: string };
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

export const apiBase = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

export async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(`${apiBase}${path}`, { cache: "no-store" });
  if (!response.ok) throw new Error(`API request failed (${response.status})`);
  return response.json() as Promise<T>;
}

export async function postJson<T>(path: string, body?: unknown): Promise<T> {
  const response = await fetch(`${apiBase}${path}`, { method: "POST", headers: { "Content-Type": "application/json" }, body: body ? JSON.stringify(body) : undefined });
  if (!response.ok) throw new Error(`API request failed (${response.status})`);
  return response.json() as Promise<T>;
}
