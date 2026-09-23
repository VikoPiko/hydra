"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { ReactFlow, Background, Controls, Handle, MiniMap, Position, type Node, type NodeProps } from "@xyflow/react";
import "@xyflow/react/dist/style.css";
import { apiBase, getJson, postJson, type Environment, type Impact, type OperationalEvent, type Project, type RuntimeState, type Topology, type TopologyNode, type Deployment, type WorkItem } from "@/lib/api";
import { mapTopology } from "@/lib/graph";
import { ProjectNav } from "./ProjectNav";

type ComponentGraphNode = Node<{ component: TopologyNode; selected: boolean; impactKind: string; dimmed: boolean }, "component">;

function ComponentNode({ data }: NodeProps<ComponentGraphNode>) {
  return <div className={`graph-node health-${data.component.health.toLowerCase()} ${data.selected ? "selected" : ""} ${data.impactKind ? `impact-${data.impactKind}` : ""} ${data.dimmed ? "dimmed" : ""}`}>
    <Handle type="target" position={Position.Top} />
    <div className="node-top"><span className="node-type">{data.component.type}</span><span className="node-version">{data.component.version ?? "No version"}</span></div>
    <strong>{data.component.name}</strong><span className="node-health"><span className="status-dot" />{data.component.health}</span>
    <Handle type="source" position={Position.Bottom} />
  </div>;
}
const nodeTypes = { component: ComponentNode };

