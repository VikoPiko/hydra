"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { ProjectNav } from "@/components/ProjectNav";
import { Environment, getJson, OperationalEvent, Project } from "@/lib/api";

export default function EventsPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const [project, setProject] = useState<Project | null>(null);
  const [environments, setEnvironments] = useState<Environment[]>([]);
  const [environmentId, setEnvironmentId] = useState("");
  const [events, setEvents] = useState<OperationalEvent[]>([]);
  const [error, setError] = useState("");
  useEffect(() => { Promise.all([getJson<Project>(`/api/projects/${projectId}`), getJson<Environment[]>(`/api/projects/${projectId}/environments`)]).then(([p, envs]) => {
    setProject(p); setEnvironments(envs); setEnvironmentId(envs.find(e => e.type === "Production")?.id ?? envs[0]?.id ?? "");
  }).catch(e => setError(e.message)); }, [projectId]);
  useEffect(() => { if (environmentId) getJson<OperationalEvent[]>(`/api/projects/${projectId}/environments/${environmentId}/events?limit=200`).then(setEvents).catch(e => setError(e.message)); }, [projectId, environmentId]);
  return <main className="page"><Link href="/projects" className="back-link">← Projects</Link><div className="eyebrow">{project?.name ?? "PROJECT"}</div><h1>Operational events</h1>
    <ProjectNav projectId={projectId} active="Events" /><div className="toolbar"><label>Environment <select value={environmentId} onChange={e => setEnvironmentId(e.target.value)}>{environments.map(e => <option key={e.id} value={e.id}>{e.name}</option>)}</select></label></div>
    {error && <p className="error">{error}</p>}<section className="panel">{events.map(e => <div className="list-row" key={e.id}><span className={`severity ${e.severity.toLowerCase()}`}>{e.severity}</span><div><strong>{e.title}</strong><small>{e.type} · {new Date(e.occurredAt).toLocaleString()}</small>{e.description && <p>{e.description}</p>}</div></div>)}{events.length === 0 && <p className="muted">No events in this environment.</p>}</section>
  </main>;
}
