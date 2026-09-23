"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { ProjectNav } from "@/components/ProjectNav";
import { Deployment, Environment, getJson, OperationalEvent, postJson, Project, ProjectActivity, Topology, WorkItem } from "@/lib/api";

export default function ProjectPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const [project, setProject] = useState<Project | null>(null);
  const [environments, setEnvironments] = useState<Environment[]>([]);
  const [environmentId, setEnvironmentId] = useState("");
  const [topology, setTopology] = useState<Topology | null>(null);
  const [events, setEvents] = useState<OperationalEvent[]>([]);
  const [deployments, setDeployments] = useState<Deployment[]>([]);
  const [workItems, setWorkItems] = useState<WorkItem[]>([]);
  const [activity, setActivity] = useState<ProjectActivity[]>([]);
  const [error, setError] = useState("");
  const [environmentName, setEnvironmentName] = useState("");
  const [environmentType, setEnvironmentType] = useState("Development");
  async function createEnvironment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    try {
      const environment = await postJson<Environment>(`/api/projects/${projectId}/environments`, { name: environmentName, type: environmentType });
      setEnvironments(current => [...current, environment]);
      setEnvironmentId(environment.id);
      setEnvironmentName("");
      setError("");
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to create environment."); }
  }
  useEffect(() => {
    Promise.all([getJson<Project>(`/api/projects/${projectId}`), getJson<Environment[]>(`/api/projects/${projectId}/environments`),
      getJson<WorkItem[]>(`/api/projects/${projectId}/work-items`), getJson<ProjectActivity[]>(`/api/projects/${projectId}/activity?limit=8`)])
      .then(([p, envs, work, feed]) => { setProject(p); setEnvironments(envs); setWorkItems(work); setActivity(feed);
        setEnvironmentId(envs.find(e => e.type === "Production")?.id ?? envs[0]?.id ?? ""); })
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
  const openWork = workItems.filter(item => item.state !== "Closed" && item.state !== "Removed");
  const activeBugs = openWork.filter(item => item.type === "Bug" && item.state === "Active").length;
  const componentName = (id: string | null) => topology?.nodes.find(n => n.id === id)?.name ?? "System";
  return <main className="page">
    <Link href="/projects" className="back-link">← Projects</Link><div className="eyebrow">SYSTEM WORKSPACE</div>
    <h1>{project?.name ?? "Loading…"}</h1><p className="lede">{project?.description}</p>
    <ProjectNav projectId={projectId} active="Overview" />
    {project && environments.length === 0 && <section className="panel"><h2>Add your first environment</h2><p className="muted">This project is empty. Add an environment to start tracking its topology and deployments.</p><form className="inline-form" onSubmit={createEnvironment}><label>Name<input required minLength={2} maxLength={120} value={environmentName} onChange={e => setEnvironmentName(e.target.value)} placeholder="e.g. Production" /></label><label>Type<select value={environmentType} onChange={e => setEnvironmentType(e.target.value)}>{["Development", "Testing", "Staging", "Production", "Custom"].map(type => <option key={type}>{type}</option>)}</select></label><button className="button primary">Add environment</button></form></section>}
    <div className="toolbar"><label>Environment <select value={environmentId} onChange={e => setEnvironmentId(e.target.value)}>{environments.map(e => <option key={e.id} value={e.id}>{e.name}</option>)}</select></label>
      {environmentId && <Link className="button primary" href={`/projects/${projectId}/topology?environmentId=${environmentId}`}>Open topology →</Link>}</div>
    {error && <p className="error">{error}</p>}
    <div className="stats"><div><strong>{openWork.length}</strong><span>Open work items</span></div><div><strong>{activeBugs}</strong><span>Active bugs</span></div><div><strong>{topology?.nodes.length ?? "—"}</strong><span>Components</span></div><div><strong>{topology?.edges.length ?? "—"}</strong><span>Dependencies</span></div><div><strong>{attention}</strong><span>Need attention</span></div></div>
    <div className="two-col"><section className="panel"><h2>Recent events</h2>{events.map(e => <div className="list-row" key={e.id}><span className={`severity ${e.severity.toLowerCase()}`}>{e.severity}</span><div><strong>{e.title}</strong><small>{componentName(e.componentId)} · {new Date(e.occurredAt).toLocaleString()}</small></div></div>)}{events.length === 0 && <p className="muted">No events for this environment.</p>}</section>
      <section className="panel"><h2>Recent deployments</h2>{deployments.map(d => <div className="list-row" key={d.id}><span className="version">{d.version}</span><div><strong>{componentName(d.componentId)}</strong><small>{d.status} · {new Date(d.startedAt).toLocaleString()}</small></div></div>)}{deployments.length === 0 && <p className="muted">No deployments for this environment.</p>}</section></div>
    <div className="two-col"><section className="panel"><div className="section-heading"><h2>Board snapshot</h2><Link href={`/projects/${projectId}/boards`}>Open board →</Link></div>{openWork.slice(0, 5).map(item => <div className="list-row" key={item.id}><span className="version">#{item.sequenceNumber}</span><div><strong>{item.title}</strong><small>{item.type} · {item.state} · {item.priority}</small></div></div>)}{openWork.length === 0 && <p className="muted">No open work items. Create one from the board.</p>}</section>
      <section className="panel"><h2>Recent activity</h2>{activity.map(entry => <div className="list-row" key={entry.id}><span className="version">{entry.kind === "WorkItem" ? "Work" : "Ops"}</span><div><strong>{entry.summary}</strong><small>{entry.actorName ? `${entry.actorName} · ` : ""}{new Date(entry.occurredAt).toLocaleString()}</small></div></div>)}{activity.length === 0 && <p className="muted">No project activity yet.</p>}</section></div>
  </main>;
}
