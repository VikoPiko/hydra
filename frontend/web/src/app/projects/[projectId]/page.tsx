"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { ProjectNav } from "@/components/ProjectNav";
import { Deployment, Environment, getJson, OperationalEvent, Project, Topology } from "@/lib/api";

export default function ProjectPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const [project, setProject] = useState<Project | null>(null);
  const [environments, setEnvironments] = useState<Environment[]>([]);
  const [environmentId, setEnvironmentId] = useState("");
  const [topology, setTopology] = useState<Topology | null>(null);
  const [events, setEvents] = useState<OperationalEvent[]>([]);
  const [deployments, setDeployments] = useState<Deployment[]>([]);
  const [error, setError] = useState("");
  useEffect(() => {
    Promise.all([getJson<Project>(`/api/projects/${projectId}`), getJson<Environment[]>(`/api/projects/${projectId}/environments`)])
      .then(([p, envs]) => { setProject(p); setEnvironments(envs); setEnvironmentId(envs.find(e => e.type === "Production")?.id ?? envs[0]?.id ?? ""); })
      .catch(e => setError(e.message));
  }, [projectId]);
  useEffect(() => {
    if (!environmentId) return;
    Promise.all([
      getJson<Topology>(`/api/projects/${projectId}/environments/${environmentId}/topology`),
      getJson<OperationalEvent[]>(`/api/projects/${projectId}/environments/${environmentId}/events?limit=5`),
      getJson<Deployment[]>(`/api/projects/${projectId}/environments/${environmentId}/deployments`)
    ]).then(([t, e, d]) => { setTopology(t); setEvents(e); setDeployments(d.slice(0, 5)); }).catch(e => setError(e.message));
  }, [projectId, environmentId]);
  const attention = topology?.nodes.filter(n => n.health === "Degraded" || n.health === "Unhealthy").length ?? 0;
  const componentName = (id: string | null) => topology?.nodes.find(n => n.id === id)?.name ?? "System";
  return <main className="page">
    <Link href="/projects" className="back-link">← Projects</Link><div className="eyebrow">SYSTEM WORKSPACE</div>
    <h1>{project?.name ?? "Loading…"}</h1><p className="lede">{project?.description}</p>
    <ProjectNav projectId={projectId} active="Overview" />
    <div className="toolbar"><label>Environment <select value={environmentId} onChange={e => setEnvironmentId(e.target.value)}>{environments.map(e => <option key={e.id} value={e.id}>{e.name}</option>)}</select></label>
      <Link className="button primary" href={`/projects/${projectId}/topology?environmentId=${environmentId}`}>Open topology →</Link></div>
    {error && <p className="error">{error}</p>}
    <div className="stats"><div><strong>{topology?.nodes.length ?? "—"}</strong><span>Components</span></div><div><strong>{topology?.edges.length ?? "—"}</strong><span>Dependencies</span></div><div><strong>{attention}</strong><span>Need attention</span></div></div>
    <div className="two-col"><section className="panel"><h2>Recent events</h2>{events.map(e => <div className="list-row" key={e.id}><span className={`severity ${e.severity.toLowerCase()}`}>{e.severity}</span><div><strong>{e.title}</strong><small>{componentName(e.componentId)} · {new Date(e.occurredAt).toLocaleString()}</small></div></div>)}{events.length === 0 && <p className="muted">No events for this environment.</p>}</section>
      <section className="panel"><h2>Recent deployments</h2>{deployments.map(d => <div className="list-row" key={d.id}><span className="version">{d.version}</span><div><strong>{componentName(d.componentId)}</strong><small>{d.status} · {new Date(d.startedAt).toLocaleString()}</small></div></div>)}{deployments.length === 0 && <p className="muted">No deployments for this environment.</p>}</section></div>
  </main>;
}
