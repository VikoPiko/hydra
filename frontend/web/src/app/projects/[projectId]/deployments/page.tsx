"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { ProjectNav } from "@/components/ProjectNav";
import { Component, Deployment, Environment, getJson, Project } from "@/lib/api";

export default function DeploymentsPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const [project, setProject] = useState<Project | null>(null);
  const [environments, setEnvironments] = useState<Environment[]>([]);
  const [environmentId, setEnvironmentId] = useState("");
  const [components, setComponents] = useState<Component[]>([]);
  const [deployments, setDeployments] = useState<Deployment[]>([]);
  const [error, setError] = useState("");
  useEffect(() => { Promise.all([getJson<Project>(`/api/projects/${projectId}`), getJson<Environment[]>(`/api/projects/${projectId}/environments`), getJson<Component[]>(`/api/projects/${projectId}/components`)]).then(([p, envs, c]) => {
    setProject(p); setEnvironments(envs); setComponents(c); setEnvironmentId(envs.find(e => e.type === "Production")?.id ?? envs[0]?.id ?? "");
  }).catch(e => setError(e.message)); }, [projectId]);
  useEffect(() => { if (environmentId) getJson<Deployment[]>(`/api/projects/${projectId}/environments/${environmentId}/deployments`).then(setDeployments).catch(e => setError(e.message)); }, [projectId, environmentId]);
  return <main className="page"><Link href="/projects" className="back-link">← Projects</Link><div className="eyebrow">{project?.name ?? "PROJECT"}</div><h1>Deployment history</h1>
    <ProjectNav projectId={projectId} active="Deployments" /><div className="toolbar"><label>Environment <select value={environmentId} onChange={e => setEnvironmentId(e.target.value)}>{environments.map(e => <option key={e.id} value={e.id}>{e.name}</option>)}</select></label></div>
    {error && <p className="error">{error}</p>}<section className="panel table-panel"><table><thead><tr><th>Component</th><th>Version</th><th>Status</th><th>Started</th><th>Completed</th></tr></thead><tbody>{deployments.map(d => <tr key={d.id}><td>{components.find(c => c.id === d.componentId)?.name ?? d.componentId}</td><td><span className="version">{d.version}</span></td><td>{d.status}</td><td>{new Date(d.startedAt).toLocaleString()}</td><td>{d.completedAt ? new Date(d.completedAt).toLocaleString() : "—"}</td></tr>)}</tbody></table>{deployments.length === 0 && <p className="muted">No deployments in this environment.</p>}</section>
  </main>;
}
