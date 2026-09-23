"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { FormEvent, useEffect, useState } from "react";
import { ProjectNav } from "@/components/ProjectNav";
import { Environment, getJson, postJson, Project, ProjectMember } from "@/lib/api";

const types = ["Development", "Testing", "Staging", "Production", "Custom"];

export default function EnvironmentsPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const [project, setProject] = useState<Project | null>(null);
  const [environments, setEnvironments] = useState<Environment[]>([]);
  const [canEdit, setCanEdit] = useState(false);
  const [name, setName] = useState("");
  const [type, setType] = useState("Development");
  const [error, setError] = useState("");

  useEffect(() => {
    Promise.all([getJson<Project>(`/api/projects/${projectId}`), getJson<Environment[]>(`/api/projects/${projectId}/environments`),
      getJson<ProjectMember[]>(`/api/projects/${projectId}/members`), getJson<{ id: string }>("/api/me")])
      .then(([p, envs, members, me]) => { setProject(p); setEnvironments(envs);
        setCanEdit(members.some(member => member.userId === me.id && member.role !== "Viewer")); })
      .catch(cause => setError(cause instanceof Error ? cause.message : "Unable to load environments."));
  }, [projectId]);
  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    try {
      const environment = await postJson<Environment>(`/api/projects/${projectId}/environments`, { name, type });
      setEnvironments(current => [...current, environment].sort((a, b) => a.name.localeCompare(b.name)));
      setName(""); setError("");
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to create environment."); }
  }
  return <main className="page">
    <Link className="back-link" href={`/projects/${projectId}`}>← Project overview</Link><div className="eyebrow">PROJECT STRUCTURE</div>
    <h1>{project?.name ?? "Environments"}</h1><p className="lede">Track where components run and versions are deployed.</p>
    <ProjectNav projectId={projectId} active="Environments" />
    {error && <p className="error" role="alert">{error}</p>}
    <section className="panel"><h2>Environments</h2>{environments.map(environment => <div className="list-row" key={environment.id}><span className="version">{environment.type}</span><div><strong>{environment.name}</strong><small><Link href={`/projects/${projectId}/topology?environmentId=${environment.id}`}>Open topology →</Link></small></div></div>)}{project && environments.length === 0 && <p className="muted">No environments yet. Add Development, Staging, or Production to start tracking runtime state.</p>}</section>
    {canEdit && <section className="panel"><h2>Add environment</h2><form className="inline-form" onSubmit={create}><label>Name<input required minLength={2} maxLength={120} value={name} onChange={event => setName(event.target.value)} placeholder="e.g. Production" /></label><label>Type<select value={type} onChange={event => setType(event.target.value)}>{types.map(option => <option key={option}>{option}</option>)}</select></label><button className="button primary">Add environment</button></form></section>}
  </main>;
}