export function TopologyWorkspace({ projectId }: { projectId: string }) {
  const [project, setProject] = useState<Project | null>(null);
  const [environments, setEnvironments] = useState<Environment[]>([]);
  const [environmentId, setEnvironmentId] = useState("");
  const [topology, setTopology] = useState<Topology | null>(null);
  const [events, setEvents] = useState<OperationalEvent[]>([]);
  const [deployments, setDeployments] = useState<Deployment[]>([]);
  const [linkedItems, setLinkedItems] = useState<WorkItem[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [impact, setImpact] = useState<Impact | null>(null);
  const [search, setSearch] = useState("");
  const [error, setError] = useState("");
  const [live, setLive] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  useEffect(() => {
    Promise.all([getJson<Project>(`/api/projects/${projectId}`), getJson<Environment[]>(`/api/projects/${projectId}/environments`)])
      .then(([p, envs]) => { setProject(p); setEnvironments(envs);
        const requested = new URLSearchParams(window.location.search).get("environmentId");
        setEnvironmentId(envs.find(e => e.id === requested)?.id ?? envs.find(e => e.type === "Production")?.id ?? envs[0]?.id ?? "");
      }).catch(e => setError(e.message));
  }, [projectId]);
  const refresh = useCallback(() => {
    if (!environmentId) return;
    Promise.all([
      getJson<Topology>(`/api/projects/${projectId}/environments/${environmentId}/topology`),
      getJson<OperationalEvent[]>(`/api/projects/${projectId}/environments/${environmentId}/events?limit=8`),
      getJson<Deployment[]>(`/api/projects/${projectId}/environments/${environmentId}/deployments`)
    ]).then(([t, e, d]) => { setTopology(t); setEvents(e); setDeployments(d); setError(""); }).catch(e => setError(e.message));
  }, [projectId, environmentId]);
  useEffect(() => { refresh(); }, [refresh, refreshKey]);
  useEffect(() => {
    if (!environmentId) return;
    const source = new EventSource(`${apiBase}/api/projects/${projectId}/environments/${environmentId}/events/stream`);
    source.onopen = () => { setLive(true); refresh(); };
    source.onerror = () => setLive(false);
    source.addEventListener("runtime-state-updated", event => {
      const state = JSON.parse((event as MessageEvent).data) as RuntimeState;
      setTopology(current => current ? { ...current, nodes: current.nodes.map(node => node.id === state.componentId ?
        { ...node, health: state.healthStatus, metrics: { cpuPercent: state.cpuPercent, memoryPercent: state.memoryPercent,
          requestRate: state.requestRate, errorRatePercent: state.errorRatePercent, latencyP95Ms: state.latencyP95Ms } } : node) } : current);
    });
    source.addEventListener("operational-event-created", event => {
      const item = JSON.parse((event as MessageEvent).data) as OperationalEvent;
      setEvents(current => [item, ...current.filter(x => x.id !== item.id)].slice(0, 8));
    });
    source.addEventListener("deployment-updated", () => refresh());
    return () => { source.close(); setLive(false); };
  }, [projectId, environmentId, refresh]);
  useEffect(() => { setSelectedId(null); setImpact(null); }, [environmentId]);
  useEffect(() => {
    if (!selectedId) { setLinkedItems([]); return; }
    let current = true;
    getJson<WorkItem[]>(`/api/components/${selectedId}/work-items`)
      .then(items => { if (current) setLinkedItems(items); })
      .catch(cause => { if (current) setError(cause instanceof Error ? cause.message : "Unable to load linked work items."); });
    return () => { current = false; };
  }, [selectedId, refreshKey]);
  const graph = useMemo(() => topology ? mapTopology(topology, selectedId, impact, search) : { nodes: [], edges: [] }, [topology, selectedId, impact, search]);
  const selected = topology?.nodes.find(n => n.id === selectedId);
  const dependencies = topology?.edges.filter(e => e.source === selectedId) ?? [];
  const dependents = topology?.edges.filter(e => e.target === selectedId) ?? [];
  const componentName = (id: string | null) => topology?.nodes.find(n => n.id === id)?.name ?? "System";
  const selectNode = (id: string) => { setSelectedId(id); setImpact(null); };
  const analyze = async () => {
    if (!selectedId) return;
    try { setImpact(await getJson<Impact>(`/api/projects/${projectId}/components/${selectedId}/impact`)); setError(""); }
    catch (e) { setError((e as Error).message); }
  };
  const simulate = async (mode: "degradation" | "recovery") => {
    if (!selectedId) return;
    try { await postJson(`/api/dev/simulate/${mode}/${selectedId}?environmentId=${environmentId}`); setError(""); }
    catch (e) { setError((e as Error).message); }
  };
  const totals = { healthy: topology?.nodes.filter(n => n.health === "Healthy").length ?? 0,
    degraded: topology?.nodes.filter(n => n.health === "Degraded").length ?? 0,
    unhealthy: topology?.nodes.filter(n => n.health === "Unhealthy").length ?? 0 };
  return <main className="workspace">
    <header className="workspace-header"><div><Link href="/projects" className="back-link">← Projects</Link><h1>{project?.name ?? "Topology"}</h1><span className="subtitle">Live operational topology</span></div>
      <div className="header-status"><span className={live ? "live-indicator online" : "live-indicator"}>{live ? "Live" : "Reconnecting"}</span><span>{totals.healthy} healthy</span><span>{totals.degraded} degraded</span><span>{totals.unhealthy} unhealthy</span></div></header>
    <ProjectNav projectId={projectId} active="Topology" />
    <div className="workspace-toolbar"><label>Environment <select value={environmentId} onChange={e => setEnvironmentId(e.target.value)}>{environments.map(e => <option key={e.id} value={e.id}>{e.name}</option>)}</select></label>
      <input aria-label="Search components" placeholder="Search components…" value={search} onChange={e => setSearch(e.target.value)} />
      <button onClick={() => setRefreshKey(x => x + 1)}>Refresh</button><span className="toolbar-hint">Select a component to inspect impact</span></div>
    {error && <p className="error workspace-error">{error}</p>}
    <div className="workspace-main"><div className="graph-area" role="img" aria-label="Interactive dependency topology">
      <ReactFlow nodes={graph.nodes} edges={graph.edges} nodeTypes={nodeTypes} onNodeClick={(_, node) => selectNode(node.id)} fitView fitViewOptions={{ padding: 0.25 }} minZoom={0.35} maxZoom={1.5} proOptions={{ hideAttribution: true }}>
        <Background color="#294257" gap={26} size={1} /><Controls /><MiniMap nodeColor={node => {
          const component = (node.data as ComponentGraphNode["data"]).component;
          return component.health === "Degraded" ? "#e7ad5b" : component.health === "Unhealthy" ? "#e47676" : "#4bc7a7";
        }} /></ReactFlow>
      {!topology && <div className="graph-loading">Loading topology…</div>}
      <div className="graph-legend"><span>● Healthy</span><span>● Degraded</span><span>● Unhealthy</span><span>● Unknown</span></div>
    </div><aside className="detail-panel">
      {selected ? <><div className="eyebrow">COMPONENT DETAIL</div><h2>{selected.name}</h2><div className="detail-subtitle">{selected.type} · <span className={`text-${selected.health.toLowerCase()}`}>{selected.health}</span></div>
        <div className="detail-group"><h3>Current state</h3><dl><dt>Version</dt><dd>{selected.version ?? "Unknown"}</dd><dt>Owner</dt><dd>{selected.owner ?? "—"}</dd><dt>Repository</dt><dd>{selected.repositoryUrl ? <a href={selected.repositoryUrl} target="_blank" rel="noreferrer">Open repository ↗</a> : "—"}</dd></dl></div>
        <div className="detail-group"><h3>Metrics</h3><dl><dt>CPU</dt><dd>{formatMetric(selected.metrics.cpuPercent, "%")}</dd><dt>Memory</dt><dd>{formatMetric(selected.metrics.memoryPercent, "%")}</dd><dt>Request rate</dt><dd>{formatMetric(selected.metrics.requestRate, "/s")}</dd><dt>Error rate</dt><dd>{formatMetric(selected.metrics.errorRatePercent, "%")}</dd><dt>P95 latency</dt><dd>{formatMetric(selected.metrics.latencyP95Ms, " ms")}</dd></dl></div>
        <div className="detail-group"><h3>Dependencies</h3>{dependencies.length ? dependencies.map(e => <div className="relation" key={e.id}>{componentName(e.target)} <small>{e.type} · {e.sourceKind}</small></div>) : <p className="muted">None</p>}
          <h3>Dependents</h3>{dependents.length ? dependents.map(e => <div className="relation" key={e.id}>{componentName(e.source)} <small>{e.type} · {e.sourceKind}</small></div>) : <p className="muted">None</p>}</div>
        <button className="button primary full" onClick={analyze}>Analyze potential impact</button>
        {impact && <div className="impact-results"><h3>Potentially affected</h3><strong>Direct</strong>{impact.directDependents.length ? impact.directDependents.map(x => <p key={x.id}>{x.name}</p>) : <p className="muted">None</p>}<strong>Indirect</strong>{impact.indirectDependents.length ? impact.indirectDependents.map(x => <p key={x.id}>{x.name}</p>) : <p className="muted">None</p>}</div>}
        {process.env.NEXT_PUBLIC_ENABLE_DEMO === "true" && <div className="demo-actions"><button onClick={() => simulate("degradation")}>Simulate degradation</button><button onClick={() => simulate("recovery")}>Restore health</button></div>}
        <div className="detail-group"><h3>Recent deployments</h3>{deployments.filter(d => d.componentId === selected.id).slice(0, 3).map(d => <div className="relation" key={d.id}>{d.version}<small>{d.status} · {new Date(d.startedAt).toLocaleString()}</small></div>)}</div>
        <div className="detail-group"><h3>Active linked work</h3>{linkedItems.length ? linkedItems.map(item => <div className="relation" key={item.id}>#{item.sequenceNumber} {item.title}<small>{item.type} · {item.state} · {item.priority}</small></div>) : <p className="muted">No active work items linked to this component.</p>}<Link href={`/projects/${projectId}/boards`}>Open board →</Link></div>
        <div className="detail-group"><h3>Recent events</h3>{events.filter(e => e.componentId === selected.id).slice(0, 3).map(e => <div className="relation" key={e.id}>{e.title}<small>{new Date(e.occurredAt).toLocaleString()}</small></div>)}</div>
      </> : <div className="empty-detail"><span>◎</span><h2>Select a component</h2><p>Inspect health, deployment, dependencies, and potential impact.</p></div>}
    </aside></div>
    <section className="timeline"><div className="timeline-heading"><h2>Recent operational events</h2><Link href={`/projects/${projectId}/events`}>View all →</Link></div><div className="timeline-items">{events.map(e => <div className="timeline-item" key={e.id}><span className={`severity ${e.severity.toLowerCase()}`}>{e.severity}</span><div><strong>{e.title}</strong><small>{componentName(e.componentId)} · {new Date(e.occurredAt).toLocaleString()}</small></div></div>)}</div></section>
  </main>;
}

function formatMetric(value: number | null, unit: string) { return value == null ? "—" : `${value}${unit}`; }
